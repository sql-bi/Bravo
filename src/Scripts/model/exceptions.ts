/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ProblemDetails } from '../controllers/host';
import { Utils } from '../helpers/utils';
import { app, logger, telemetry } from '../main';
import { i18n } from '../model/i18n'; 
import { pii } from './pii';
import { strings } from './strings';

export enum AppProblem {
    None = 0,
    AnalysisServicesConnectionFailed = 10,

    TOMDatabaseDatabaseNotFound = 101,
    TOMDatabaseUpdateFailed = 102,
    TOMDatabaseUpdateConflictMeasure = 103,
    TOMDatabaseUpdateErrorMeasure = 104,
    ConnectionUnsupported = 200,
    UserSettingsSaveError = 300,
    SignInMsalExceptionOccurred = 400,
    SignInMsalTimeoutExpired = 401,
    VpaxFileImportError = 500,
    VpaxFileExportError = 501,
    NetworkError = 600,
    ExportDataFileError = 700,
    ManageDateTemplateError = 800,
    TemplateDevelopmentError = 900,          // TODO: Add localized string for this error to avoid reporting strings.errorUnhandled
    VpaxObfuscatorObfuscationError = 1000,   // TODO: Add localized string for this error to avoid reporting strings.errorUnhandled
    VpaxObfuscatorDeobfuscationError = 1001, // TODO: Add localized string for this error to avoid reporting strings.errorUnhandled
}

export enum AppErrorType {
    Managed,
    Response,
    Abort,
    Auth,
    Generic
}

export class AppError {

    type: AppErrorType;
    code: number;
    message: string;
    details: string;
    traceId: string;
    readonly requestAborted: boolean;
    readonly requestTimedout: boolean;

    private constructor(type: AppErrorType, message?: string, code?: number, traceId?: string, details?: string) {
        if (!message)
            message = i18n(strings.errorUnspecified);

        this.message = message;
        this.details = details;

        this.type = type;
        this.code = code;
        this.requestAborted = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Aborted);
        this.requestTimedout = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Timeout);

        this.traceId = traceId;
    }

    toString(anonymize = false, includeTraceId = true, includeVersion = true) {

        let message = (anonymize ? Utils.Obj.anonymize(this.message, pii) : this.message);
        let details = (this.details ? (anonymize ? Utils.Obj.anonymize(this.details, pii) : this.details) : null);

        return `${ i18n(strings.error) }${ this.code ? ` ${this.type != AppErrorType.Managed ? "HTTP/" : "" }${ this.code }` : "" }: ${ message }${ details ? `\n${details}` : "" }${ includeTraceId && this.traceId ? `\n${ i18n(strings.traceId) }: ${this.traceId}` : ""}${ includeVersion ? `\n${i18n(strings.version)}: ${app.currentVersion.toString()}` : ""}`;
    }

    static InitFromProblem(problem: ProblemDetails, message?: string) {

        let errorType;
        let errorCode;
        let errorMessage;
        let errorDetails;

        let traceId = problem.traceId;

        // 400 (Handled)
        if (problem.status == Utils.ResponseStatusCode.BadRequest) { 
            errorType = AppErrorType.Managed;
            errorCode = Number(problem.instance);
            const key = `error${AppProblem[errorCode]}`;
            errorMessage = i18n(key in strings ? (<any>strings)[key] : strings.errorUnhandled);
            errorDetails = problem.detail;

        } else {
            errorCode = problem.status;

            // Not authorized
            if (problem.status == Utils.ResponseStatusCode.NotAuthorized) {
                errorType = AppErrorType.Auth;
                errorMessage = i18n(strings.errorNotAuthorized);
            
            // Aborted
            } else if (problem.status == Utils.ResponseStatusCode.Aborted) {
                errorType = AppErrorType.Abort;
                errorMessage = i18n(strings.errorAborted);

            // Timeout
            } else if (problem.status == Utils.ResponseStatusCode.Timeout) {
                errorType = AppErrorType.Abort;
                errorMessage = i18n(strings.errorTimeout);

            // HTTP error (unhandled)
            } else {
                errorType = AppErrorType.Response;
                errorMessage = problem.title ? problem.title : i18n(strings.errorUnspecified);
                if (errorMessage.trim().slice(-1) != ".") errorMessage += "."; 
                
                errorDetails = problem.detail;
                
            }
        }

        if (message) errorMessage = message;

        return new AppError(errorType, errorMessage, errorCode, traceId, errorDetails);
    }

    static InitFromProblemCode(code: number, message?: string, details?: string) {
        return AppError.InitFromProblem({ status: Utils.ResponseStatusCode.BadRequest, instance: String(code), detail: details }, message);
    }

    //This error is not generated by the host, so it should be tracked if >= 500
    static InitFromResponseStatus(code: number, message?: string, track = true) {

        let problem: ProblemDetails = { status: code };
        if (message) problem.title = message;

        if (track && code >= 500 && telemetry.enabled) {
            problem.traceId = Utils.Text.uuid();
            telemetry.trackError(problem);
        }
        return AppError.InitFromProblem(problem, message);
    }

    static InitFromError(error: Error){
        return AppError.InitFromProblem({ status: Utils.ResponseStatusCode.InternalError, title: error.message });
    }

    static InitFromString(message: string){
        return AppError.InitFromProblem({ status: Utils.ResponseStatusCode.InternalError, title: message });
    }
}