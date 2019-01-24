// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import {
    Activity,
    ActivityTypes } from 'botbuilder';
import {
    ComponentDialog,
    Dialog,
    DialogContext,
    DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { ActivityExtensions } from '../../extensions/activityExtensions';

export abstract class RouterDialog extends ComponentDialog {
    constructor(dialogId: string) { super(dialogId); }

    protected onBeginDialog(dc: DialogContext): Promise<DialogTurnResult> {
        return this.onContinueDialog(dc);
    }

    protected async onContinueDialog(dc: DialogContext): Promise<DialogTurnResult> {
        const activity: Activity = dc.context.activity;
        if (ActivityExtensions.isStartActivity(activity)) {
            await this.onStart(dc);
        }
        switch (activity.type) {
            case ActivityTypes.Message: {
                if (activity.value !== undefined) {
                    await this.onEvent(dc);
                } else if (typeof activity.text !== undefined && activity.text) {
                    const result: DialogTurnResult<any> = await dc.continueDialog();
                    switch (result.status) {
                        case DialogTurnStatus.empty: {
                            await this.route(dc);
                            break;
                        }
                        case DialogTurnStatus.complete: {
                            await this.complete(dc);

                            // End active dialog.
                            await dc.endDialog();
                        }
                        default:
                    }
                }
                break;
            }
            case ActivityTypes.Event: {
                await this.onEvent(dc);
                break;
            }
            default: {
                await this.onSystemMessage(dc);
            }
        }

        return Dialog.EndOfTurn;
    }

    protected abstract route(innerDC: DialogContext): Promise<void>;

    protected complete(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    protected onEvent(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    protected onSystemMessage(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }

    protected onStart(innerDC: DialogContext): Promise<void> {
        return Promise.resolve();
    }
}
