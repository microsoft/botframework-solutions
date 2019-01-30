// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog } from 'botbuilder-dialogs';
import { <%=dialogNameClass%>Responses } from './<%=dialogFileName%>Responses';

export class <%=dialogNameClass%>Dialog extends ComponentDialog {
    // Fields
    private static readonly RESPONDER:<%=dialogNameClass%>Responses = new <%=dialogNameClass%>Responses();

    constructor() {
        super();
        this.initialDialogId = <%=dialogNameClass%>Dialog.name;
    }   
}     
