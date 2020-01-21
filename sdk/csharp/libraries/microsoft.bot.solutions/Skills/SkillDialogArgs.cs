// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Skills
{
    /// <summary>
    /// A class with dialog arguments for a <see cref="SkillDialog"/>.
    /// </summary>
    public class SkillDialogArgs
    {
        /// <summary>
        /// Gets or sets the ID of the skill to invoke.
        /// </summary>
        /// <value>
        /// Skill Id.
        /// </value>
        public string SkillId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ActivityTypes"/> to send to the skill.
        /// </summary>
        /// <value>
        /// Activity type.
        /// </value>
        public string ActivityType { get; set; } = ActivityTypes.Message;

        /// <summary>
        /// Gets or sets the name of the event or invoke activity to send to the skill (this value is ignored for other types of activities).
        /// </summary>
        /// <value>
        /// Name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value property for the activity to send to the skill.
        /// </summary>
        /// <value>
        /// Value.
        /// </value>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the text property for the <see cref="ActivityTypes.Message"/> to send to the skill (ignored for other types of activities).
        /// </summary>
        /// <value>
        /// Text.
        /// </value>
        public string Text { get; set; }
    }
}
