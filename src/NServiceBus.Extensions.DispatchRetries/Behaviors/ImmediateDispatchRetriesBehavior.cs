using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.Transport;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class ImmediateDispatchRetriesBehavior : Behavior<IDispatchContext>
    {
        static ILog logger = LogManager.GetLogger(typeof(ImmediateDispatchRetriesBehavior));
        private readonly AsyncPolicy _defaultImmediateRetryPolicy;
        private readonly ResiliencePipeline _defaultImmediateRetryResiliencePipeline;

        private readonly AsyncPolicy _defaultBatchRetryPolicy;
        private readonly ResiliencePipeline _defaultBatchRetryResiliencePipeline;

        public ImmediateDispatchRetriesBehavior(IReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultImmediateDispatchRetryPolicy, out _defaultImmediateRetryPolicy);
            readOnlySettings.TryGet(Constants.DefaultImmediateDispatchRetryResiliencePipeline, out _defaultImmediateRetryResiliencePipeline);

            if (_defaultImmediateRetryPolicy != null && _defaultImmediateRetryResiliencePipeline != null)
            {
                logger.Warn("Both a default immediate dispatch retry policy and an immediate dispatch retry resilience strategy are configured. Only the immediate dispatch retry resilience strategy will be used.");
            }

            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultBatchRetryPolicy);
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryResiliencePipeline, out _defaultBatchRetryResiliencePipeline);
            
            if (_defaultBatchRetryPolicy != null && _defaultBatchRetryResiliencePipeline != null)
            {
                logger.Warn("Both a default batch dispatch retry policy and a batch dispatch retry resilience strategy are configured. Only the batch dispatch retry resilience strategy will be used.");
            }
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            var isIsolatedConsistency = context.Operations.All(op => op.RequiredDispatchConsistency == DispatchConsistency.Isolated);
            return isIsolatedConsistency ? HandleIsolatedConsistency(context, next) : HandleDefaultConsistency(context, next);
        }

        Task HandleIsolatedConsistency(IDispatchContext context, Func<Task> next)
        {
            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            if (overrides.ImmediateDispatchResiliencePipelineOverride != null)
            {
                return overrides.ImmediateDispatchResiliencePipelineOverride.ExecuteAsync(_=> new ValueTask(next()), context.CancellationToken).AsTask();
            }
            
            if (overrides.ImmediateDispatchPolicyOverride != null)
            {
                return overrides.ImmediateDispatchPolicyOverride.ExecuteAsync(next);
            }
            
            if (_defaultImmediateRetryResiliencePipeline != null)
            {
                return _defaultImmediateRetryResiliencePipeline.ExecuteAsync(_=>new ValueTask(next()), context.CancellationToken).AsTask();
            }

            if (_defaultImmediateRetryPolicy != null)
            {
                return _defaultImmediateRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }

        Task HandleDefaultConsistency(IDispatchContext context, Func<Task> next)
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

            if (_defaultBatchRetryResiliencePipeline != null)
            {
                return _defaultBatchRetryResiliencePipeline.ExecuteAsync(_=>new ValueTask(next()), context.CancellationToken).AsTask();
            }

            if (_defaultBatchRetryPolicy != null)
            {
                return _defaultBatchRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}