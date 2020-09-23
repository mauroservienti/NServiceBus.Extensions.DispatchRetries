using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Extensions.DispatchRetries.AcceptanceTests.Transport.Helpers;

namespace NServiceBus
{
    /// <summary>
    /// Configuration options for the learning transport.
    /// </summary>
    public static class AcceptanceTestTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures the location where message files are stored.
        /// </summary>
        /// <param name="transportExtensions">The transport extensions to extend.</param>
        /// <param name="path">The storage path.</param>
        public static void StorageDirectory(this TransportExtensions<AcceptanceTestTransport> transportExtensions, string path)
        {
            Guard.AgainstNullAndEmpty(nameof(path), path);
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);
            PathChecker.ThrowForBadPath(path, "StorageDirectory");

            transportExtensions.GetSettings().Set(AcceptanceTestTransportInfrastructure.StorageLocationKey, path);
        }

        /// <summary>
        /// Allows messages of any size to be sent.
        /// </summary>
        /// <param name="transportExtensions">The transport extensions to extend.</param>
        public static void NoPayloadSizeRestriction(this TransportExtensions<AcceptanceTestTransport> transportExtensions)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);

            transportExtensions.GetSettings().Set(AcceptanceTestTransportInfrastructure.NoPayloadSizeRestrictionKey, true);
        }

        public static void FailDispatchOnce(this TransportExtensions<AcceptanceTestTransport> transportExtensions)
        {
            Guard.AgainstNull(nameof(transportExtensions), transportExtensions);

            transportExtensions.GetSettings().Set(AcceptanceTestTransportInfrastructure.FailDispatchOnceKey, true);
        }
    }
}
