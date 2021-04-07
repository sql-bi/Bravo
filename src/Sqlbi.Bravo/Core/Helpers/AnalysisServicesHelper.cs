using Sqlbi.Bravo.Client.AnalysisServicesEventWatcher;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Data.Common;
using System.IO;
using System.Xml;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class AnalysisServicesHelper
    {
        public static WatcherEvent GetEventType(this string xmla)
        {
            if (xmla == null)
                return WatcherEvent.Unknown;

            using var stringReader = new StringReader(xmla);
            using var xmlReader = XmlReader.Create(stringReader);

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    var name = xmlReader.Name;

                    if (nameof(WatcherEvent.Create).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return WatcherEvent.Create;

                    if (nameof(WatcherEvent.Delete).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return WatcherEvent.Delete;

                    if (nameof(WatcherEvent.Alter).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return WatcherEvent.Alter;
                }
            }

            return WatcherEvent.Unknown;
        }
    }
}
