using NServiceBus.Logging;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    class DispatchRetriesOverrides
    {
        static ILog logger = LogManager.GetLogger(typeof(DispatchRetriesOverrides));
        private AsyncPolicy _batchDispatchPolicyOverride;
        private AsyncPolicy _immediateDispatchPolicyOverride;
        private ResiliencePipeline _batchDispatchResiliencePipelineOverride;
        private ResiliencePipeline _immediateDispatchResiliencePipelineOverride;

        public AsyncPolicy BatchDispatchPolicyOverride
        {
            get => _batchDispatchPolicyOverride;
            set
            {
                _batchDispatchPolicyOverride = value;
                LogWarningIfDuplicateBatchDispatchUsage();
            }
        }
        
        public ResiliencePipeline BatchDispatchResiliencePipelineOverride
        {
            get => _batchDispatchResiliencePipelineOverride;
            set
            {
                _batchDispatchResiliencePipelineOverride = value;
                LogWarningIfDuplicateBatchDispatchUsage();
            }
        }
        
        void LogWarningIfDuplicateBatchDispatchUsage()
        {
            if (_batchDispatchPolicyOverride != null && _batchDispatchResiliencePipelineOverride != null)
            {
                logger.Warn("Overrides for the batch dispatch retry policy and batch dispatch retry resilience strategy are configured. Only the batch dispatch retry resilience strategy override will be used.");
            }
        }

        public AsyncPolicy ImmediateDispatchPolicyOverride
        {
            get => _immediateDispatchPolicyOverride;
            set => _immediateDispatchPolicyOverride = value;
        }

        public ResiliencePipeline ImmediateDispatchResiliencePipelineOverride
        {
            get => _immediateDispatchResiliencePipelineOverride;
            set => _immediateDispatchResiliencePipelineOverride = value;
        }
        
        void LogWarningIfDuplicateImmediateDispatchUsage()
        {
            if (_immediateDispatchPolicyOverride != null && _immediateDispatchResiliencePipelineOverride != null)
            {
                logger.Warn("Overrides for the immediate dispatch retry policy and immediate dispatch retry resilience strategy are configured. Only the immediate dispatch retry resilience strategy override will be used.");
            }
        }
    }
}