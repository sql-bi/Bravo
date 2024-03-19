namespace Sqlbi.Bravo.Infrastructure
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents unexpected errors that occur during application execution.
    /// </summary>
    /// <remarks>
    /// This class does not inherit from <see cref="BravoException"/>. <see cref="BravoUnexpectedException"/> will be tracked in telemetry as an unhandled exception and should not be handled at the application level.
    /// </remarks>
    [Serializable]
    public class BravoUnexpectedException : Exception
    {
        public BravoUnexpectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Throws an <see cref="BravoUnexpectedArgumentNullException"/> if <paramref name="argument"/> is null. See https://github.com/dotnet/runtime/issues/48573
        /// </summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
        {
            if (argument is null)
            {
                ThrowUnexpectedArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="BravoUnexpectedInvalidOperationException"/> if <paramref name="condition"/> is false.
        /// </summary>
        /// <param name="condition">The Boolean condition to be evaluated.</param>
        /// <param name="conditionExpression">The expression of the parameter with which <paramref name="condition"/> corresponds.</param>
        public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression("condition")] string? conditionExpression = null)
        {
            if (condition == false)
            {
                ThrowUnexpectedInvalidOperationException(conditionExpression);
            }
        }

        [DoesNotReturn]
        private static void ThrowUnexpectedArgumentNullException(string? paramName) => throw new BravoUnexpectedArgumentNullException(paramName);

        [DoesNotReturn]
        private static void ThrowUnexpectedInvalidOperationException(string? conditionExpression) => throw new BravoUnexpectedInvalidOperationException($"Condition failed '{conditionExpression}'");
    }

    public class BravoUnexpectedPolicyViolationException : BravoUnexpectedException
    {
        public BravoUnexpectedPolicyViolationException(string policyName)
            : base(message: policyName)
        {
        }
    }

    public class BravoUnexpectedArgumentNullException : ArgumentNullException
    {
        public BravoUnexpectedArgumentNullException(string? paramName)
            : base(paramName)
        {
        }
    }

    public class BravoUnexpectedInvalidOperationException : InvalidOperationException
    {
        public BravoUnexpectedInvalidOperationException(string? message)
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

        public string? ProblemDetail => Message.NullIfWhiteSpace();

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

    public enum BravoProblem
    {
        [JsonPropertyName("None")]
        None = 0,

        /// <summary>
        /// A <see cref="OperationCanceledException"/> or <see cref="TaskCanceledException"/> was thrown, an HTTP request was aborted or the user  cancelled a long-running operation.
        /// This BravoProblem is a placehoder since the response message is never sent back to the user/UI due to the aborted request.
        /// </summary>
        [JsonPropertyName("OperationCancelled")]
        OperationCancelled = 1,

        /// <summary>
        /// A connection problem arises between the server and current application
        /// </summary>
        [JsonPropertyName("AnalysisServicesConnectionFailed")]
        AnalysisServicesConnectionFailed = 10,

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
        /// An error occurred while importing the VPAX file
        /// </summary> 
        [JsonPropertyName("VpaxFileImportError")]
        VpaxFileImportError = 500,

        /// <summary>
        /// An error has occurred while exporting the VPAX file
        /// </summary> 
        [JsonPropertyName("VpaxFileExportError")]
        VpaxFileExportError = 501,

        /// <summary>
        /// You are not connected to the Internet
        /// </summary> 
        [JsonPropertyName("NetworkError")]
        NetworkError = 600,

        /// <summary>
        /// An exception occurred while exporting data to file
        /// </summary> 
        [JsonPropertyName("ExportDataFileError")]
        ExportDataFileError = 700,

        /// <summary>
        /// An exception occurred while executing the DAX template engine
        /// </summary> 
        [JsonPropertyName("ManageDateTemplateError")]
        ManageDateTemplateError = 800,

        /// <summary>
        /// An exception occurred while executing the template development APIs
        /// </summary> 
        [JsonPropertyName("TemplateDevelopmentError")]
        TemplateDevelopmentError = 900,

        /// <summary>
        /// An error occurred while obfuscating the VPAX file
        /// </summary> 
        [JsonPropertyName("VpaxObfuscationError")]
        VpaxObfuscationError = 1000,

        /// <summary>
        /// An error occurred while deobfuscating the VPAX file
        /// </summary> 
        [JsonPropertyName("VpaxDeobfuscationError")]
        VpaxDeobfuscationError = 1001,
    }
}
