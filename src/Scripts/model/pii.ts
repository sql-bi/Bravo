/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

// Personal identifiable information - Used by anonymization

const userPii = [
    "userPrincipalName",
    "username",
    "emailAddress"
];

const databasePii = [
    "workspaceId",
    "workspaceName",
    "workspaceObjectId",
    "serverName",
    "databaseName",
    "dbName",
    "owner",
    "creatorUser"
];

export const pii = [...userPii, ...databasePii];