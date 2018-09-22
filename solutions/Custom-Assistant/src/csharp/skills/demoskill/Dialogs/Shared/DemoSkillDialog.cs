using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;

namespace DemoSkill
{
    public class DemoSkillDialog : ComponentDialog
    {
        public const string AuthSkillMode = "SkillAuth";
        public const string AuthLocalMode = "LocalAuth";

        public DemoSkillDialog(string dialogId)
            : base(dialogId)
        {
            AddDialog(new EventPrompt(AuthSkillMode, "tokens/response", TokenResponseValidator));
            AddDialog(new OAuthPrompt(
                AuthLocalMode,
                new OAuthPromptSettings()
                {
                    ConnectionName = "adauth",
                    Text = $"Authentication",
                    Title = "Signin",
                    Timeout = 300000, // User has 5 minutes to login
                }, AuthPromptValidator));

            InitialDialogId = dialogId;
        }

        public static async Task<DialogTurnResult> AuthPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // var adapter = dialogContext.Context.Adapter as BotFrameworkAdapter;
            // await adapter.SignOutUserAsync(dialogContext.Context,"adauth", default(CancellationToken));
            var skillOptions = (DemoSkillDialogOptions)sc.Options;

            // If in Skill mode we ask the calling Bot for the token
            if (skillOptions != null && skillOptions.SkillMode)
            {
                // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                // TODO Error handling - if we get a new activity that isn't an event
                var response = sc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Event;
                response.Name = "tokens/request";

                // Send the tokens/request Event
                await sc.Context.SendActivityAsync(response);

                // Wait for the tokens/response event
                return await sc.PromptAsync(AuthSkillMode, new PromptOptions());
            }
            else
            {
                return await sc.PromptAsync(AuthLocalMode, new PromptOptions());
            }
        }

        private Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                // pc.End(activity.Value);
                return Task.FromResult(true);
            }
            else
            {
                // pc.End(null);
                return Task.FromResult(false);
            }
        }

        // Used for skill/event signin scenarios
        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> pc, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
