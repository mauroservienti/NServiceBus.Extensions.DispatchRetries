using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;
using PublicApiGenerator;
using System.Runtime.CompilerServices;

namespace NServiceBus.Extensions.DispatchRetries.Tests.API
{
    public class APIApprovals
    {
        [Test]
        [UseReporter(typeof(DiffReporter))]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve_API()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(DispatchRetriesEndpointConfigurationExtensions).Assembly, options:null);

            Approvals.Verify(publicApi);
        }
    }
}
