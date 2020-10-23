using System.Diagnostics;
using System.Linq;
using System.Management;

namespace Sqlbi.Bravo.Core.Management
{
    internal static class ManagementHelpers
    {
        public static Process GetParent(this Process process)
        {
            var queryString = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = { process.Id }";

            using var query = new ManagementObjectSearcher(queryString);
            using var collection = query.Get();
            using var item = collection.OfType<ManagementObject>().Single();
           
            var parentProcessId = (int)(uint)item["ParentProcessId"];
            return Process.GetProcessById(parentProcessId);
        }
    }
}
