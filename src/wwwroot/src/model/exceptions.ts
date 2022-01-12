/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { ProblemDetails } from '../controllers/host';
import { Utils } from '../helpers/utils';
import { i18n } from './i18n';
import { strings } from './strings';

export enum AppProblem {
    None = 0,
    TOMDatabaseDatabaseNotFound = 101,
    TOMDatabaseUpdateFailed = 102,
    TOMDatabaseUpdateConflictMeasure = 103,
    PBIDesktopProcessNotFound = 200,
    PBIDesktopSSASProcessNotFound = 300,
    PBIDesktopSSASConnectionNotFound = 301,
    PBIDesktopSSASDatabaseUnexpectedCount = 302,
    SignInMsalExceptionOccurred = 400,
    SignInMsalTimeoutExpired = 401,
}

export enum AppErrorType {
    Managed,
    Response,
    Abort,
    Auth,
    Generic
}

export class AppError extends Error {

    type: AppErrorType;
    code: number;
    traceId: string;
    readonly requestAborted: boolean;
    readonly requestTimedout: boolean;

    constructor(type: AppErrorType, message?: string, code?: number, traceId?: string) {
        if (!message)
            message = i18n(strings.errorUnspecified);
        
        if (message.slice(-1) != ".") message += ".";
        super(message);

        this.type = type;

        this.code = code;
        this.requestAborted = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Aborted);
        this.requestTimedout = (type == AppErrorType.Abort && code == Utils.ResponseStatusCode.Timeout);

        this.traceId = traceId;
    }

    static InitFromProblem(problem: ProblemDetails) {

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

        return new AppError(errorType, errorMessage, errorCode, traceId);
    }

    static InitFromResponseError(code: number, message?: string) {

        return AppError.InitFromProblem({
            status: code,
            title: message
        });
    }

    static initFromError(error: Error) {
        return new AppError(AppErrorType.Generic, error.message);
    }
}