/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

// Extends verify definitions to be compatible with callback handler to resolve signingKey
declare module 'jsonwebtoken' {
    export type signingKeyResolver = (headers: jwks.Headers, cb: (err: Error, signingKey: string) => void) => void;

    export function verify(
        token: string,
        secretOrPublicKey: signingKeyResolver,
        callback?: VerifyCallback
    ): void;
}

import { HttpOperationResponse, ServiceClient } from '@azure/ms-rest-js';
import { signingKeyResolver, verify } from 'jsonwebtoken';
import * as jwks from 'jwks-rsa';
import { IAuthenticationProvider } from './authenticationProvider';

export class MsJWTAuthenticationProvider implements IAuthenticationProvider {
    private readonly openIdMetadataUrl: string = 'https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration';
    private readonly jwtIssuer: string = 'https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0';
    private readonly httpClient: ServiceClient;
    private readonly appId: string;

    constructor(appId: string) {
        this.httpClient = new ServiceClient();
        this.appId = appId;
    }

    public async authenticate(authHeader: string): Promise<boolean> {
        try {
            const token: string = authHeader.includes(' ') ? authHeader.split(' ')[1] : authHeader;

            const jwksInfo: HttpOperationResponse = await this.httpClient.sendRequest({
                method: 'GET',
                url: this.openIdMetadataUrl
            });

            const jwksUri: string = <string>jwksInfo.parsedBody.jwks_uri;
            const jwksClient: jwks.JwksClient = jwks({ jwksUri: jwksUri });

            const getKey: signingKeyResolver = (headers: jwks.Headers, cb: (err: Error, signingKey: string) => void): void => {
                jwksClient.getSigningKey(headers.kid, (err: Error, key: jwks.Jwk) => {
                    cb(err, key.publicKey || key.rsaPublicKey || '');
                });
            };

            // tslint:disable-next-line:typedef
            const decoder: Promise<{[key: string]: Object}> = new Promise((resolve, reject) => {
                verify(token, getKey, (err: Error, decodedObj: Object) => {
                    if (err) { reject(err); }
                    const result: {[key: string]: Object} = <{[key: string]: Object}>decodedObj;
                    resolve(result);
                });
            });

            const decoded: { [key: string]: Object } = await decoder;

            return decoded.appid === this.appId;
        } catch (error) {
            return false;
        }
    }
}
