/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { default as PQueue } from 'p-queue';

export type WorkItemFunc = () => Promise<void>;

export interface IBackgroundTaskQueue {
    queueBackgroundWorkItem(workItem: WorkItemFunc): void;
}

export class BackgroundTaskQueue implements IBackgroundTaskQueue {
    private readonly queueExecutor: PQueue;

    constructor() {
        this.queueExecutor = new PQueue({
            concurrency: 1,
            autoStart: true
        });
    }

    public queueBackgroundWorkItem(workItem: WorkItemFunc): void {
        if (!workItem) { throw new Error('Missing parameter.  workItem is required'); }

        this.queueExecutor.add(workItem);
    }
}
