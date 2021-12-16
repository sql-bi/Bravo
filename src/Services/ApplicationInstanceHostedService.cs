using Microsoft.Extensions.Hosting;
using System;
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
        }

        private void OnStopping()
        {
        }

        private void OnStopped()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Hosting shutdown process
            // https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host#hosting-shutdown-process

            return Task.CompletedTask;
        }
    }
}
