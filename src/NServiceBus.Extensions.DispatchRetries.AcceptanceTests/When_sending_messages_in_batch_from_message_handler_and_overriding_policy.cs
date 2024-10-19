using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_messages_in_batch_from_message_handler_and_overriding_policy
    {
        private static int _numberOfPollyPolicyRetries = 0;
        private static int _numberOfOverriddenPollyPolicyRetries = 0;

        private static int _numberOfPollyResiliencePipelineRetries = 0;
        private static int _numberOfOverriddenPollyResiliencePipelineRetries = 0;

        [Test]
        public async Task should_be_retried_according_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpointWithPolicy));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpointWithPolicy>()
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(_numberOfPollyPolicyRetries, Is.EqualTo(0));
            Assert.That(_numberOfOverriddenPollyPolicyRetries, Is.EqualTo(1));
        }
        
        [Test]
        public async Task should_be_retried_according_to_resilience_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpointWithResiliencePipeline));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpointWithResiliencePipeline>()
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(_numberOfPollyResiliencePipelineRetries, Is.EqualTo(0));
            Assert.That(_numberOfOverriddenPollyResiliencePipelineRetries, Is.EqualTo(1));
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

        class ReceiverEndpointWithPolicy : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithPolicy()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var policy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfPollyPolicyRetries++;
                        });

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesPolicy(policy);
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    var policy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfOverriddenPollyPolicyRetries++;
                        });

                    context.OverrideBatchDispatchRetryPolicy(policy);

                    return context.Reply(new ReplyMessage());
                }
            }
        }
        
        class ReceiverEndpointWithResiliencePipeline : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithResiliencePipeline()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var pipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfPollyResiliencePipelineRetries++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesResiliencePipeline(pipeline);
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    var pipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfOverriddenPollyResiliencePipelineRetries++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();

                    context.OverrideBatchDispatchRetryStrategy(pipeline);

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