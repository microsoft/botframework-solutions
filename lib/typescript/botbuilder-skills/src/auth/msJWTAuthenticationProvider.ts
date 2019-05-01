/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

// import {  } from 'jsonwebtoken';
// import {  } from 'jwks-rsa';
// https://github.com/auth0/node-jsonwebtoken#jwtverifytoken-secretorpublickey-options-callback
// https://github.com/auth0/node-jwks-rsa
import { IAuthenticationProvider } from "./authenticationProvider";

export class MsJWTAuthenticationProvider implements IAuthenticationProvider {
    private readonly openIdMetadataUrl: string = 'https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration';
    private readonly jwtIssuer: string = 'https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0';

    public authenticate(authHeader: string): Promise<boolean> {
        throw new Error("Method not implemented.");
    }
}
