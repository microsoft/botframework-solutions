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

    public constructor() {
        this.queueExecutor = new PQueue({
            concurrency: 1,
            autoStart: true
        });
    }

    public async queueBackgroundWorkItem(workItem: WorkItemFunc): Promise<void> {
        if (workItem === undefined) { throw new Error('Missing parameter.  workItem is required'); }

        await this.queueExecutor.add(workItem);
    }
}
