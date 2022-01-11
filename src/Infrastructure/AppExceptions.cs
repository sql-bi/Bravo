using Sqlbi.Bravo.Infrastructure.Extensions;
using System;

namespace Sqlbi.Bravo.Infrastructure
{
    [Serializable]
    public class BravoUnexpectedException : Exception
    {
        public BravoUnexpectedException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class BravoException : Exception
    {
        public BravoProblem Problem { get; private set; }

        public string? ProblemDetail => Message.NullIfEmpty();

        public string ProblemInstance => $"{ (int)Problem }";

        public BravoException(BravoProblem problem)
            : base(string.Empty)
        {
            Problem = problem;
        }

        public BravoException(BravoProblem problem, string message)
            : base(message)
        {
            Problem = problem;
        }

        public BravoException(BravoProblem problem, string message, Exception inner)
            : base(message, inner)
        {
            Problem = problem;
        }
    }

    [Serializable]
    public class SignInException : BravoException
    {
        public SignInException(BravoProblem problem)
            : base(problem)
        {
        }

        public SignInException(BravoProblem problem, string message, Exception inner)
            : base(problem, message, inner)
        {
        }
    }

    [Serializable]
    public class TOMDatabaseException : BravoException
    {
        public TOMDatabaseException(BravoProblem problem)
            : base(problem)
        {
        }

        public TOMDatabaseException(BravoProblem problem, string message)
            : base(problem, message)
        {
        }
    }

    public enum BravoProblem
    {
        None = 0,

        /// <summary>
        /// TOM database does not exists in the collection or the user does not have admin rights for it.
        /// </summary> 
        TOMDatabaseDatabaseNotFound = 101,

        /// <summary>
        /// TOM database update failed while saving local changes made on the model tree to the version of the model residing in the database server.
        /// </summary> 
        TOMDatabaseUpdateFailed = 102,

        /// <summary>
        /// TOM measure update request conflict with current state of the target resource
        /// </summary> 
        TOMDatabaseUpdateConflictMeasure = 103,

        /// <summary>
        /// PBIDesktop process is no longer running or the identifier might be expired.
        /// </summary> 
        PBIDesktopProcessNotFound = 200,

        /// <summary>
        /// PBIDesktop SSAS instance process not found.
        /// </summary> 
        PBIDesktopSSASProcessNotFound = 300,

        /// <summary>
        /// PBIDesktop SSAS instance connection not found.
        /// </summary> 
        PBIDesktopSSASConnectionNotFound = 301,

        /// <summary>
        /// PBIDesktop SSAS instance database not found.
        /// </summary> 
        PBIDesktopSSASDatabaseUnexpectedCount = 302,

        /// <summary>
        /// 
        /// </summary> 
        SignInMsalExceptionOccurred = 400,

        /// <summary>
        /// Sign-in request was canceled because the configured timeout period elapsed prior to completion of the operation
        /// </summary> 
        SignInMsalTimeoutExpired = 401,
    }
}
