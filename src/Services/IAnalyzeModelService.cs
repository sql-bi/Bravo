using Bravo.Models;
using System.IO;

namespace Sqlbi.Bravo.Services
{
    public interface IAnalyzeModelService
    {
        DatabaseModel GetDatabaseModelFromVpax(Stream stream);
    }
}