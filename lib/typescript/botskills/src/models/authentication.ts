/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface IScopeManifest {
    resourceAppId: string;
    resourceAccess: IResourceAccess[];
}

export interface IResourceAccess {
    id: string;
    // tslint:disable-next-line:no-reserved-keywords
    type: string;
}

export interface IAzureAuthSetting {
    etag: string;
    id: string;
    kind: string;
    location: string;
    name: string;
    properties: {
        clientId: string;
        clientSecret: string;
        parameters: {
            key: string;
            value: string;
        }[];
        provisioningState: string;
        scopes: string;
        serviceProviderDisplayName: string;
        serviceProviderId: string;
        settingId: string;
    };
    resourceGroup: string;
    sku: string;
    tags: string;
    // tslint:disable-next-line:no-reserved-keywords
    type: string;
}

export interface IAppSettingOauthConnection {
    oauthConnections: IOauthConnection[];
    microsoftAppId: string;
    microsoftAppPassword: string;
}

export interface IOauthConnection {
    name: string;
    provider: string;
}

export interface IAppShowReplyUrl {
    replyUrls: string[];
}