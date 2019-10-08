using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;

/// <summary>
/// This dialog will prompt user to log into calendar account
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class OAuthPromptDialog : ComponentDialog
    {
        public OAuthPromptDialog()
            : base(nameof(OAuthPromptDialog))
        {
            var oauthDialog = new AdaptiveDialog("oauth")
            {
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new OAuthInput()
                            {
                                Title = "Sign in",
                                Text = "Please log in to your calendar account",
                                ConnectionName = "Outlook",
                                TokenProperty = "user.token"
                            }
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(oauthDialog);

            // The initial child Dialog to run.
            InitialDialogId = oauthDialog.Id;
        }
    }
}

