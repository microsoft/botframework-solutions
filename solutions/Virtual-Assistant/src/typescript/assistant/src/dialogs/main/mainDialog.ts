// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog,
        DialogTurnResult,
        WaterfallDialog,
        WaterfallStepContext} from 'botbuilder-dialogs';
import { MainResponses } from './mainResponses';

export class MainDialog extends ComponentDialog {

    // Declare here the type of properties
    private static readonly RESPONDER: MainResponses = new MainResponses();

    // Initialize the dialog class properties
    constructor() {
        super(MainDialog.name);
        this.initialDialogId = MainDialog.name;

        // tslint:disable-next-line:no-any
        const value: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult<any>>)[] = [
            this.end.bind(this)
        ];

   // Add here the waterfall dialog
        this.addDialog(new WaterfallDialog(this.initialDialogId, value));
    }

    // Add here end dialog waterfall.
    private async end(sc: WaterfallStepContext): Promise<DialogTurnResult> {

        return sc.endDialog(<boolean> sc.result);
    }
}
