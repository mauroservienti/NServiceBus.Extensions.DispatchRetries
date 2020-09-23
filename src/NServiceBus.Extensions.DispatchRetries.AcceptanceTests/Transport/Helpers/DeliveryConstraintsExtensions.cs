using System.Collections.Generic;
using System.Linq;
using NServiceBus.DeliveryConstraints;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests.Transport.Helpers
{
    internal static class DeliveryConstraintsExtensions
    {
        internal static bool TryGet<T>(this List<DeliveryConstraint> list, out T constraint) where T : DeliveryConstraint
        {
            constraint = list.OfType<T>().FirstOrDefault();

            return constraint != null;
        }
    }
}