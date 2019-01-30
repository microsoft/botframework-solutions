// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog } from 'botbuilder-dialogs';
import { <%=responsesNameClass%> } from './<%=responsesNameFile%>';

export class <%=dialogNameClass%> extends ComponentDialog {
    // Fields
    private static readonly RESPONDER: <%=responsesNameClass%> = new <%=responsesNameClass%>();

    constructor() {
        super(<%=dialogNameClass%>.name);
        this.initialDialogId = <%=dialogNameClass%>.name;
    }   
}     
