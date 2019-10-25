using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Solutions.Skill
{
    public class SkillDialog : Dialog
    {
        private ConversationState _conversationState;

        public SkillDialog(ConversationState conversationState, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
               : base(nameof(SkillDialog))
        {
            RegisterSourceLocation(callerPath, callerLine);

            _conversationState = conversationState;
        }

        /// <summary>
        /// Gets or sets SkillId to call.
        /// </summary>
        [JsonProperty("method")]
        public string SkillId { get; set; }

        /// <summary>
        /// Gets or sets the event name you want to use to invoke the skill.
        /// </summary>
        /// <value>If this is null then default is to route the current activity to the skill. If this is set, then a EventActivity will be sent to the skill to initiate the conversation</value>
        [JsonProperty("eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the value to pass to the skill.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the property path to store the result returned from the skill.
        /// </summary>
        public string ResultProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            var dialogArgs = options as SkillDialogArgs;
            var skillId = dialogArgs?.SkillId ?? SkillId;
            dc.State.SetValue("this.SkillId", skillId);
            var eventName = dialogArgs?.EventName ?? EventName;
            var boundValue = BindValue(dc, dialogArgs?.Value);
            var fwdActivity = dc.Context.Activity;

            if (!string.IsNullOrEmpty(eventName))
            {
                await dc.Context.SendActivityAsync($"SkillDialog: InBeginDialog using an event: {EventName}", cancellationToken: cancellationToken);
                var eventActivity = Activity.CreateEventActivity();
                eventActivity.Name = eventName;
                eventActivity.Value = boundValue;
                eventActivity.ApplyConversationReference(dc.Context.Activity.GetConversationReference());
                eventActivity.From = dc.Context.Activity.From;
                eventActivity.Recipient = dc.Context.Activity.Recipient;
                fwdActivity = (Activity)eventActivity;
            }
            else
            {
                await dc.Context.SendActivityAsync($"SkillDialog: InBeginDialog using pass through (activity is: {dc.Context.Activity.Type}.", cancellationToken: cancellationToken);
                fwdActivity.Value = boundValue;
            }

            // forward fwdActivity to the remote skill.
            return await SendToSkill(dc, fwdActivity, cancellationToken);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            await dc.Context.SendActivityAsync($"SkillDialog: InContinueDialog, ActivityType: {dc.Context.Activity.Type}", cancellationToken: cancellationToken);
            if (dc.Context.Activity.Type == ActivityTypes.EndOfConversation && (string)dc.Context.Activity.Recipient.Properties["SkillId"] == SkillId)
            {
                // look at the dc.Context.Activity.Code for exit status.
                await dc.Context.SendActivityAsync("SkillDialog: got EndOfConversation", cancellationToken: cancellationToken);
                if (ResultProperty != null)
                {
                    // set the result of the remote skill into ResultProperty memory
                    dc.State.SetValue(ResultProperty, dc.Context.Activity.Value);
                }

                return await dc.EndDialogAsync(dc.Context.Activity.Value, cancellationToken);
            }

            // Just forward to the remote skill
            return await SendToSkill(dc, dc.Context.Activity, cancellationToken);
        }

        public override async Task EndDialogAsync(ITurnContext turnContext, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default(CancellationToken))
        {
            await turnContext.SendActivityAsync("SkillDialog: In EndDialog", cancellationToken: cancellationToken);
            await base.EndDialogAsync(turnContext, instance, reason, cancellationToken);
        }

        protected override string OnComputeId()
        {
            return $"{GetType().Name}[{SkillId}|{EventName}]";
        }

        protected object BindValue(DialogContext dc, object value)
        {
            // binding options are static definition of options with overlay of passed in options);
            var bindingValue = (JObject)ObjectPath.Merge(Value ?? new JObject(), value ?? new JObject());
            var boundValue = bindingValue;

            // TODO, when fully declarative we should support dynamic binding of values
            // This code relies on the ExpressionEngine to evaluate strings in the bindingValue object
            // var boundValue = new JObject();
            // foreach (var binding in bindingValue)
            // {
            //    // evalute the value
            //    var (result, error) = new ExpressionEngine().Parse(binding.Value.ToString()).TryEvaluate(dc.State);
            //    if (error != null)
            //    {
            //        throw new Exception(error);
            //    }
            //    // and store in options as the result
            //    boundValue[binding.Key] = JToken.FromObject(result);
            // }
            return boundValue;
        }

        private async Task<DialogTurnResult> SendToSkill(DialogContext dc, Activity activity, CancellationToken cancellationToken)
        {
            var skillId = dc.State.GetValue<string>("this.SkillId");

            // TODO: consider having an extension method in DC that saves state for you.
            // Always save state before forwarding (things won't work if you don't)
            await _conversationState.SaveChangesAsync(dc.Context, true, cancellationToken);

            await dc.Context.TurnState.Get<SkillHostAdapter>().ForwardActivityAsync(dc.Context, skillId, activity, cancellationToken);
            return EndOfTurn;
        }
    }
}
