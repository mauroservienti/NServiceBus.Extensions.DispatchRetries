using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_immediate_and_batched_messages_from_message_handler
    {
        private static int _numberOfImmediatePollyPolicyRetries = 0;
        private static int _numberOfBatchPollyPolicyRetries = 0;
        
        private static int _numberOfImmediatePollyResiliencePipelineRetries = 0;
        private static int _numberOfBatchPollyResiliencePipelineRetries = 0;

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
                .Done(c => c.ReplyMessageReceived && c.AnotherReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(context.AnotherReplyMessageReceived, Is.True);
            Assert.That(_numberOfImmediatePollyPolicyRetries, Is.EqualTo(1), "Wrong number of immediate policy retries");
            Assert.That(_numberOfBatchPollyPolicyRetries, Is.EqualTo(1), "Wrong number of batch policy retries");
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
                .Done(c => c.ReplyMessageReceived && c.AnotherReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(context.AnotherReplyMessageReceived, Is.True);
            Assert.That(_numberOfImmediatePollyResiliencePipelineRetries, Is.EqualTo(1), "Wrong number of immediate resilience pipeline retries");
            Assert.That(_numberOfBatchPollyResiliencePipelineRetries, Is.EqualTo(1), "Wrong number of batch resilience pipeline retries");
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

        class ReceiverEndpointWithPolicy : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithPolicy()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var batchPolicy = Policy
                        .Handle<Exception>(ex => true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfBatchPollyPolicyRetries++;
                        });

                    var immediatePolicy = Policy
                        .Handle<Exception>(ex => true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfImmediatePollyPolicyRetries++;
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

        class ReceiverEndpointWithResiliencePipeline : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithResiliencePipeline()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var batchPipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfBatchPollyResiliencePipelineRetries++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();
                    
                    var immediatePipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfImmediatePollyResiliencePipelineRetries++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesResiliencePipeline(batchPipeline);
                    dispatchRetriesOptions.DefaultImmediateDispatchRetriesResiliencePipeline(immediatePipeline);
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