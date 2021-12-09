using Sqlbi.Bravo.Models;
using System.IO;

namespace Sqlbi.Bravo.Services
{
    public interface IAnalyzeModelService
    {
        TabularDatabase GetDatabaseFromVpax(Stream vpax);
    }
}