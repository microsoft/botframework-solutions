/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ActivityTypes } from 'botframework-schema';

/**
 * A class with dialog arguments for a SkillDialog
 */
export class SkillDialogArgs {

    /**
     * Gets or sets the ID of the skill to invoke.
     * @param skillId
     */
    public skillId: string = '';

    /**
     * Gets or sets the ActivityTypes to send to the skill.
     * @param activityType
     */
    public activityType: string = ActivityTypes.Message;
    
    /**
     * Gets or sets the name of the event or invoke activity to send to the skill (this value is ignored for other types of activities).
     * @param name
     */
    public name: string = ''; 

    /**
     * Gets or sets the value property for the activity to send to the skill.
     * @param value
     */
    public value: Object = new Object(); 

    /**
     * Gets or sets the text property for the 'ActivityTypes.Message' to send to the skill (ignored for other types of activities).
     * @param text
     */
    public text: string = ''; 

}
