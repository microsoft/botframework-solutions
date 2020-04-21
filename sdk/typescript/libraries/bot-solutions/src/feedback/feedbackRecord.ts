/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Activity } from 'botframework-schema';

export class FeedbackRecord {
    /**
     * Gets or sets the activity for which feedback was requested.
     * The activity for which feedback was requested.
     */
    public request?: Activity;

    /**
     * Gets or sets feedback value submitted by user.
     * Feedback value submitted by user.
     */
    public feedback?: string;

    /**
     * Gets or sets free-form comment submitted by user.
     * Free-form comment submitted by user.
     */
    public comment?: string;

    /**
     * Gets or sets tag for categorizing feedback.
     * Tag for categorizing feedback.
     */
    public tag?: string;
}
