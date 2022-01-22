using Sqlbi.Bravo.Infrastructure.Extensions;
using System;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure
{
    /// <summary>
    /// Represents unexpected errors that occur during application execution.
    /// </summary>
    /// <remarks>
    /// <see cref="BravoUnexpectedException"/> will be tracked in telemetry as an unhandled exception and should not be handled at the application level.
    /// ** This class does not inherit from <see cref="BravoException"/> **
    /// </remarks>
    [Serializable]
    public class BravoUnexpectedException : Exception
    {
        public BravoUnexpectedException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Represents the base class for error that occurs during application execution.
    /// </summary>
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

        public BravoException(BravoProblem problem, string message, Exception innerException)
            : base(message, innerException)
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

        public SignInException(BravoProblem problem, string message, Exception innerException)
            : base(problem, message, innerException)
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

        public TOMDatabaseException(BravoProblem problem, string message, Exception innerException)
            : base(problem, message, innerException)
        {
        }
    }

    public enum BravoProblem
    {
        [JsonPropertyName("None")]
        None = 0,

        /// <summary>
        /// TOM database does not exists in the collection or the user does not have admin rights for it.
        /// </summary> 
        [JsonPropertyName("TOMDatabaseDatabaseNotFound")]
        TOMDatabaseDatabaseNotFound = 101,

        /// <summary>
        /// TOM database update failed while saving local changes made on the model tree to the version of the model residing in the database server.
        /// </summary> 
        [JsonPropertyName("TOMDatabaseUpdateFailed")]
        TOMDatabaseUpdateFailed = 102,

        /// <summary>
        /// TOM measure update request conflict with current state of the target resource
        /// </summary> 
        [JsonPropertyName("TOMDatabaseUpdateConflictMeasure")]
        TOMDatabaseUpdateConflictMeasure = 103,

        /// <summary>
        /// TOM measure update request failed because the measure contains DaxFormatter errors
        /// </summary> 
        [JsonPropertyName("TOMDatabaseUpdateErrorMeasure")]
        TOMDatabaseUpdateErrorMeasure = 104,

        /// <summary>
        /// The connection to the requested resource is not supported
        /// </summary> 
        [JsonPropertyName("ConnectionUnsupported")]
        ConnectionUnsupported = 200,

        /// <summary>
        /// An error occurred while saving user settings
        /// </summary> 
        [JsonPropertyName("UserSettingsSaveError")]
        UserSettingsSaveError = 300,

        ///// <summary>
        ///// PBIDesktop process is no longer running or the identifier might be expired.
        ///// </summary> 
        //[JsonPropertyName("PBIDesktopProcessNotFound")]
        //PBIDesktopProcessNotFound = 200,

        ///// <summary>
        ///// PBIDesktop SSAS instance process not found.
        ///// </summary> 
        //[JsonPropertyName("PBIDesktopSSASProcessNotFound")]
        //PBIDesktopSSASProcessNotFound = 300,

        ///// <summary>
        ///// PBIDesktop SSAS instance connection not found.
        ///// </summary> 
        //[JsonPropertyName("PBIDesktopSSASConnectionNotFound")]
        //PBIDesktopSSASConnectionNotFound = 301,

        ///// <summary>
        ///// PBIDesktop SSAS instance contains an unexpected number of databases.
        ///// </summary> 
        //[JsonPropertyName("PBIDesktopSSASDatabaseUnexpectedCount")]
        //PBIDesktopSSASDatabaseUnexpectedCount = 302,

        ///// <summary>
        ///// PBIDesktop SSAS instance does not contain any databases.
        ///// </summary> 
        //[JsonPropertyName("PBIDesktopSSASDatabaseCollectionEmpty")]
        //PBIDesktopSSASDatabaseCollectionEmpty = 303,

        /// <summary>
        /// An error occurs during token acquisition.
        /// </summary> 
        /// <remarks>
        /// Exceptions in MSAL.NET are intended for app developers to troubleshoot and not for displaying to end-users
        /// </remarks>
        [JsonPropertyName("SignInMsalExceptionOccurred")]
        SignInMsalExceptionOccurred = 400,

        /// <summary>
        /// Sign-in request was canceled because the configured timeout period elapsed prior to completion of the operation
        /// </summary> 
        [JsonPropertyName("SignInMsalTimeoutExpired")]
        SignInMsalTimeoutExpired = 401,

        /// <summary>
        /// VPAX file format is not valid or file contains corrupted data
        /// </summary> 
        [JsonPropertyName("VpaxFileContainsCorruptedData")]
        VpaxFileContainsCorruptedData = 500,

        /// <summary>
        /// You are not connected to the Internet
        /// </summary> 
        [JsonPropertyName("NetworkError")]
        NetworkError = 600,
    }
}
