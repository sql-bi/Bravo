namespace Sqlbi.Bravo.Infrastructure.Services.ExportData
{
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Models.ExportData;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;

    internal class ExportDataJobMap<T> where T : class, IPBIDataModel<T>
    {
        private readonly ConcurrentDictionary<T, ExportDataJob> _jobs;
        
        public ExportDataJobMap()
        {
            _jobs = new ConcurrentDictionary<T, ExportDataJob>();
        }

        public ExportDataJob AddNew(T datamodel)
        {
            var job = new ExportDataJob();

            var jobAdded = _jobs.TryAdd(datamodel, job);
            
            // Each IPBIDataModel is not allowed to start more than a single export job at a time
            BravoUnexpectedException.Assert(jobAdded);
            
            job.SetRunning();

            return job;
        }

        public bool TryGet(T datamodel, [MaybeNullWhen(false)] out ExportDataJob job)
        {
            return _jobs.TryGetValue(datamodel, out job);
        }

        public void Remove(T datamodel)
        {
            var jobRemoved = _jobs.TryRemove(datamodel, out _);

            BravoUnexpectedException.Assert(jobRemoved);
        }
    }
}
