using System;
using NServiceBus.Extensions.DispatchRetries;
using Polly;

namespace NServiceBus
{
    public static class MessageHandlerContextExtensions
    {
        public static void OverrideImmediateDispatchRetryPolicy(this IMessageHandlerContext context, AsyncPolicy immediateDispatchRetryPolicy)
        {
            if (immediateDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryPolicy));
            }

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.ImmediateDispatchPolicyOverride = immediateDispatchRetryPolicy;
        }

        public static void OverrideBatchDispatchRetryPolicy(this IMessageHandlerContext context, AsyncPolicy batchDispatchRetryPolicy)
        {
            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.BatchDispatchPolicyOverride = batchDispatchRetryPolicy;
        }
        
        public static void OverrideBatchDispatchRetryStrategy(this IMessageHandlerContext context, ResiliencePipeline batchDispatchRetryResiliencePipeline)
        {
            if (batchDispatchRetryResiliencePipeline == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryResiliencePipeline));
            }

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.BatchDispatchResiliencePipelineOverride = batchDispatchRetryResiliencePipeline;
        }
    }
}