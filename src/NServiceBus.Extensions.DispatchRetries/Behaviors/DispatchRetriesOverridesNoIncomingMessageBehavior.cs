using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class DispatchRetriesOverridesNoIncomingMessageBehavior : Behavior<IRoutingContext>
    {
        public override Task Invoke(IRoutingContext context, Func<Task> next)
        {
            if (!context.Extensions.TryGet<bool>(Constants.IncomingMessage, out _))
            {
                context.Extensions.Set(Constants.Overrides, new DispatchRetriesOverrides());
            }

            return next();
        }
    }
}