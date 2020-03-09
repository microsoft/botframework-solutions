using System.Collections.Generic;
using BotProject.Actions.MSGraph;
using BotProject.Input;
using Microsoft.Bot.Builder.Dialogs.Declarative;

namespace BotProject
{
    public class MSGraphComponentRegistration : ComponentRegistration
    {
        public override IEnumerable<TypeRegistration> GetTypes()
        {
            // Actions
            yield return new TypeRegistration<CancelEvent>(CancelEvent.DeclarativeType);
            yield return new TypeRegistration<CreateEvent>(CreateEvent.DeclarativeType);
            yield return new TypeRegistration<CreateOnlineMeeting>(CreateOnlineMeeting.DeclarativeType);
            yield return new TypeRegistration<DeclineEvent>(DeclineEvent.DeclarativeType);
            yield return new TypeRegistration<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            yield return new TypeRegistration<GetContacts>(GetContacts.DeclarativeType);
            yield return new TypeRegistration<GetEvents>(GetEvents.DeclarativeType);
            yield return new TypeRegistration<GetPeople>(GetPeople.DeclarativeType);
            yield return new TypeRegistration<UpdateEvent>(UpdateEvent.DeclarativeType);
            yield return new TypeRegistration<EventDateTimeInput>(EventDateTimeInput.DeclarativeType);
        }
    }
}