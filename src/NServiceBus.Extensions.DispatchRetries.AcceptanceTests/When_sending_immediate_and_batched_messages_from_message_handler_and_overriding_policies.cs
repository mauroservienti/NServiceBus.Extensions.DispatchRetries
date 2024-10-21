using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_immediate_and_batched_messages_from_message_handler_and_overriding_policies
    {
        private static int _numberOfImmediatePollyPolicyRetries = 0;
        private static int _numberOfBatchPollyPolicyRetries = 0;
        private static int _numberOfOverriddenImmediatePollyPolicyRetries = 0;
        private static int _numberOfOverriddenBatchPollyPolicyRetries = 0;

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
            Assert.That(_numberOfImmediatePollyPolicyRetries, Is.EqualTo(0), "Wrong number of immediate policy retries");
            Assert.That(_numberOfBatchPollyPolicyRetries, Is.EqualTo(0), "Wrong number of batch policy retries");
            Assert.That(_numberOfOverriddenImmediatePollyPolicyRetries, Is.EqualTo(1), "Wrong number of overridden immediate policy retries");
            Assert.That(_numberOfOverriddenBatchPollyPolicyRetries, Is.EqualTo(1), "Wrong number of overridden batch policy retries");
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
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfBatchPollyPolicyRetries++;
                        });

                    var immediatePolicy = Policy
                        .Handle<Exception>(ex=>true)
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
                    var batchPolicy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfOverriddenBatchPollyPolicyRetries++;
                        });

                    context.OverrideBatchDispatchRetryPolicy(batchPolicy);

                    var immediatePolicy = Policy
                        .Handle<Exception>(ex=>true)
                        .RetryAsync(1, (exception, retryAttempt, context) =>
                        {
                            _numberOfOverriddenImmediatePollyPolicyRetries++;
                        });

                    context.OverrideBatchDispatchRetryPolicy(batchPolicy);
                    context.OverrideImmediateDispatchRetryPolicy(immediatePolicy);

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