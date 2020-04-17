/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    AuthProviderCallback,
    Client,
    GraphError } from '@microsoft/microsoft-graph-client';
import { User } from '@microsoft/microsoft-graph-types';

export class GraphClient {
    private readonly token: string;

    public constructor(token: string) {
        this.token = token;
    }

    public async getMe(): Promise<User> {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        return new Promise((resolve, reject): Promise<any> => {
            const client: Client = this.getAuthenticatedClient();

            return client
                .api('/me')
                .select('displayName')
                .get((err: GraphError, res: User): void => {
                    if (err !== undefined) {
                        reject(err);
                    }

                    resolve(res);
                });
        });
    }

    private getAuthenticatedClient(): Client {
        return Client.init({
            authProvider: (done: AuthProviderCallback): void => {
                done(undefined, this.token);
            }
        });
    }
}
