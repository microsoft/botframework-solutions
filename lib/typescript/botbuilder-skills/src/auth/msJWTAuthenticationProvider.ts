/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { verify } from 'jsonwebtoken';
import * as jwks from 'jwks-rsa';
import { JwksClient, Headers, Jwk } from 'jwks-rsa';
import { ServiceClient } from '@azure/ms-rest-js';
import { IAuthenticationProvider } from "./authenticationProvider";

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
            const token = authHeader.includes(' ') ? authHeader.split(' ')[1] : authHeader;

            const jwksInfo = await this.httpClient.sendRequest({
                method: 'GET',
                url: this.openIdMetadataUrl
            });

            const jwksUri: string = <string>jwksInfo.parsedBody.jwks_uri;
            const jwksClient: JwksClient = jwks({ jwksUri: jwksUri });

            const getKey = (headers: Headers, cb: (err: Error, signingKey: string) => void) => {
                jwksClient.getSigningKey(headers['kid'], (err: Error, key: Jwk) => {
                    cb(err, key.publicKey || key.rsaPublicKey || '');
                });
            };

            const decoder: Promise<{[key: string]: Object}> = new Promise((resolve, reject) => {
                verify(token, <any>getKey, (err: Error, decoded: Object) => {
                    if (err) reject(err);
                    const result: {[key: string]: Object} = <{[key: string]: Object}>decoded;
                    resolve(result);
                });
            });

            const decoded: { [key: string]: Object } = await decoder;

            return decoded['appid'] === this.appId;
        } catch (error) {
            return false;
        }
    }
}
