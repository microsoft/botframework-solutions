export interface IManifest {
    schema: string;
    id: string;
    name: string;
    description: string;
    version: string;
    luisDictionary: Map<string, string[]>;
    msaAppId: string;
    endpoint: string;
    entries?: [string, any][];
    allowedIntents: string[];
}
