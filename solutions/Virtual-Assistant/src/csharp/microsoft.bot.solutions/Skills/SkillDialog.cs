using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialog : Dialog
    {
        // Constants
        private const string ActiveSkillStateKey = "ActiveSkill";
        private const string TokenRequestEventName = "tokens/request";
        private const string TokenResponseEventName = "tokens/response";

        // Fields
        private static Dictionary<string, SkillConfiguration> _skills;
        private static OAuthPrompt _authPrompt;
        private InProcAdapter _inProcAdapter;
        private IBot _activatedSkill;
        private bool _skillInitialized;

        public SkillDialog(Dictionary<string, SkillConfiguration> skills)
            : base(nameof(SkillDialog))
        {
            _skills = skills;
        }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var skillOptions = (SkillDialogOptions)options;

            // Save the active skill in state
            var skillDefinition = skillOptions.SkillDefinition;
            dc.ActiveDialog.State[ActiveSkillStateKey] = skillDefinition;

            // Set parameters
            var skillConfiguration = _skills[skillDefinition.Id];
            foreach (var parameter in skillDefinition.Parameters)
            {
                if (skillOptions.Parameters.TryGetValue(parameter, out var paramValue))
                {
                    skillConfiguration.Properties.Add(parameter, paramValue);
                }
            }

            // Initialize authentication prompt
            _authPrompt = new OAuthPrompt(nameof(OAuthPrompt), new OAuthPromptSettings()
            {
                ConnectionName = skillDefinition.AuthConnectionName,
                Title = "Skill Authentication",
                Text = $"Please login to access this feature.",
            });

            return Task.FromResult(EndOfTurn);
        }

        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ForwardToSkill(dc, dc.Context.Activity);
        }

        private async Task InitializeSkill(DialogContext dc)
        {
            try
            {
                var skillDefinition = dc.ActiveDialog.State[ActiveSkillStateKey] as SkillDefinition;
                var skillConfiguration = _skills[skillDefinition.Id];

                var cosmosDbOptions = skillConfiguration.CosmosDbOptions;
                cosmosDbOptions.CollectionId = skillDefinition.Name;

                // Initialize skill state
                var cosmosDbStorage = new CosmosDbStorage(cosmosDbOptions);
                var userState = new UserState(cosmosDbStorage);
                var conversationState = new ConversationState(cosmosDbStorage);

                // Create skill instance
                try
                {
                    var skillType = Type.GetType(skillDefinition.Assembly);
                    _activatedSkill = (IBot)Activator.CreateInstance(skillType, skillConfiguration, conversationState, userState, true);
                }
                catch (Exception e)
                {
                    var message = $"Skill ({skillDefinition.Name}) could not be created.";
                    throw new InvalidOperationException(message, e);
                }

                _inProcAdapter = new InProcAdapter
                {
                    // set up skill turn error handling
                    OnTurnError = async (context, exception) =>
                    {
                        await context.SendActivityAsync(context.Activity.CreateReply($"Sorry, something went wrong trying to communicate with the skill. Please try again."));

                        // Send error trace to emulator
                        await dc.Context.SendActivityAsync(
                            new Activity(
                                type: ActivityTypes.Trace,
                                text: $"Skill Error: {exception.Message} | {exception.StackTrace}"
                                ));

                        // Log exception in AppInsights
                        skillConfiguration.TelemetryClient.TrackException(exception);
                    },
                };

                _inProcAdapter.Use(new EventDebuggerMiddleware());
                _inProcAdapter.Use(new AutoSaveStateMiddleware(userState, conversationState));
                _skillInitialized = true;
            }
            catch
            {
                // something went wrong initializing the skill, so end dialog cleanly and throw so the error is logged
                _skillInitialized = false;
                await dc.EndDialogAsync();
                throw;
            }
        }

        public async Task<DialogTurnResult> ForwardToSkill(DialogContext dc, Activity activity)
        {
            try
            {
                if (!_skillInitialized)
                {
                    await InitializeSkill(dc);
                }

                _inProcAdapter.ProcessActivity(activity, async (skillContext, ct) =>
                {
                    await _activatedSkill.OnTurnAsync(skillContext);
                }).Wait();

                var queue = new List<Activity>();
                var endOfConversation = false;
                var skillResponse = _inProcAdapter.GetNextReply();

                while (skillResponse != null)
                {
                    if (skillResponse.Type == ActivityTypes.EndOfConversation)
                    {
                        endOfConversation = true;
                    }
                    else if (skillResponse?.Name == TokenRequestEventName)
                    {
                        // Send trace to emulator
                        await dc.Context.SendActivityAsync(
                            new Activity(
                                type: ActivityTypes.Trace,
                                text: $"<--Received a Token Request from a skill"
                                ));

                        // Uncomment this line to prompt user for login every time the skill requests a token
                        // var a = dc.Context.Adapter as BotFrameworkAdapter;
                        // await a.SignOutUserAsync(dc.Context, _skill.AuthConnectionName, dc.Context.Activity.From.Id, default(CancellationToken));

                        var authResult = await _authPrompt.BeginDialogAsync(dc);
                        if (authResult.Result?.GetType() == typeof(TokenResponse))
                        {
                            var tokenEvent = skillResponse.CreateReply();
                            tokenEvent.Type = ActivityTypes.Event;
                            tokenEvent.Name = TokenResponseEventName;
                            tokenEvent.Value = authResult.Result;

                            return await ForwardToSkill(dc, tokenEvent);
                        }
                        else
                        {
                            return authResult;
                        }
                    }
                    else
                    {
                        queue.Add(skillResponse);
                    }

                    skillResponse = _inProcAdapter.GetNextReply();
                }

                // send skill queue to User
                if (queue.Count > 0)
                {
                    await dc.Context.SendActivitiesAsync(queue.ToArray());
                }

                // handle ending the skill conversation
                if (endOfConversation)
                {
                    await dc.Context.SendActivityAsync(
                        new Activity(
                            type: ActivityTypes.Trace,
                            text: $"<--Ending the skill conversation"
                            ));

                    return await dc.EndDialogAsync();
                }
                else
                {
                    return EndOfTurn;
                }
            }
            catch
            {
                // something went wrong forwarding to the skill, so end dialog cleanly and throw so the error is logged.
                // NOTE: errors within the skill itself are handled by the OnTurnError handler on the adapter.
                await dc.EndDialogAsync();
                throw;
            }
        }
    }
}
