using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class BatchDispatchRetriesBehavior : Behavior<IBatchDispatchContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public BatchDispatchRetriesBehavior(IReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultRetryPolicy);
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            if (overrides.BatchDispatchPolicyOverride != null)
            {
                return overrides.BatchDispatchPolicyOverride.ExecuteAsync(next);
            }

            if (_defaultRetryPolicy!= null)
            {
                return _defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}