using System;
using System.Threading.Tasks;
using static SimpleExec.Command;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public static class DockerCompose
    {
        public static async Task Up(Func<Task<bool>> statusChecker = null)
        {
            await RunAsync("docker", "compose up -d", workingDirectory: AppDomain.CurrentDomain.BaseDirectory);
            await RunAsync("docker", "ps -a");

            statusChecker ??= () => Task.FromResult(true);
            while (!await statusChecker())
            {
                await Task.Delay(500);
            }
        }

        public static void Down()
        {
            Run("docker", "compose down", workingDirectory: AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}