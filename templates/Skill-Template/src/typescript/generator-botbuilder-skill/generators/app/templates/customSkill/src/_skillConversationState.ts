// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DialogState } from 'botbuilder-dialogs';

/**
 * Here is the documentation of the <%=skillConversationStateNameClass%> class
 */
export interface <%=skillConversationStateNameClass%> extends DialogState {
    token: string;
    //PENDING search about skillProjectNameLU class
    // tslint:disable-next-line:no-any
    luisResult: any; //<%=skillConversationStateNameClass%>LU;

    clear(): void;
}
