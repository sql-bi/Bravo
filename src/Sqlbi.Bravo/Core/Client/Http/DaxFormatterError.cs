namespace Sqlbi.Bravo.Core.Client.Http
{
    internal class DaxFormatterError
    {
        public int Line { get; set; }

        public int Column { get; set; }

        public string Message { get; set; }
    }
}
