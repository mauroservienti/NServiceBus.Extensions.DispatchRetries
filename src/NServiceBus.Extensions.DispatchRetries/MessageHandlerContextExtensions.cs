using System;
using NServiceBus.Extensions.DispatchRetries;
using Polly;

namespace NServiceBus
{
    public static class MessageHandlerContextExtensions
    {
        public static void OverrideImmediateDispatchRetryPolicy(this IMessageHandlerContext context, AsyncPolicy immediateDispatchRetryPolicy)
        {
            ArgumentNullException.ThrowIfNull(immediateDispatchRetryPolicy);

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.ImmediateDispatchPolicyOverride = immediateDispatchRetryPolicy;
        }

        public static void OverrideBatchDispatchRetryPolicy(this IMessageHandlerContext context, AsyncPolicy batchDispatchRetryPolicy)
        {
            ArgumentNullException.ThrowIfNull(batchDispatchRetryPolicy);

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.BatchDispatchPolicyOverride = batchDispatchRetryPolicy;
        }
        
        public static void OverrideBatchDispatchRetryStrategy(this IMessageHandlerContext context, ResiliencePipeline batchDispatchRetryResiliencePipeline)
        {
            ArgumentNullException.ThrowIfNull(batchDispatchRetryResiliencePipeline);

            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            overrides.BatchDispatchResiliencePipelineOverride = batchDispatchRetryResiliencePipeline;
        }
    }
}