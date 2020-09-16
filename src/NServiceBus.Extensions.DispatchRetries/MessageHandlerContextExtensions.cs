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

            context.Extensions.Set(Constants.ImmediateDispatchRetryPolicy, immediateDispatchRetryPolicy);
        }

        public static void OverrideBatchDispatchRetryPolicy(this IMessageHandlerContext context, AsyncPolicy batchDispatchRetryPolicy)
        {
            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            context.Extensions.Set(Constants.BatchDispatchRetryPolicy, batchDispatchRetryPolicy);
        }
    }
}