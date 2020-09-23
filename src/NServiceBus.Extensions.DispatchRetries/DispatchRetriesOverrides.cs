using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    class DispatchRetriesOverrides
    {
        public AsyncPolicy BatchDispatchPolicyOverride { get; set; }
        public AsyncPolicy ImmediateDispatchPolicyOverride { get; set; }
    }
}