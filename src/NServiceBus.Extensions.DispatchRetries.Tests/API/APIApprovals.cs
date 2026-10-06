using NUnit.Framework;
using PublicApiGenerator;
using System.Runtime.CompilerServices;

namespace NServiceBus.Extensions.DispatchRetries.Tests.API
{
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve_API()
        {
            var publicApi = typeof(DispatchRetriesEndpointConfigurationExtensions).Assembly.GeneratePublicApi(options:null);

            Approver.Verify(publicApi.Replace(".git", ""));
        }
    }
}
