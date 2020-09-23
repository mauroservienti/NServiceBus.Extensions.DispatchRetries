using NServiceBus.Extensions.DispatchRetries.AcceptanceTests.Transport.Helpers;

namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Extensibility;
    using Performance.TimeToBeReceived;
    using Transport;

    class AcceptanceTestTransportDispatcher : IDispatchMessages
    {
        public AcceptanceTestTransportDispatcher(string basePath, int maxMessageSizeKB, bool failDispatchOnce)
        {
            if (maxMessageSizeKB > int.MaxValue / 1024)
            {
                throw new ArgumentException("The message size cannot be larger than int.MaxValue / 1024.", nameof(maxMessageSizeKB));
            }

            this.basePath = basePath;
            this.maxMessageSizeKB = maxMessageSizeKB;
            _failDispatchOnce = failDispatchOnce;
        }

        private List<string> _idsForWhichDispatchAlreadyFailedOnce = new List<string>();

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            var allIds = outgoingMessages.MulticastTransportOperations.Select(mto => mto.Message.MessageId).ToList();
            allIds.AddRange(outgoingMessages.UnicastTransportOperations.Select(mto => mto.Message.MessageId));
            var alreadyFailedOnce = allIds.All(id => _idsForWhichDispatchAlreadyFailedOnce.Contains(id));
            if (_failDispatchOnce && !alreadyFailedOnce)
            {
                _idsForWhichDispatchAlreadyFailedOnce.AddRange(allIds);
                throw new Exception("Bad!");
            }

            return Task.WhenAll(
                DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction),
                DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction));
        }

        async Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction)
        {
            var tasks = new List<Task>();

            foreach (var transportOperation in transportOperations)
            {
                var subscribers = await GetSubscribersFor(transportOperation.MessageType)
                    .ConfigureAwait(false);

                foreach (var subscriber in subscribers)
                {
                    tasks.Add(WriteMessage(subscriber, transportOperation, transaction));
                }
            }

            await Task.WhenAll(tasks)
                .ConfigureAwait(false);
        }

        Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction)
        {
            return Task.WhenAll(operations.Select(operation =>
            {
                PathChecker.ThrowForBadPath(operation.Destination, "message destination");

                return WriteMessage(operation.Destination, operation, transaction);
            }));
        }

        async Task WriteMessage(string destination, IOutgoingTransportOperation transportOperation, TransportTransaction transaction)
        {
            var message = transportOperation.Message;

            var nativeMessageId = Guid.NewGuid().ToString();
            var destinationPath = Path.Combine(basePath, destination);
            var bodyDir = Path.Combine(destinationPath, AcceptanceTestTransportMessagePump.BodyDirName);

            Directory.CreateDirectory(bodyDir);

            var bodyPath = Path.Combine(bodyDir, nativeMessageId) + AcceptanceTestTransportMessagePump.BodyFileSuffix;

            await AsyncFile.WriteBytes(bodyPath, message.Body)
                .ConfigureAwait(false);

            DateTime? timeToDeliver = null;

            if (transportOperation.DeliveryConstraints.TryGet(out DoNotDeliverBefore doNotDeliverBefore))
            {
                timeToDeliver = doNotDeliverBefore.At;
            }
            else if (transportOperation.DeliveryConstraints.TryGet(out DelayDeliveryWith delayDeliveryWith))
            {
                timeToDeliver = DateTime.UtcNow + delayDeliveryWith.Delay;
            }

            if (timeToDeliver.HasValue)
            {
                // we need to "ceil" the seconds to guarantee that we delay with at least the requested value
                // since the folder name has only second resolution.
                if (timeToDeliver.Value.Millisecond > 0)
                {
                    timeToDeliver += TimeSpan.FromSeconds(1);
                }

                destinationPath = Path.Combine(destinationPath, AcceptanceTestTransportMessagePump.DelayedDirName, timeToDeliver.Value.ToString("yyyyMMddHHmmss"));

                Directory.CreateDirectory(destinationPath);
            }

            if (transportOperation.DeliveryConstraints.TryGet(out DiscardIfNotReceivedBefore timeToBeReceived) && timeToBeReceived.MaxTime < TimeSpan.MaxValue)
            {
                if (timeToDeliver.HasValue)
                {
                    throw new Exception($"Postponed delivery of messages with TimeToBeReceived set is not supported. Remove the TimeToBeReceived attribute to postpone messages of type '{message.Headers[Headers.EnclosedMessageTypes]}'.");
                }

                message.Headers[AcceptanceTestTransportHeaders.TimeToBeReceived] = timeToBeReceived.MaxTime.ToString();
            }

            var messagePath = Path.Combine(destinationPath, nativeMessageId) + ".metadata.txt";

            var headerPayload = HeaderSerializer.Serialize(message.Headers);
            var headerSize = Encoding.UTF8.GetByteCount(headerPayload);

            if (headerSize + message.Body.Length > maxMessageSizeKB * 1024)
            {
                throw new Exception($"The total size of the '{message.Headers[Headers.EnclosedMessageTypes]}' message body ({message.Body.Length} bytes) plus headers ({headerSize} bytes) is larger than {maxMessageSizeKB} KB and will not be supported on some production transports. Consider using the NServiceBus DataBus or the claim check pattern to avoid messages with a large payload. Use 'EndpointConfiguration.UseTransport<LearningTransport>().NoPayloadSizeRestriction()' to disable this check and proceed with the current message size.");
            }

            if (transportOperation.RequiredDispatchConsistency != DispatchConsistency.Isolated && transaction.TryGet(out IAcceptanceTestTransportTransaction directoryBasedTransaction))
            {
                await directoryBasedTransaction.Enlist(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
            else
            {
                // atomic avoids the file being locked when the receiver tries to process it
                await AsyncFile.WriteTextAtomic(messagePath, headerPayload)
                    .ConfigureAwait(false);
            }
        }

        async Task<IEnumerable<string>> GetSubscribersFor(Type messageType)
        {
            var subscribers = new HashSet<string>();

            var allEventTypes = GetPotentialEventTypes(messageType);

            foreach (var eventType in allEventTypes)
            {
                var eventDir = Path.Combine(basePath, ".events", eventType.FullName);

                if (!Directory.Exists(eventDir))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(eventDir))
                {
                    var allText = await AsyncFile.ReadText(file)
                        .ConfigureAwait(false);

                    subscribers.Add(allText);
                }
            }

            return subscribers;
        }

        static IEnumerable<Type> GetPotentialEventTypes(Type messageType)
        {
            var allEventTypes = new HashSet<Type>();

            var currentType = messageType;

            while (currentType != null)
            {
                //do not include the marker interfaces
                if (IsCoreMarkerInterface(currentType))
                {
                    break;
                }

                allEventTypes.Add(currentType);

                currentType = currentType.BaseType;
            }

            foreach (var type in messageType.GetInterfaces().Where(i => !IsCoreMarkerInterface(i)))
            {
                allEventTypes.Add(type);
            }

            return allEventTypes;
        }

        static bool IsCoreMarkerInterface(Type type) => type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

        int maxMessageSizeKB;
        private readonly bool _failDispatchOnce;
        string basePath;
    }
}