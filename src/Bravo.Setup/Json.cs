using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Bravo.Setup
{
    /// <summary>
    /// JSON with the reader and the writer of <c>DataContractJsonSerializer</c>, which .NET Framework has. They see
    /// a document as XML: the root element is the object, each element in it a member, and the attribute
    /// <c>type</c> of an element says what its value is.
    /// </summary>
    internal static class Json
    {
        private const string Root = "root";
        private const string TypeAttribute = "type";
        private const string ObjectType = "object";
        private const string StringType = "string";
        private const string NumberType = "number";

        /// <summary>
        /// A writer of UTF-8 with no byte order mark, which leaves the stream open. It escapes a quote, a backslash,
        /// a control character and a slash with a backslash, and writes any other character as it is.
        /// </summary>
        public static XmlDictionaryWriter CreateWriter(Stream stream, bool indent)
        {
            return JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, ownsStream: false, indent: indent);
        }

        /// <summary>
        /// Starts the object of the document, which <see cref="XmlWriter.WriteEndElement"/> ends.
        /// </summary>
        public static void WriteStartObject(XmlDictionaryWriter writer)
        {
            WriteStartObject(writer, Root);
        }

        /// <summary>
        /// Starts a member whose value is an object, which <see cref="XmlWriter.WriteEndElement"/> ends.
        /// </summary>
        public static void WriteStartObject(XmlDictionaryWriter writer, string name)
        {
            writer.WriteStartElement(name);
            writer.WriteAttributeString(TypeAttribute, ObjectType);
        }

        public static void WriteString(XmlDictionaryWriter writer, string name, string value)
        {
            writer.WriteStartElement(name);
            writer.WriteAttributeString(TypeAttribute, StringType);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        public static void WriteNumber(XmlDictionaryWriter writer, string name, int value)
        {
            writer.WriteStartElement(name);
            writer.WriteAttributeString(TypeAttribute, NumberType);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        /// <summary>
        /// The object that a stream holds, or <see langword="null"/> when the stream holds no JSON or another value.
        /// </summary>
        public static XElement ReadObject(Stream stream)
        {
            try
            {
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(stream, XmlDictionaryReaderQuotas.Max))
                {
                    // An empty stream has no element, for which XElement throws another exception.
                    if (reader.MoveToContent() != XmlNodeType.Element)
                    {
                        return null;
                    }

                    var root = XElement.Load(reader);
                    return HasType(root, ObjectType) ? root : null;
                }
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// The value of a member of an object when it is a string, or <see langword="null"/>.
        /// </summary>
        public static string ReadString(XElement value, string name)
        {
            var member = value.Element(name);
            return member != null && HasType(member, StringType) ? member.Value : null;
        }

        private static bool HasType(XElement element, string type)
        {
            return (string)element.Attribute(TypeAttribute) == type;
        }
    }
}
