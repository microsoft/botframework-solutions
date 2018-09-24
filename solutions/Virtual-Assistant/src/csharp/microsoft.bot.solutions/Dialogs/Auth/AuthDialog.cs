using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Dialogs.Auth
{
    public class AuthDialog : ComponentDialog
    {
        public AuthDialog(string authConnectionName, OAuthPromptSettings oauthSettings) 
            : base(nameof(AuthDialog))
        {
            var auth = new WaterfallStep[]
            {
                PromptToSignIn,
                GetAuthToken,
            };

            AddDialog(new WaterfallDialog("authPrompt", auth));
            AddDialog(new OAuthPrompt(nameof(OAuthPrompt), oauthSettings, AuthPromptValidator));
        }

        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task<DialogTurnResult> PromptToSignIn(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task<DialogTurnResult> GetAuthToken(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
