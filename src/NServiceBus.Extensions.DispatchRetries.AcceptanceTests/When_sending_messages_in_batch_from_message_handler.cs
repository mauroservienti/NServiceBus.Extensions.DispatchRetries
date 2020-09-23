using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_messages_in_batch_from_message_handler
    {
        private static int _numberOfPollyRetries = 0;

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
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.True(context.ReplyMessageReceived);
            Assert.AreEqual(1, _numberOfPollyRetries);
        }

        class Context : ScenarioContext
        {
            public bool ReplyMessageReceived { get; set; }
        }

        class SenderEndpoint : EndpointConfigurationBuilder
        {
            public SenderEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {

                });
            }

            class Handler : IHandleMessages<ReplyMessage>
            {
                private readonly Context _testContext;

                public Handler(Context testContext)
                {
                    _testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    _testContext.ReplyMessageReceived = true;

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
                    var policy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfPollyRetries++;
                        });

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesPolicy(policy);
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    return context.Reply(new ReplyMessage());
                }
            }
        }

        public class Message : IMessage
        {
        }

        public class ReplyMessage : IMessage
        {
        }
    }
}