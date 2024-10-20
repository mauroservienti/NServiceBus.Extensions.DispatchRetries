using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_from_message_session
    {
        private static int _numberOfPollyRetriesWithPolicy = 0;
        private static int _numberOfPollyRetriesWithResilienceStrategy = 0;

        [Test]
        public async Task should_be_retried_according_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpointWithPolicy>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpoint));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpoint>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.MessageReceived, Is.True);
            Assert.That(_numberOfPollyRetriesWithPolicy, Is.EqualTo(1));
        }
        
        [Test]
        public async Task should_be_retried_according_to_resilience_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpointWithResiliencePipeline>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpoint));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpoint>()
                .Done(c => c.MessageReceived)
                .Run();

            Assert.That(context.MessageReceived, Is.True);
            Assert.That(_numberOfPollyRetriesWithResilienceStrategy, Is.EqualTo(1));
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class SenderEndpointWithPolicy : EndpointConfigurationBuilder
        {
            public SenderEndpointWithPolicy()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var policy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfPollyRetriesWithPolicy++;
                        });

                    _ = config.DispatchRetries(policy);
                });
            }
        }
        
        class SenderEndpointWithResiliencePipeline : EndpointConfigurationBuilder
        {
            public SenderEndpointWithResiliencePipeline()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var pipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfPollyRetriesWithResilienceStrategy++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();
                    _ = config.DispatchRetries(pipeline);
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