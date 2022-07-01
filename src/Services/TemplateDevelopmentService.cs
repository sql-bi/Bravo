namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Services.DaxTemplate;
    using Sqlbi.Bravo.Models.ManageDates;
    using System.Linq;
    using System.Threading;

    public interface ITemplateDevelopmentService
    {
        void CreateWorkspace(string path, DateConfiguration configuration, CancellationToken cancellationToken);
    }

    internal class TemplateDevelopmentServiceService : ITemplateDevelopmentService
    {
        private readonly DaxTemplateManager _templateManager;

        public TemplateDevelopmentServiceService()
        {
            _templateManager = new DaxTemplateManager();
        }

        public void CreateWorkspace(string path, DateConfiguration configuration, CancellationToken cancellationToken)
        {
            var packages = _templateManager.GetPackages();
            var configurations = packages.Select(DateConfiguration.CreateFrom).ToList();
        }

    }
}