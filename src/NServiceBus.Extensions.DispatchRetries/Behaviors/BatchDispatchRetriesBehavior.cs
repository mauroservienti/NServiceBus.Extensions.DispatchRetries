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
        private readonly ResiliencePipeline _defaultRetryResiliencePipeline;

        public BatchDispatchRetriesBehavior(IReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultRetryPolicy);
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryResiliencePipeline, out _defaultRetryResiliencePipeline);

            if (_defaultRetryPolicy != null && _defaultRetryResiliencePipeline != null)
            {
                //TODO warn if both a Policy and a ResiliencePipeline are set for the same dispatch mode and inform that only the ResiliencePipeline will be used    
            }
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            //TODO: Add support for the resilience pipelines
            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            if (overrides.BatchDispatchPolicyOverride != null)
            {
                return overrides.BatchDispatchPolicyOverride.ExecuteAsync(next);
            }

            if (_defaultRetryResiliencePipeline != null)
            {
                return _defaultRetryResiliencePipeline.ExecuteAsync(_=>new ValueTask(next()), context.CancellationToken).AsTask();
            }
            
            if (_defaultRetryPolicy!= null)
            {
                return _defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}