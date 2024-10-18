using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    class DispatchRetriesOverrides
    {
        public AsyncPolicy BatchDispatchPolicyOverride { get; set; }
        public AsyncPolicy ImmediateDispatchPolicyOverride { get; set; }
        public ResiliencePipeline BatchDispatchResiliencePipelineOverride { get; set; }
        public ResiliencePipeline ImmediateDispatchResiliencePipelineOverride { get; set; }
    }
}