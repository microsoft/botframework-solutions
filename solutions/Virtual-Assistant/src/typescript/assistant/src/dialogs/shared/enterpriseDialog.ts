// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog,
        DialogTurnResult,
        WaterfallDialog,
        WaterfallStepContext} from 'botbuilder-dialogs';

export class EnterpriseDialog extends ComponentDialog {

    // Initialize the dialog class properties
    constructor() {
        super(EnterpriseDialog.name);
        this.initialDialogId = EnterpriseDialog.name;

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
