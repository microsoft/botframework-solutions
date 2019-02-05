// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    AuthProviderCallback,
    Client,
    GraphError } from '@microsoft/microsoft-graph-client';
import { User } from '@microsoft/microsoft-graph-types';

export class GraphClient {
    private readonly TOKEN: string;

    constructor(TOKEN: string) {
        this.TOKEN = TOKEN;
    }

    public getMe(): Promise<User> {
        // tslint:disable-next-line:no-any
        return new Promise((resolve: (value?: User | PromiseLike<User> | undefined) => any, reject: (reason?: any) => void): void => {
            const client: Client = this.getAuthenticatedClient();
            client.api('/me')
                  .select('displayName')
            .get((err: GraphError, res: User) => {
                if (err) { return reject(err); }

                return resolve(res);
            });
        });
    }

    private getAuthenticatedClient(): Client {
        return Client.init({
            authProvider: (done: AuthProviderCallback): void => {
                done(undefined, this.TOKEN);
            }
        });
    }
}
