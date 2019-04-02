// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { DialogState } from 'botbuilder-dialogs';

export interface IOnboardingState extends DialogState {
    name: string;
    location: string;
}
