using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using NServiceBus.Transport;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    public class ImmediateDispatchRetriesBehavior : Behavior<IDispatchContext>
    {
        private readonly ReadOnlySettings _readOnlySettings;

        public ImmediateDispatchRetriesBehavior(ReadOnlySettings readOnlySettings)
        {
            _readOnlySettings = readOnlySettings;
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            var isImmediate = context.Operations.All(op => op.RequiredDispatchConsistency == DispatchConsistency.Isolated);
            if (isImmediate && _readOnlySettings.TryGet("default-immediate-dispatch-retry-policy", out AsyncPolicy defaultRetryPolicy))
            {
                return defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}