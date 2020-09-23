using NServiceBus.Extensions.DispatchRetries.Behaviors;
using NServiceBus.Features;

namespace NServiceBus.Extensions.DispatchRetries.Features
{
    class DispatchRetriesFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register(new DispatchRetriesOverridesBehavior(), "Dispatch retries behavior to enable policy overrides in the context of incoming messages.");
            context.Pipeline.Register(new DispatchRetriesOverridesNoIncomingMessageBehavior(), "Dispatch retries behavior to enable policy overrides.");
            context.Pipeline.Register(new ImmediateDispatchRetriesBehavior(context.Settings), "Immediate dispatch retries behavior based on Polly.");
            context.Pipeline.Register(new BatchDispatchRetriesBehavior(context.Settings), "Batch dispatch retries behavior based on Polly.");
        }
    }
}