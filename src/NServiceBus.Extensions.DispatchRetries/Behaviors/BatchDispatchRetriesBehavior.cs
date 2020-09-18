using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    public class BatchDispatchRetriesBehavior : Behavior<IBatchDispatchContext>
    {
        private readonly AsyncPolicy _defaultRetryPolicy;

        public BatchDispatchRetriesBehavior(ReadOnlySettings readOnlySettings)
        {
            readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out _defaultRetryPolicy);
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            if (context.Extensions.TryGet(Constants.BatchDispatchRetryPolicy, out AsyncPolicy retryPolicy))
            {
                return retryPolicy.ExecuteAsync(next);
            }

            if (_defaultRetryPolicy!= null)
            {
                return _defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}