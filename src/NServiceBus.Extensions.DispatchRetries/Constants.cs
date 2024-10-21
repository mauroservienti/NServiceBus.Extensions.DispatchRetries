namespace NServiceBus.Extensions.DispatchRetries
{
    static class Constants
    {
        public const string Overrides="nservicebus-extensions-dispatchretries-overrides";
        public const string DefaultBatchDispatchRetryPolicy = "nservicebus-extensions-dispatchretries-default-batch-dispatch-retry-policy";
        public const string DefaultBatchDispatchRetryResiliencePipeline = "nservicebus-extensions-dispatchretries-default-batch-dispatch-retry-resilience-pipeline";
        public const string DefaultImmediateDispatchRetryPolicy = "nservicebus-extensions-dispatchretries-default-immediate-dispatch-retry-policy";
        public const string DefaultImmediateDispatchRetryResiliencePipeline = "nservicebus-extensions-dispatchretries-default-immediate-dispatch-retry-resilience-pipeline";
        public const string IncomingMessage = "nservicebus-extensions-dispatchretries-incoming-message";
    }
}