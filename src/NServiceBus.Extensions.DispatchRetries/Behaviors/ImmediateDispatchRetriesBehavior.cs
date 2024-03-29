﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.Transport;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class ImmediateDispatchRetriesBehavior : Behavior<IDispatchContext>
    {
        private readonly AsyncPolicy _defaultImmediateRetryPolicy;
        private readonly AsyncPolicy _defaultBatchRetryPolicy;

        public ImmediateDispatchRetriesBehavior(IReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultImmediateDispatchRetryPolicy, out _defaultImmediateRetryPolicy);
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultBatchRetryPolicy);
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            var isIsolatedConsistency = context.Operations.All(op => op.RequiredDispatchConsistency == DispatchConsistency.Isolated);
            return isIsolatedConsistency ? HandleIsolatedConsistency(context, next) : HandleDefaultConsistency(context, next);
        }

        Task HandleIsolatedConsistency(IDispatchContext context, Func<Task> next)
        {
            var overrides = context.Extensions.Get<DispatchRetriesOverrides>(Constants.Overrides);
            if (overrides.ImmediateDispatchPolicyOverride != null)
            {
                return overrides.ImmediateDispatchPolicyOverride.ExecuteAsync(next);
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
            if (overrides.BatchDispatchPolicyOverride != null)
            {
                return overrides.BatchDispatchPolicyOverride.ExecuteAsync(next);
            }

            if (_defaultBatchRetryPolicy != null)
            {
                return _defaultBatchRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}