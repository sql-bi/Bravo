/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ProblemDetails } from '../controllers/host';
import { Utils } from '../helpers/utils';
import { i18n } from '../model/i18n'; 
import { strings } from './strings';

export enum AppProblem {
    None = 0,
    TOMDatabaseDatabaseNotFound = 101,
    TOMDatabaseUpdateFailed = 102,
    TOMDatabaseUpdateConflictMeasure = 103,
    TOMDatabaseUpdateErrorMeasure = 104,
    PBIDesktopProcessNotFound = 200,
    PBIDesktopSSASProcessNotFound = 300,
    PBIDesktopSSASConnectionNotFound = 301,
    PBIDesktopSSASDatabaseUnexpectedCount = 302,
    SignInMsalExceptionOccurred = 400,
    SignInMsalTimeoutExpired = 401,
    VpaxFileContainsCorruptedData = 500,
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
    traceId: string;
    readonly requestAborted: boolean;
    readonly requestTimedout: boolean;

    constructor(type: AppErrorType, message?: string, code?: number, traceId?: string) {
        if (!message)
            message = i18n(strings.errorUnspecified);
        
        if (message.slice(-1) != ".") message += ".";
        this.message = message;

        this.type = type;
        this.code = code;
        this.requestAborted = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Aborted);
        this.requestTimedout = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Timeout);

        this.traceId = traceId;
    }

    toString() {
        return `${ i18n(strings.error) }${ this.code ? ` ${this.type != AppErrorType.Managed ? "HTTP/" : "" }${ this.code }` : "" }: ${ this.message }${ this.traceId ? `\n${ i18n(strings.traceId) }: ${this.traceId}` : ""}`;
    }

    static InitFromProblem(problem: ProblemDetails, message?: string) {

        let errorType;
        let errorCode;
        let errorMessage;
        let traceId;

        if (problem.status == Utils.ResponseStatusCode.BadRequest) {
            errorType = AppErrorType.Managed;
            errorCode = Number(problem.instance);
            const key = `error${AppProblem[errorCode]}`;
            errorMessage = i18n(key in strings ? (<any>strings)[key] : strings.errorUnhandled);
            traceId = problem.traceId;
            
        } else {
            errorCode = problem.status;

            if (problem.status == Utils.ResponseStatusCode.NotAuthorized) {
                errorType = AppErrorType.Auth;
                errorMessage = i18n(strings.errorNotAuthorized);
                
            } else if (problem.status == Utils.ResponseStatusCode.Aborted) {
                errorType = AppErrorType.Abort;
                errorMessage = i18n(strings.errorAborted);

            } else if (problem.status == Utils.ResponseStatusCode.Timeout) {
                errorType = AppErrorType.Abort;
                errorMessage = i18n(strings.errorTimeout);

            } else {
                errorType = AppErrorType.Response;
                errorMessage = problem.title ? problem.title : i18n(strings.errorUnspecified);
            }
        }

        if (message) errorMessage = message;

        return new AppError(errorType, errorMessage, errorCode, traceId);
    }

    static InitFromInternalError(code: number, message?: string) {
        return AppError.InitFromProblem({ status: Utils.ResponseStatusCode.BadRequest, instance: String(code) }, message);
    }

    static InitFromResponseError(code: number, message?: string) {
        return AppError.InitFromProblem({ status: code }, message);
    }

    static initFromError(error: Error) {
        return new AppError(AppErrorType.Generic, error.message);
    }
}