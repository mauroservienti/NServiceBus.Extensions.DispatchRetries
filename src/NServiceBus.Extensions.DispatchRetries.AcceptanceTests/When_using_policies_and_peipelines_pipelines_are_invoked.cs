using System;
using System.Linq;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Reporters;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NServiceBus.Extensions.DispatchRetries.Behaviors;
using NServiceBus.Logging;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_using_policies_and_peipelines_pipelines_are_invoked
    {
        private static int _numberOfPollyRetriesWithPolicy = 0;
        private static int _numberOfPollyRetriesWithResilienceStrategy = 0;
        
        [Test]
        [UseReporter(typeof(DiffReporter))]
        public async Task should_be_retried_according_to_resilience_pipeline_and_log_warning()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpoint));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpoint>()
                .Done(c => c.MessageReceived)
                .Run();

            
            var warning = context.Logs.Single(l=>l.Level== LogLevel.Warn && l.LoggerName.EndsWith(nameof(BatchDispatchRetriesBehavior)));
            Approvals.Verify(warning.Message);
            
            Assert.That(context.MessageReceived, Is.True);
            Assert.That(_numberOfPollyRetriesWithPolicy, Is.EqualTo(0));
            Assert.That(_numberOfPollyRetriesWithResilienceStrategy, Is.EqualTo(1));
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
                            _numberOfPollyRetriesWithPolicy++;
                        });
                    
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
                    
                    var options = config.DispatchRetries();
                    options.DefaultBatchDispatchRetriesPolicy(policy);
                    options.DefaultImmediateDispatchRetriesPolicy(policy);
                    options.DefaultBatchDispatchRetriesResilienceStrategy(pipeline);
                    options.DefaultImmediateDispatchRetriesResilienceStrategy(pipeline);
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