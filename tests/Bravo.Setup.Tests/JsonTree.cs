using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bravo.Setup.Tests
{
    /// <summary>
    /// Reads JSON as the reader of .NET Framework sees it: the root element is the value, each element in an
    /// object a member, and the attribute <c>type</c> of an element says what its value is.
    /// </summary>
    internal static class JsonTree
    {
        public static XElement Parse(byte[] json)
        {
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(json, XmlDictionaryReaderQuotas.Max))
            {
                return XElement.Load(reader);
            }
        }

        public static XElement Parse(string json)
        {
            return Parse(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// The value of a member of type string, or <see langword="null"/> when it is missing or of another type.
        /// </summary>
        public static string ReadString(XElement value, params string[] path)
        {
            var member = value;
            foreach (var name in path)
            {
                member = member?.Element(name);
            }

            return member != null && (string)member.Attribute("type") == "string" ? member.Value : null;
        }
    }
}
