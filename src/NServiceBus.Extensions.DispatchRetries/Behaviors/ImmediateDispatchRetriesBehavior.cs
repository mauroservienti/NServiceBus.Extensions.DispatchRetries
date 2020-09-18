using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.Transport;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    public class ImmediateDispatchRetriesBehavior : Behavior<IDispatchContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public ImmediateDispatchRetriesBehavior(ReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultImmediateDispatchRetryPolicy, out _defaultRetryPolicy);
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            var isImmediate = context.Operations.All(op => op.RequiredDispatchConsistency == DispatchConsistency.Isolated);
            if (isImmediate && context.Extensions.TryGet(Constants.ImmediateDispatchRetryPolicy, out AsyncPolicy retryPolicy))
            {
                return retryPolicy.ExecuteAsync(next);
            }

            if (isImmediate && _defaultRetryPolicy != null)
            {
                return _defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}