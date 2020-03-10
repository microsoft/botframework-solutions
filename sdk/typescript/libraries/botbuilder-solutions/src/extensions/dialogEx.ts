
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { Dialog, DialogState, DialogSet, DialogContext, DialogTurnResult, DialogTurnStatus } from 'botbuilder-dialogs';
import { TurnContext, StatePropertyAccessor } from 'botbuilder';

export namespace DialogEx {
    export async function run(dialog: Dialog, turnContext: TurnContext, accessor: StatePropertyAccessor<DialogState>): Promise<void> {
        const dialogSet = new DialogSet(accessor);
        dialogSet.telemetryClient = dialog.telemetryClient;
        dialogSet.add(dialog);

        const dialogContext: DialogContext = await dialogSet.createContext(turnContext);
        const results: DialogTurnResult = await dialogContext.continueDialog();
        if (results.status === DialogTurnStatus.empty) {
            await dialogContext.beginDialog(dialog.id);
        }
    }
}
