/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export type WorkItemFunc = () => Promise<void>;

export interface IBackgroundTaskQueue {
    queueBackgroundWorkItem(workItem: WorkItemFunc): void;
    dequeue(): Promise<WorkItemFunc>;
}

export class BackgroundTaskQueue implements IBackgroundTaskQueue {
    private readonly workItems: WorkItemFunc[];

    constructor() {
        this.workItems = [];
    }

    public queueBackgroundWorkItem(workItem: WorkItemFunc): void {
        if (!workItem) { throw new Error('Missing parameter.  workItem is required'); }

        this.workItems.push(workItem);
    }

    public dequeue(): Promise<WorkItemFunc> {
        const workItem: WorkItemFunc|undefined = this.workItems.pop();

        if (workItem) {
            return Promise.resolve(workItem);
        }

        return Promise.resolve(() => Promise.resolve());
    }
}
