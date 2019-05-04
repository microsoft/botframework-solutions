/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ReceiveRequest } from 'microsoft-bot-protocol';
import { IRouteContext } from './routeContext';
import { IRouteAction } from './routerAction';
import { IRouteTemplate } from './routeTemplate';

export class Router {
    private readonly routes: IRouteTemplate[];
    private readonly root: TrieNode;

    constructor(routes: IRouteTemplate[]) {
        this.routes = routes;
        this.root = new TrieNode('root');
        this.compile();
    }

    public route(request: ReceiveRequest): IRouteContext|undefined {
        let found: boolean = true;
        let path: string = request.Path;
        // MISSING: if (Uri.IsWellFormedUriString(path, UriKind.Absolute))
        if (path.startsWith('/')) {
            path = path.substr(1);
        }

        const parts: string[] = path.split('/');
        const initial: TrieNode|undefined = this.root.tryGetNext(request.Verb);
        if (initial) {
            let current: TrieNode = initial;
            const routeData: Map<string, Object> = new Map();
            parts.forEach((part: string) => {
                const next: TrieNode|undefined = current.tryGetNext(part);
                if (next) {
                    // found an exact match, keep going
                    current = next;
                } else {
                    // check for variables and continue
                    const variables: TrieNode[] = current.getVariables();
                    if (variables.length > 0) {
                        // PENDING: we are only going to allow 1 variable for now
                        current  = variables[0];
                        routeData.set(current.variableName, part);
                    } else {
                        found = false;
                    }
                }
            });
            if (found && current.action) {
                return {
                    request: request,
                    routerData: routeData,
                    action: current.action
                };
            }
        }

        return undefined;
    }

    private compile(): void {
        this.routes.forEach((route: IRouteTemplate) => {
            let path: string = route.path;
            if (path.startsWith('/')) {
                path = path.substr(1);
            }

            const parts: string[] = path.split('/');

            const methodName: TrieNode = this.root.add(route.method);
            let current: TrieNode = methodName;
            parts.forEach((part: string) => {
                current = current.add(part);
            });

            if (current.action !== undefined) {
                throw new Error('Invalid operation.  Route already exists.');
            }

            current.action = route.action;
        });
    }

}

class TrieNode {
    public readonly value: string;
    public readonly isVariable: boolean;
    public readonly variableName: string;
    public action?: IRouteAction;
    public next: Map<string, TrieNode>;

    constructor(value: string) {
        this.value = value;
        this.isVariable = value.startsWith('{') && value.endsWith('}');
        this.next = new Map();
        if (this.isVariable) {
            this.variableName = value.substring(1, value.length - 2);
        } else {
            this.variableName = '';
        }
    }

    public add(value: string): TrieNode {
        const next: TrieNode = this.next.get(value) || new TrieNode(value);
        if (!this.next.has(value)) {
            this.next.set(value, next);
        }

        return next;
    }

    public tryGetNext(value: string): TrieNode|undefined {
        return this.next.get(value);
    }

    public getVariables(): TrieNode[] {
        return Array.from(this.next.values())
            .filter((x: TrieNode) => x.isVariable);
    }
}
