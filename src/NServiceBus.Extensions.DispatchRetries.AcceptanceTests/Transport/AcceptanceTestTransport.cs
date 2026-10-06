namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Routing;
    using Transport;

    public class AcceptanceTestTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
    {
        public AcceptanceTestTransport(bool enableNativeDelayedDelivery = true, bool enableNativePublishSubscribe = true)
            : base(TransportTransactionMode.SendsAtomicWithReceive, enableNativeDelayedDelivery, enableNativePublishSubscribe, true)
        {
        }

        public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
        {
            if (hostSettings == null)
            {
                throw new ArgumentNullException(nameof(hostSettings));
            }
            var infrastructure = new AcceptanceTestTransportInfrastructure(hostSettings, this, receivers, FailToDispatchOnce);
            infrastructure.ConfigureDispatcher();
            await infrastructure.ConfigureReceivers().ConfigureAwait(false);

            return infrastructure;
        }

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
        {
            return new[]
            {
                TransportTransactionMode.None,
                TransportTransactionMode.ReceiveOnly,
                TransportTransactionMode.SendsAtomicWithReceive
            };
        }

        public bool FailToDispatchOnce { get; set; }

        string storageLocation;
        public string StorageLocation
        {
            get => storageLocation;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(StorageLocation));
                }
                PathChecker.ThrowForBadPath(value, nameof(StorageLocation));
                storageLocation = value;
            }
        }

        static void ThrowForBadPath(string value, string valueName)
        {
            var invalidPathChars = Path.GetInvalidPathChars();

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (value.IndexOfAny(invalidPathChars) < 0)
            {
                return;
            }

            throw new Exception($"The value for '{valueName}' has illegal path characters. Provided value: {value}. Must not contain any of {string.Join(", ", invalidPathChars)}.");
        }
    }
}