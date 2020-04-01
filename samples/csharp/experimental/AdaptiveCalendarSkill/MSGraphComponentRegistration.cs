using System.Collections.Generic;
using ExtensionsLib.Actions.MSGraph;
using ExtensionsLib.Input;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Newtonsoft.Json;

namespace BotProject
{
    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public virtual IEnumerable<DeclarativeType> GetDeclarativeTypes()
        {
            // Actions
            yield return new DeclarativeType<CancelEvent>(CancelEvent.DeclarativeType);
            yield return new DeclarativeType<CreateEvent>(CreateEvent.DeclarativeType);
            yield return new DeclarativeType<CreateOnlineMeeting>(CreateOnlineMeeting.DeclarativeType);
            yield return new DeclarativeType<DeclineEvent>(DeclineEvent.DeclarativeType);
            yield return new DeclarativeType<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            yield return new DeclarativeType<GetContacts>(GetContacts.DeclarativeType);
            yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            yield return new DeclarativeType<UpdateEvent>(UpdateEvent.DeclarativeType);
            yield return new DeclarativeType<EventDateTimeInput>(EventDateTimeInput.DeclarativeType);
        }

        public virtual IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, Stack<string> paths)
        {
            yield break;
        }
    }
}