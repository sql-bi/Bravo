using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Services
{
    internal class ApplicationInstanceHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;

        public ApplicationInstanceHostedService(IHostApplicationLifetime applicationLifetime)
        {
            _applicationLifetime = applicationLifetime;

            _applicationLifetime.ApplicationStarted.Register(OnStarted);
            _applicationLifetime.ApplicationStopping.Register(OnStopping);
            _applicationLifetime.ApplicationStopped.Register(OnStopped);
        }

        private void OnStarted()
        {
            Trace.WriteLine($"::Bravo:INF:ApplicationInstanceHostedService:OnStarted");
        }

        private void OnStopping()
        {
            Trace.WriteLine($"::Bravo:INF:ApplicationInstanceHostedService:OnStopping");
        }

        private void OnStopped()
        {
            Trace.WriteLine($"::Bravo:INF:ApplicationInstanceHostedService:OnStopped");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Trace.WriteLine($"::Bravo:INF:ApplicationInstanceHostedService:StartAsync");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Hosting shutdown process
            // https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host#hosting-shutdown-process

            Trace.WriteLine($"::Bravo:INF:ApplicationInstanceHostedService:StopAsync");
            return Task.CompletedTask;
        }
    }
}
