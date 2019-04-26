/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export enum SkillExceptionType {
    /**
     * Access Denied when calling external APIs
     */
    APIAccessDenied,
    /**
     * Account Not Activated when calling external APIs
     */
    AccountNotActivated,
    /**
     * Other types of exceptions
     */
    Other
}

export class SkillException extends Error {
    public exceptionType: SkillExceptionType;
    public innerException: Error;

    constructor(exceptionType: SkillExceptionType, message: string, innerException: Error) {
        super(message);
        this.exceptionType = exceptionType;
        this.innerException = innerException;
    }
}
