﻿using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

namespace NServiceBus.Extensions.DispatchRetries.Behaviors
{
    class DispatchRetriesOverridesBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            context.Extensions.Set(Constants.IncomingMessage, true);
            context.Extensions.Set(Constants.Overrides, new DispatchRetriesOverrides());

            return next();
        }
    }
}