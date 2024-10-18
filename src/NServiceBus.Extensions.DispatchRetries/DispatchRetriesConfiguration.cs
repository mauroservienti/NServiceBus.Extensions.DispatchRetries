using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.DispatchRetries;
using NServiceBus.Extensions.DispatchRetries.Features;
using Polly;

namespace NServiceBus
{
    public class DispatchRetriesConfiguration
    {
        private readonly EndpointConfiguration _configuration;

        internal DispatchRetriesConfiguration(EndpointConfiguration configuration)
        {
            _configuration = configuration;

            configuration.EnableFeature<DispatchRetriesFeature>();
        }

        internal DispatchRetriesConfiguration(EndpointConfiguration configuration, AsyncPolicy defaultBatchAndImmediateDispatchRetriesPolicy)
            : this(configuration)
        {
            DefaultBatchDispatchRetriesPolicy(defaultBatchAndImmediateDispatchRetriesPolicy);
            DefaultImmediateDispatchRetriesPolicy(defaultBatchAndImmediateDispatchRetriesPolicy);
        }
        
        internal DispatchRetriesConfiguration(EndpointConfiguration configuration, ResiliencePipeline defaultBatchAndImmediateDispatchRetriesResiliencePipeline)
            : this(configuration)
        {
            DefaultBatchDispatchRetriesResiliencePipeline(defaultBatchAndImmediateDispatchRetriesResiliencePipeline);
            DefaultImmediateDispatchRetriesResiliencePipeline(defaultBatchAndImmediateDispatchRetriesResiliencePipeline);
        }

        [Obsolete("Favor using ResiliencePipeline via the DefaultImmediateDispatchRetriesResiliencePipeline, this method will be treated as an error starting V4 and removed in V5.")]
        public void DefaultImmediateDispatchRetriesPolicy(AsyncPolicy immediateDispatchRetryPolicy)
        {
            if (immediateDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryPolicy));
            }

            _configuration.GetSettings().Set(Constants.DefaultImmediateDispatchRetryPolicy, immediateDispatchRetryPolicy);
        }
        
        public void DefaultImmediateDispatchRetriesResiliencePipeline(ResiliencePipeline immediateDispatchRetryResiliencePipeline)
        {
            if (immediateDispatchRetryResiliencePipeline == null)
            {
                throw new ArgumentNullException(nameof(immediateDispatchRetryResiliencePipeline));
            }

            _configuration.GetSettings().Set(Constants.DefaultImmediateDispatchRetryResiliencePipeline, immediateDispatchRetryResiliencePipeline);
        }

        [Obsolete("Favor using ResiliencePipeline via the DefaultBatchDispatchRetriesResiliencePipeline, this method will be treated as an error starting V4 and removed in V5.")]
        public void DefaultBatchDispatchRetriesPolicy(AsyncPolicy batchDispatchRetryPolicy)
        {
            if (batchDispatchRetryPolicy == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchRetryPolicy));
            }

            _configuration.GetSettings().Set(Constants.DefaultBatchDispatchRetryPolicy, batchDispatchRetryPolicy);
        }
        
        public void DefaultBatchDispatchRetriesResiliencePipeline(ResiliencePipeline batchDispatchResiliencePipeline)
        {
            if (batchDispatchResiliencePipeline == null)
            {
                throw new ArgumentNullException(nameof(batchDispatchResiliencePipeline));
            }

            _configuration.GetSettings().Set(Constants.DefaultBatchDispatchRetryResiliencePipeline, batchDispatchResiliencePipeline);
        }
    }
}