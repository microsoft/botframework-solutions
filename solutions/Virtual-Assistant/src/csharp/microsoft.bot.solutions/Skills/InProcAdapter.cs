using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Skills
{
    /// <summary>
    /// We want to invoke skills "in process" rather than invoking through DirectLine/Connector which his heavyweight and overlays additional security
    /// BUT we want to preserve standard BF communication protocols so leverage the Adapter pattern to interact with Skills.
    /// </summary>
    public class InProcAdapter : BotAdapter
    {
        private readonly Queue<Activity> queuedActivities = new Queue<Activity>();

        public InProcAdapter()
            : base()
        {
        }

        public delegate void MessageReceivedHandler();

        public async Task ProcessActivity(Activity activity, BotCallbackHandler callback = null)
        {
            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, default(CancellationToken));
            }
        }

        public new InProcAdapter Use(IMiddleware middleware)
        {
            base.Use(middleware);

            return this;
        }

        public override async Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentNullException(nameof(botId));
            }

            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            using (var context = new TurnContext(this, reference.GetContinuationActivity()))
            {
                await RunPipelineAsync(context, callback, cancellationToken);
            }
        }

        public Activity GetNextReply()
        {
            lock (queuedActivities)
            {
                if (queuedActivities.Count > 0)
                {
                    return queuedActivities.Dequeue();
                }
            }

            return null;
        }

        public List<Activity> GetReplies()
        {
            var replies = new List<Activity>();

            lock (queuedActivities)
            {
                if (queuedActivities.Count > 0)
                {
                    var count = queuedActivities.Count;
                    for (var i = 0; i < count; ++i)
                    {
                        replies.Add(queuedActivities.Dequeue());
                    }
                }
            }

            return replies;
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext context, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();

            foreach (var activity in activities)
            {
                if (string.IsNullOrEmpty(activity.Id))
                {
                    activity.Id = Guid.NewGuid().ToString("n");
                }

                if (activity.Timestamp == null)
                {
                    activity.Timestamp = DateTime.UtcNow;
                }

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The BotFrameworkAdapter and Console adapter implement this
                    // hack directly in the POST method. Replicating that here
                    // to keep the behavior as close as possible to facillitate
                    // more realistic tests.
                    var delayMs = (int)activity.Value;
                    await Task.Delay(delayMs);
                }
                else
                {
                    lock (queuedActivities)
                    {
                        queuedActivities.Enqueue(activity);
                    }
                }

                responses.Add(new ResourceResponse(activity.Id));
            }

            return responses.ToArray();
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken)
        {
            // TODO - Validate if we need this
            throw new NotImplementedException();
        }

        public override Task DeleteActivityAsync(ITurnContext context, ConversationReference reference, CancellationToken cancellationToken)
        {
            // TODO - Validate if we need this
            throw new NotImplementedException();
        }

        private TurnContext CreateContext(Activity activity)
        {
            return new TurnContext(this, activity);
        }
    }
}