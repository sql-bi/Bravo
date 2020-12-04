using Sqlbi.Bravo.UI.DataModel;
using Sqlbi.Bravo.UI.Views;
using Sqlbi.Bravo.UI.Framework.ViewModels;
using System;

namespace Sqlbi.Bravo.UI.ViewModels
{
    internal class TabItem : BaseViewModel
    {
        private BiConnectionType connectionType;

        private TabItem()
        {
        }

        public static TabItem Create()
        {
            return new TabItem
            {
                ConnectionType = BiConnectionType.UnSelected,
                ContentPageSource = typeof(SelectConnectionType),
            };
        }

        public string Header
        {
            get
            {
                switch (ConnectionType)
                {
                    case BiConnectionType.ConnectedPowerBiDataset:
                        return $"{ConnectionName} - powerbi.com";

                    case BiConnectionType.ActivePowerBiWindow:
                    case BiConnectionType.VertipaqAnalyzerFile:
                        return ConnectionName;

                    case BiConnectionType.DemoMode:
                        return $"Demo data";

                    default: return " ";  // Empty string is treated as null by WinUI control and so shows FullName
                }
            }
        }

        private Type _contentPageSource;

        public Type ContentPageSource
        {
            get
            {
                return _contentPageSource;
            }

            set
            {
                SetProperty(ref _contentPageSource, value);
            }
        }

        public string ConnectionName { get; set; }

        public BiConnectionType ConnectionType
        {
            get => connectionType;
            set
            {
                if (SetProperty(ref connectionType, value))
                {
                    OnPropertyChanged(nameof(Header));
                    //   OnPropertyChanged(nameof(Icon));
                }
            }
        }

        public bool IsTabClosable { get; set; } = false;

        public override string ToString()
        {
            return Header;
        }
    }
}
