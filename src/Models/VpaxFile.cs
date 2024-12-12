
namespace Sqlbi.Bravo.Models
{
    public class VpaxFile
    {
        public VpaxFile(Stream stream)
        {
            Stream = stream;
        }

        public Stream Stream { get; }
    }

    public sealed class OvpaxFile : VpaxFile
    {
        public OvpaxFile(Stream stream, string dictionaryPath)
            : base(stream)
        {
            DictionaryPath = dictionaryPath;
        }

        public string DictionaryPath { get; }
    }
}
