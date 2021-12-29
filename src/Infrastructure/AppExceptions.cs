using Microsoft.Identity.Client;
using System;

namespace Sqlbi.Bravo.Infrastructure
{
    [Serializable]
    public class BravoException : Exception
    {
        public BravoException()
        {
        }

        public BravoException(string message)
            : base(message)
        {
        }

        public BravoException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    [Serializable]
    public class BravoUnexpectedException : BravoException
    {
        public BravoUnexpectedException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class SignInMsalException: BravoException
    {
        public string MsalErrorCode { get; init; }

        public SignInMsalException(MsalException inner)
            : base(inner.Message, inner)
        {
            MsalErrorCode = inner.ErrorCode;
        }
    }

    [Serializable]
    public class SignInTimeoutException : BravoException
    {
    }

    [Serializable]
    public class TOMDatabaseNotFoundException : BravoException
    {
        public TOMDatabaseNotFoundException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class TOMDatabaseOutOfSyncException : BravoException
    {
        public TOMDatabaseOutOfSyncException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class TOMDatabaseUpdateException : BravoException
    {
        public TOMDatabaseUpdateException(string message)
            : base(message)
        {
        }
    }
}
