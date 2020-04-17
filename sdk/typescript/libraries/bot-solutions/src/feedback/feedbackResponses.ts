/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { join } from 'path';
import { IResponseIdCollection } from '../responses/responseIdCollection';

export class FeedbackResponses implements IResponseIdCollection {
    public readonly name: string = this.name;
    public static readonly pathToResource: string = join(__dirname, 'resources');
    public static readonly commentPrompt: string = 'CommentPrompt';
    public static readonly commentReceivedMessage: string = 'CommentReceivedMessage';
    public static readonly dismissTitle: string = 'DismissTitle';
    public static readonly feedbackReceivedMessage: string = 'FeedbackReceivedMessage';
}
