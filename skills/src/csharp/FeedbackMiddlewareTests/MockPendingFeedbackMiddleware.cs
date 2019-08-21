using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using ToDoSkill.Utilities.FeedbackMiddleware;

namespace FeedbackMiddlewareTest
{
    public class MockPendingFeedbackMiddleware : IMiddleware
    {
        public MockPendingFeedbackMiddleware(ConversationState convState, FeedbackRecord record)
        {
            ConvState = convState;
            Record = record;
        }

        public static FeedbackRecord MockFeedbackRecordWithoutComments { get; set; } = new FeedbackRecord()
        {
            Tag = "test",
            Request = new Activity()
            {
                Type = ActivityTypes.Message,
                Text = "foo"
            },
            Response = "bar",
            Feedback = null,
            Comments = null
        };

        public static FeedbackRecord MockFeedbackRecordWithComments { get; set; } = new FeedbackRecord()
        {
            Tag = "test",
            Request = new Activity()
            {
                Type = ActivityTypes.Message,
                Text = "foo"
            },
            Response = "bar",
            Feedback = "very good",
            Comments = null
        };

        public ConversationState ConvState { get; set; }

        public FeedbackRecord Record { get; set; }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var accessor = ConvState.CreateProperty<FeedbackRecord>("Feedback");
            await accessor.SetAsync(turnContext, Record);
            await ConvState.SaveChangesAsync(turnContext);
            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
