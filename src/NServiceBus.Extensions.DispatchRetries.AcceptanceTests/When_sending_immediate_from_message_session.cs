using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_immediate_from_message_session
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
                    options.RequireImmediateDispatch();

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpoint>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
            Assert.AreEqual(1, _numberOfPollyRetries);
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class SenderEndpoint : EndpointConfigurationBuilder
        {
            public SenderEndpoint()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var policy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfPollyRetries++;
                        });

                    var dispatchRetriesOptions= config.DispatchRetries(policy);
                });
            }
        }

        class ReceiverEndpoint : EndpointConfigurationBuilder
        {
            public ReceiverEndpoint()
            {
                EndpointSetup<DefaultServer>(config => { });
            }

            class Handler : IHandleMessages<Message>
            {
                private readonly Context _testContext;

                public Handler(Context testContext)
                {
                    _testContext = testContext;
                }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    _testContext.MessageReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class Message : IMessage
        {
        }
    }
}