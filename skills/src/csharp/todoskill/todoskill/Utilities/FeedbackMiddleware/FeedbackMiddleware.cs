using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ToDoSkill.Utilities.FeedbackMiddleware
{
    public class FeedbackMiddleware : IMiddleware
    {
        private static readonly string DefaultFeedbackResponse = "Thanks for your feedback!";
        private static readonly List<FeedbackAction> DefaultFeedbackActions = new List<FeedbackAction>()
        {
            new FeedbackAction()
            {
                Text = "👍 good answer"
            },
            new FeedbackAction()
            {
                Text = "👎 bad answer"
            }
        };

        private static readonly FeedbackAction DefaultDismissAction = new FeedbackAction()
        {
            Text = "dismiss"
        };

        private static readonly string DefaultFreeFormPrompt = "Please add any additional comments in the chat";

        private static readonly PromptFreeForm DefaultPromptFreeForm = new PromptFreeForm()
        {
            UserCanGiveFreeForm = false
        };

        private static readonly string TraceType = "https://www.example.org/schemas/feedback/trace";
        private static readonly string TraceName = "Feedback";
        private static readonly string TraceLabel = "User Feedback";

        public FeedbackMiddleware(ConversationState conversationState, FeedbackOptions options = null)
        {
            ConversationState = conversationState;
            Options = options ?? new FeedbackOptions();
            if (Options.FeedbackActions == null)
            {
                Options.FeedbackActions = DefaultFeedbackActions;
            }

            if (string.IsNullOrEmpty(Options.FeedbackResponse))
            {
                Options.FeedbackResponse = DefaultFeedbackResponse;
            }

            if (Options.DismissAction == null)
            {
                Options.DismissAction = DefaultDismissAction;
            }

            if (string.IsNullOrEmpty(Options.FreeFormPrompt))
            {
                Options.FreeFormPrompt = DefaultFreeFormPrompt;
            }

            if (Options.PromptFreeForm == null)
            {
                Options.PromptFreeForm = DefaultPromptFreeForm;
            }
        }

        private ConversationState ConversationState { get; set; }

        private FeedbackOptions Options { get; set; }

        public static async Task<Activity> CreateFeedbackMessage(ITurnContext turnContext, string text, string tag = null)
        {
            var feedbackOptions = turnContext.TurnState.Get<FeedbackOptions>();
            var conversationState = feedbackOptions.ConversationState;
            var feedbackRecord = new FeedbackRecord()
            {
                Tag = tag,
                Request = turnContext.Activity,
                Response = text,
                Feedback = null,
                Comments = null
            };
            var accessor = conversationState.CreateProperty<FeedbackRecord>("Feedback");
            await accessor.SetAsync(turnContext, feedbackRecord);
            await conversationState.SaveChangesAsync(turnContext);

            var feedbackActions = feedbackOptions.FeedbackActions;
            if (feedbackActions[0].Text != null)
            {
                var actions = feedbackActions.Select(x => x.Text).ToList<string>();
                actions.Add(feedbackOptions.DismissAction.Text);
                return MessageFactory.SuggestedActions(actions, text) as Activity;
            }
            else
            {
                var actions = feedbackActions.Select(x => x.CardAction).ToList<CardAction>();
                actions.Add(feedbackOptions.DismissAction.CardAction);
                return MessageFactory.SuggestedActions(actions, text) as Activity;
            }
        }

        public static async Task SendFeedbackActivity(ITurnContext turnContext, string text, string tag = null)
        {
            var message = await CreateFeedbackMessage(turnContext, text, tag);
            await turnContext.SendActivityAsync(message);
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var feedbackOptions = Options;
            feedbackOptions.ConversationState = ConversationState;

            var currentFeedbackOptions = turnContext.TurnState.Get<FeedbackOptions>();
            if (currentFeedbackOptions == null)
            {
                turnContext.TurnState.Add<FeedbackOptions>(feedbackOptions);
            }
            else
            {
                currentFeedbackOptions = feedbackOptions;
            }

            var record = await GetFeedbackState(turnContext);

            // no pending feedback
            if (record == null)
            {
                await next(cancellationToken).ConfigureAwait(false);
                return;
            }

            // User is giving free-form comments.
            if (record.Feedback != null)
            {
                await UpdateFeedbackState(turnContext, comments: turnContext.Activity.Text);
                await this.StoreFeedback(turnContext);
                await ClearFeedbackState(turnContext);
                return;
            }

            // User is giving feedback selection.
            if (UserGaveFeedback(turnContext))
            {
                await UpdateFeedbackState(turnContext, feedback: turnContext.Activity.Text);

                // User can give free-form comments, give user freeFormPrompt to collect user's comments.
                if (UserCanGiveComments(turnContext))
                {
                    await turnContext.SendActivityAsync(Options.FreeFormPrompt);
                }
                else
                {
                    await StoreFeedback(turnContext);
                    await ClearFeedbackState(turnContext);
                }

                return;
            }

            // User choose dismiss, or did not provide feedback
            await ClearFeedbackState(turnContext);

            // User did not provide feedback, await next.
            if (!UserDismissed(turnContext))
            {
                await next(cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        private async Task<FeedbackRecord> GetFeedbackState(ITurnContext turnContext)
        {
            FeedbackRecord feedback = null;
            try
            {
                await ConversationState.LoadAsync(turnContext);
                feedback = ConversationState.Get(turnContext)["Feedback"].ToObject<FeedbackRecord>();
            }
            catch
            {
            }

            return feedback;
        }

        private async Task ClearFeedbackState(ITurnContext turnContext)
        {
            var accessor = ConversationState.CreateProperty<FeedbackRecord>("Feedback");
            await accessor.SetAsync(turnContext, null);
            await ConversationState.SaveChangesAsync(turnContext);
        }

        private async Task UpdateFeedbackState(ITurnContext turnContext, string feedback = null, string comments = null)
        {
            var accessor = ConversationState.CreateProperty<FeedbackRecord>("Feedback");
            var updatedFeedbackRecord = await GetFeedbackState(turnContext);
            if (feedback != null)
            {
                updatedFeedbackRecord.Feedback = feedback;
            }

            if (comments != null)
            {
                updatedFeedbackRecord.Comments = comments;
            }

            await accessor.SetAsync(turnContext, updatedFeedbackRecord);
            await ConversationState.SaveChangesAsync(turnContext);
        }

        private async Task StoreFeedback(ITurnContext turnContext)
        {
            var record = await GetFeedbackState(turnContext);
            await turnContext.SendActivityAsync(new Activity()
            {
                Type = ActivityTypes.Trace,
                ValueType = TraceType,
                Name = TraceName,
                Label = TraceLabel,
                Value = record
            });
            if (!string.IsNullOrEmpty(Options.FeedbackResponse))
            {
                await turnContext.SendActivityAsync(Options.FeedbackResponse);
            }
        }

        private bool UserCanGiveComments(ITurnContext turnContext)
        {
            var promptFreeFormList = Options.PromptFreeForm.PromptFreeFormAction;
            if (promptFreeFormList != null)
            {
                return promptFreeFormList.Exists(x => SelectThisAction(turnContext.Activity, x));
            }
            else
            {
                return Options.PromptFreeForm.UserCanGiveFreeForm;
            }
        }

        private bool UserGaveFeedback(ITurnContext turnContext)
        {
            if (UserDismissed(turnContext))
            {
                return false;
            }

            return Options.FeedbackActions.Exists(x => SelectThisAction(turnContext.Activity, x));
        }

        private bool UserDismissed(ITurnContext turnContext)
        {
            return SelectThisAction(turnContext.Activity, Options.DismissAction);
        }

        private bool SelectThisAction(Activity activity, FeedbackAction action)
        {
            if (action.Text != null)
            {
                return action.Text == activity.Text;
            }
            else
            {
                return action.CardAction.Text == activity.Text;
            }
        }
    }
}
