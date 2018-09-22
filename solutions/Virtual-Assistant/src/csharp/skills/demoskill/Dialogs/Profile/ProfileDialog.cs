using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace DemoSkill
{
    public class ProfileDialog : DemoSkillDialog
    {
        public const string Name = "ProfileDialog";

        private ProfileResponses _responder = new ProfileResponses();
        private ProfileGraphClient _graphClient;

        public ProfileDialog()
            : base(Name)
        {
            var profile = new WaterfallStep[]
            {
                AuthPrompt,
                ShowProfile,
            };

            AddDialog(new WaterfallDialog(Name, profile));
        }

        public async Task<DialogTurnResult> ShowProfile(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
            // When the token is cached we get a TokenResponse object.
            TokenResponse tokenResponse;

            var resultType = sc.Result.GetType();
            if (resultType == typeof(TokenResponse))
            {
                tokenResponse = sc.Result as TokenResponse;
            }
            else
            {
                var tokenResponseObject = sc.Result as JObject;
                tokenResponse = tokenResponseObject?.ToObject<TokenResponse>();
            }

            var token = tokenResponse.Token;
            _graphClient = new ProfileGraphClient(token);

            var user = await _graphClient.GetMe();

            if (user != null)
            {
                var name = user.DisplayName;
                var jobTitle = user.JobTitle;
                var location = user.OfficeLocation;
                var email = user.Mail;

                await _responder.ReplyWith(sc.Context, ProfileResponses.HaveProfile, new { name, jobTitle, location, email });
            }
            else
            {
                await _responder.ReplyWith(sc.Context, ProfileResponses.NullProfile);
            }

            return await sc.EndDialogAsync();
        }
    }
}