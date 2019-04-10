/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export enum SkillExceptionType {
    APIAccessDenied,
    AccountNotActivated,
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
