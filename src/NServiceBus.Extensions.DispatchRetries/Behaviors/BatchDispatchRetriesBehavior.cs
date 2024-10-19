using System;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class BatchDispatchRetriesBehavior : Behavior<IBatchDispatchContext>
    {
        static ILog logger = LogManager.GetLogger(typeof(BatchDispatchRetriesBehavior));
        private readonly AsyncPolicy _defaultRetryPolicy;
        private readonly ResiliencePipeline _defaultRetryResiliencePipeline;

        public BatchDispatchRetriesBehavior(IReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultRetryPolicy);
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryResiliencePipeline, out _defaultRetryResiliencePipeline);

            if (_defaultRetryPolicy != null && _defaultRetryResiliencePipeline != null)
            {
                logger.Warn("Both a default batch dispatch retry policy and a batch dispatch retry resilience strategy are configured. Only the batch dispatch retry resilience strategy will be used.");
            }
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            if (overrides.BatchDispatchResiliencePipelineOverride != null)
            {
                return overrides.BatchDispatchResiliencePipelineOverride.ExecuteAsync(_=>new ValueTask(next()), context.CancellationToken).AsTask();
            }
            
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