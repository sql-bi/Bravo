namespace Sqlbi.Bravo.Infrastructure.Extensions.DynamicLinq.Parser
{
    using System;

    public sealed class ParseException : Exception
    {
        private readonly int _position;

        public ParseException(string message, int position)
            : base(message)
        {
            _position = position;
        }

        public int Position => _position;

        public override string ToString()
        {
            return string.Format(Res.ParseExceptionFormat, Message, Position);
        }
    }
}
