using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    public class DispatchRetriesBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public DispatchRetriesBehavior(AsyncPolicy defaultRetryPolicy)
        {
            _defaultRetryPolicy = defaultRetryPolicy;
        }

        public override Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
        {
            return _defaultRetryPolicy.ExecuteAsync(next);
        }
    }
}