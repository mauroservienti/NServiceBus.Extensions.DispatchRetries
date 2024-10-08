using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_immediate_and_batched_messages_from_message_handler
    {
        private static int _numberOfImmediatePollyRetries = 0;
        private static int _numberOfBatchPollyRetries = 0;

        [Test]
        public async Task should_be_retried_according_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination("ReceiverEndpoint");

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpoint>()
                .Done(c => c.ReplyMessageReceived && c.AnotherReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(context.AnotherReplyMessageReceived, Is.True);
            Assert.That(1, Is.EqualTo(_numberOfImmediatePollyRetries), "Wrong number of immediate policy retries");
            Assert.That(1, Is.EqualTo(_numberOfBatchPollyRetries), "Wrong number of batch policy retries");
        }

        class Context : ScenarioContext
        {
            public bool ReplyMessageReceived { get; set; }
            public bool AnotherReplyMessageReceived { get; set; }
        }

        class SenderEndpoint : EndpointConfigurationBuilder
        {
            public SenderEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {

                });
            }

            class ReplyHandler : IHandleMessages<ReplyMessage>
            {
                private readonly Context _testContext;

                public ReplyHandler(Context testContext)
                {
                    _testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    _testContext.ReplyMessageReceived = true;

                    return Task.FromResult(0);
                }
            }

            class AnotherReplyHandler : IHandleMessages<AnotherReplyMessage>
            {
                private readonly Context _testContext;

                public AnotherReplyHandler(Context testContext)
                {
                    _testContext = testContext;
                }

                public Task Handle(AnotherReplyMessage message, IMessageHandlerContext context)
                {
                    _testContext.AnotherReplyMessageReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        class ReceiverEndpoint : EndpointConfigurationBuilder
        {
            public ReceiverEndpoint()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var batchPolicy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfBatchPollyRetries++;
                        });

                    var immediatePolicy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfImmediatePollyRetries++;
                        });

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesPolicy(batchPolicy);
                    dispatchRetriesOptions.DefaultImmediateDispatchRetriesPolicy(immediatePolicy);
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public async Task Handle(Message message, IMessageHandlerContext context)
                {
                    var options = new ReplyOptions();
                    options.RequireImmediateDispatch();
                    await context.Reply(new ReplyMessage(), options);

                    await context.Reply(new AnotherReplyMessage());
                }
            }
        }

        public class Message : IMessage
        {
        }

        public class ReplyMessage : IMessage
        {
        }

        public class AnotherReplyMessage : IMessage
        {
        }
    }
}