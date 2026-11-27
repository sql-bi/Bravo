using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bravo.Setup.Tests.Telemetry
{
    /// <summary>
    /// The ingestion endpoint of Application Insights, as a test stands for it on the loopback address: it accepts
    /// every item, answers each request on a connection of its own and keeps the bodies as they arrived. No item of
    /// a test reaches a real resource.
    /// </summary>
    internal sealed class TestIngestion : IDisposable
    {
        private static readonly char[] s_lineSeparator = { '\n' };
        private static readonly string[] s_headerSeparator = { "\r\n" };

        private readonly TcpListener _listener;
        private readonly List<string> _bodies = new List<string>();
        private readonly Task _accepting;

        public TestIngestion()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            ConnectionString = ConnectionStringFor(((IPEndPoint)_listener.LocalEndpoint).Port);
            _accepting = Task.Run(() => Accept());
        }

        /// <summary>
        /// The connection string of a resource whose ingestion is this endpoint.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// The items of every body, in the order they arrived: a body has one JSON object on each line.
        /// </summary>
        public IReadOnlyList<XElement> Items
        {
            get
            {
                lock (_bodies)
                {
                    return _bodies
                        .SelectMany(body => body.Split(s_lineSeparator, StringSplitOptions.RemoveEmptyEntries))
                        .Select(JsonTree.Parse)
                        .ToList();
                }
            }
        }

        /// <summary>
        /// The connection string of a resource of no one whose ingestion is the port of the loopback address.
        /// </summary>
        public static string ConnectionStringFor(int port)
        {
            return $"InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=http://127.0.0.1:{port}";
        }

        public void Dispose()
        {
            _listener.Stop();
            _accepting.Wait();
        }

        private void Accept()
        {
            try
            {
                while (true)
                {
                    var client = _listener.AcceptTcpClient();
                    _ = Task.Run(() => Answer(client));
                }
            }
            catch (Exception exception) when (exception is SocketException || exception is ObjectDisposedException || exception is InvalidOperationException)
            {
                // The test has ended, and the listener was stopped.
            }
        }

        // The body is kept before the answer, which the sender waits for.
        private void Answer(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var body = ReadBody(stream);
                    lock (_bodies)
                    {
                        _bodies.Add(body);
                    }

                    var items = body.Split(s_lineSeparator, StringSplitOptions.RemoveEmptyEntries).Length;
                    var answer = $"{{\"itemsReceived\":{items},\"itemsAccepted\":{items},\"errors\":[]}}";
                    var response = Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {answer.Length}\r\nConnection: close\r\n\r\n{answer}");
                    stream.Write(response, 0, response.Length);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is SocketException || exception is ObjectDisposedException)
            {
                // The sender went away.
            }
        }

        private static string ReadBody(NetworkStream stream)
        {
            var head = new List<byte>();
            while (!EndsWithBlankLine(head))
            {
                var value = stream.ReadByte();
                if (value < 0)
                {
                    throw new IOException("The connection ended before the head of the request.");
                }

                head.Add((byte)value);
            }

            var lines = Encoding.ASCII.GetString(head.ToArray()).Split(s_headerSeparator, StringSplitOptions.None);
            const string Prefix = "Content-Length:";
            var lengthLine = lines.FirstOrDefault(line => line.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase));
            var length = lengthLine == null ? 0 : int.Parse(lengthLine.Substring(Prefix.Length).Trim(), CultureInfo.InvariantCulture);

            var body = new byte[length];
            var read = 0;
            while (read < length)
            {
                var count = stream.Read(body, read, length - read);
                if (count == 0)
                {
                    throw new IOException("The connection ended before the body of the request.");
                }

                read += count;
            }

            return Encoding.UTF8.GetString(body);
        }

        private static bool EndsWithBlankLine(List<byte> head)
        {
            var count = head.Count;
            return count >= 4 && head[count - 4] == '\r' && head[count - 3] == '\n' && head[count - 2] == '\r' && head[count - 1] == '\n';
        }
    }
}
