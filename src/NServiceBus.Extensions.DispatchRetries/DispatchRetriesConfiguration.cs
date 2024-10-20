﻿using System;
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
            DefaultBatchDispatchRetriesResilienceStrategy(defaultBatchAndImmediateDispatchRetriesResiliencePipeline);
            DefaultImmediateDispatchRetriesResilienceStrategy(defaultBatchAndImmediateDispatchRetriesResiliencePipeline);
        }

        public void DefaultImmediateDispatchRetriesPolicy(AsyncPolicy immediateDispatchRetryPolicy)
        {
            ArgumentNullException.ThrowIfNull(immediateDispatchRetryPolicy);

            _configuration.GetSettings().Set(Constants.DefaultImmediateDispatchRetryPolicy, immediateDispatchRetryPolicy);
        }
        
        public void DefaultImmediateDispatchRetriesResilienceStrategy(ResiliencePipeline immediateDispatchRetryResiliencePipeline)
        {
            ArgumentNullException.ThrowIfNull(immediateDispatchRetryResiliencePipeline);

            _configuration.GetSettings().Set(Constants.DefaultImmediateDispatchRetryResiliencePipeline, immediateDispatchRetryResiliencePipeline);
        }

        public void DefaultBatchDispatchRetriesPolicy(AsyncPolicy batchDispatchRetryPolicy)
        {
            ArgumentNullException.ThrowIfNull(batchDispatchRetryPolicy);

            _configuration.GetSettings().Set(Constants.DefaultBatchDispatchRetryPolicy, batchDispatchRetryPolicy);
        }
        
        public void DefaultBatchDispatchRetriesResilienceStrategy(ResiliencePipeline batchDispatchResiliencePipeline)
        {
            ArgumentNullException.ThrowIfNull(batchDispatchResiliencePipeline);

            _configuration.GetSettings().Set(Constants.DefaultBatchDispatchRetryResiliencePipeline, batchDispatchResiliencePipeline);
        }
    }
}