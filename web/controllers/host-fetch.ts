/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import type { ProblemDetails } from '../generated/bravo';

/** Where the API of the host is, and the token that every request must carry. */
export interface HostConnection {
    address: string
    token: string
}

/** An error answer of the host: its status and, when the host wrote one, its problem details. */
export class HostProblem extends Error {

    constructor(readonly status: number, readonly problem?: ProblemDetails) {
        super(problem?.title ?? `HTTP ${status}`);
        this.name = "HostProblem";
    }
}

let connection: HostConnection = { address: "", token: "" };

/** Tells the generated client which host to call. */
export function connectHost(hostConnection: HostConnection) {
    connection = hostConnection;
}

/**
 * The one function that every call of the generated client goes through. It adds the address and the token
 * of the host, passes on what the caller set, such as the signal that aborts the request, and throws a
 * HostProblem for an error answer.
 */
export async function hostFetch<T>(url: string, options: RequestInit): Promise<T> {

    const headers = new Headers(options.headers);
    headers.set("Authorization", `Bearer ${connection.token}`);

    const response = await fetch(`${connection.address}${url}`, { ...options, headers });
    if (!response.ok)
        throw new HostProblem(response.status, await readProblem(response));

    // An answer without a body, or with an empty one
    const text = await response.text();
    return <T>(text ? JSON.parse(text) : undefined);
}

async function readProblem(response: Response): Promise<ProblemDetails | undefined> {
    try {
        return await response.json();
    } catch (ignore) {
        return undefined;
    }
}
