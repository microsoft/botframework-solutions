using CalendarSkill.Models.Resources;

namespace CalendarSkill.Models
{
    public enum AskParameterType
    {
        /// <summary>
        /// defaut type, ask for details about event
        /// </summary>
        AskForDetail = 0,

        /// <summary>
        /// ask for start time
        /// </summary>
        AskForStartTime = 1,

        /// <summary>
        /// ask for end time
        /// </summary>
        AskForEndTime = 2,

        /// <summary>
        /// ask for start and end time
        /// </summary>
        AskForTime = 3,

        /// <summary>
        /// ask for duration
        /// </summary>http://tianqi.sogou.com/beijing/
        AskForDuration = 4,

        /// <summary>
        /// ask for location
        /// </summary>
        AskForLocation = 5,

        /// <summary>
        /// ask for attenddees
        /// </summary>
        AskForAttendee = 6,

        /// <summary>
        /// ask for title
        /// </summary>
        AskForTitle = 7,

        /// <summary>
        /// ask for content
        /// </summary>
        AskForContent = 8,
    }

    public class AskParameterModel
    {
        public AskParameterModel(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            content = content.ToLower();
            var askParameterTypes = AskParameterTemplate.GetAskParameterTypes(content);
            foreach (var type in askParameterTypes)
            {
                switch (type)
                {
                    case AskParameterType.AskForDetail:
                        {
                            // can set defuat here if needed
                            break;
                        }

                    case AskParameterType.AskForStartTime:
                        {
                            // we do not support answer only start or end time for now
                            // it will return the start and end time always when you ask about any time
                            NeedTime = true;
                            NeedDetail = true;
                            break;
                        }

                    case AskParameterType.AskForEndTime:
                        {
                            // we do not support answer only start or end time for now
                            // it will return the start and end time always when you ask about any time
                            NeedTime = true;
                            NeedDetail = true;
                            break;
                        }

                    case AskParameterType.AskForTime:
                        {
                            NeedTime = true;
                            NeedDetail = true;
                            break;
                        }

                    case AskParameterType.AskForDuration:
                        {
                            NeedDuration = true;
                            NeedDetail = true;
                            break;
                        }

                    case AskParameterType.AskForLocation:
                        {
                            NeedLocation = true;
                            NeedDetail = true;
                            break;
                        }

                    default:
                        {
                            break;
                        }
                }
            }
        }

        // this is for if there is any detail we need to answer.
        // this could be seen as the OR result of all the specific details
        // if it is false, that means the user do not need any specific detail
        public bool NeedDetail { get; set; } = false;

        public bool NeedTime { get; set; } = false;

        public bool NeedDuration { get; set; } = false;

        public bool NeedLocation { get; set; } = false;
    }
}
