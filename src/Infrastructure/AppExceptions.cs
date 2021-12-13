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
    public class BravoSignInMsalException: BravoException
    {
        public string MsalErrorCode { get; init; }

        public BravoSignInMsalException(MsalException inner)
            : base(inner.Message, inner)
        {
            MsalErrorCode = inner.ErrorCode;
        }
    }

    [Serializable]
    public class BravoSignInTimeoutException : BravoException
    {
    }

    [Serializable]
    public class BravoPBIDesktopReportNotFoundException : BravoException
    {
        public BravoPBIDesktopReportNotFoundException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class BravoPBICloudDatasetNotFoundException : BravoException
    {
        public BravoPBICloudDatasetNotFoundException(string message)
            : base(message)
        {
        }
    }
}
