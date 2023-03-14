using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Transport;

namespace NServiceBus.AcceptanceTests
{
    static class ConfigurationHelpers
    {
        public static void SetReceiveOnlyTransactionMode(this EndpointConfiguration configuration) => configuration.GetSettings().Get<TransportDefinition>().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
    }
}