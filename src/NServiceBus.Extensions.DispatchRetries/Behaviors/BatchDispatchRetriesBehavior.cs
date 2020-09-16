using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NServiceBus.Settings;
using Polly;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    public class BatchDispatchRetriesBehavior : Behavior<IBatchDispatchContext>
    {
        private readonly ReadOnlySettings _readOnlySettings;

        public BatchDispatchRetriesBehavior(ReadOnlySettings readOnlySettings)
        {
            _readOnlySettings = readOnlySettings;
        }

        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            if (context.Extensions.TryGet(Constants.BatchDispatchRetryPolicy, out AsyncPolicy retryPolicy))
            {
                return retryPolicy.ExecuteAsync(next);
            }

            if (_readOnlySettings.TryGet(Constants.DefaultBatchDispatchRetryPolicy, out AsyncPolicy defaultRetryPolicy))
            {
                return defaultRetryPolicy.ExecuteAsync(next);
            }

            return next();
        }
    }
}