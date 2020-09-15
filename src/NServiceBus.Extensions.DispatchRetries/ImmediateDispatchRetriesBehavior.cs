using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Transport;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    public class ImmediateDispatchRetriesBehavior : Behavior<IDispatchContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public ImmediateDispatchRetriesBehavior(AsyncPolicy defaultRetryPolicy)
        {
            _defaultRetryPolicy = defaultRetryPolicy;
        }

        public override Task Invoke(IDispatchContext context, Func<Task> next)
        {
            var isImmediate = context.Operations.All(op => op.RequiredDispatchConsistency == DispatchConsistency.Isolated);
            return isImmediate ? _defaultRetryPolicy.ExecuteAsync(next) : next();
        }
    }
}