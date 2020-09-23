using System;
using System.Net.Http;
using System.Threading.Tasks;
using static SimpleExec.Command;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public static class DockerCompose
    {
        public static async Task Up(Func<Task<bool>> statusChecker = null)
        {
            Run("docker-compose", "up -d", workingDirectory: AppDomain.CurrentDomain.BaseDirectory);
            Run("docker", "ps -a");

            statusChecker ??= () => Task.FromResult(true);
            while (!await statusChecker())
            {
                await Task.Delay(500);
            }
        }

        public static void Down()
        {
            Run("docker-compose", "down", workingDirectory: AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}