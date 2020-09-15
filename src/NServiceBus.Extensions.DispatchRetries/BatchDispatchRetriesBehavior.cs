using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries
{
    public class BatchDispatchRetriesBehavior : Behavior<IBatchDispatchContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public BatchDispatchRetriesBehavior(AsyncPolicy defaultRetryPolicy)
        {
            _defaultRetryPolicy = defaultRetryPolicy;
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            return _defaultRetryPolicy.ExecuteAsync(next);
        }
    }
}