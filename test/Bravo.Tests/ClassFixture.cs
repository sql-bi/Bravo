namespace Bravo.Tests
{
    using System;
    using TabularEditor.TOMWrapper;

    public class TabularModelFixture : IDisposable
    {
        private const string ModelFilePath = @"_data\Models\AdventureWorks.bim";

        private readonly TabularModelHandlerSettings _handlerSettings = new()
        {
            AutoFixup = false,
            PBIFeaturesOnly = false,
            ChangeDetectionLocalServers = false,
            UsePowerQueryPartitionsByDefault = false,
        };

        private readonly TabularModelHandler _handler;

        public TabularModelFixture()
        {
            _handler = new TabularModelHandler(ModelFilePath, _handlerSettings);
        }

        public TabularModelHandler Handler => _handler;

        public void Dispose()
        {
            _handler.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
