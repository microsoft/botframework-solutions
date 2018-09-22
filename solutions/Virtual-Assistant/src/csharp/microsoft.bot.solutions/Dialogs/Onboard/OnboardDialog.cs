// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs.Onboard
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Solutions.Cards;
    using Microsoft.Bot.Solutions.Data;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Resources;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Microsoft.Recognizers.Text;

    /// <summary>
    /// OnboardDialog.
    /// </summary>
    public class OnboardDialog : ComponentDialog
    {
        /// <summary>
        /// OnboardDialog Id.
        /// </summary>
        public const string ComponentDialogId = "OnboardingDialog";
        public const string NAME_PROMPT = "OnboardingDialog.NamePrompt";
        public const string PRIMARY_EMAIL_PROMPT = "OnboardingDialog.PrimaryEmailPrompt";
        public const string SECONDARY_EMAIL_PROMPT = "OnboardingDialog.SecondaryEmailPrompt";
        public const string LOCATION_PROMPT = "OnboardingDialog.LocationPrompt";
        public const string CONFIRM_PROMPT = "OnboardingDialog.ConfirmPrompt";

        private UserDataAccessors userDataAccessors;

        private CommonResponseBuilder commonResponseBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnboardDialog"/> class.
        /// </summary>
        /// <param name="accessors">Accessor used in dialog.</param>
        public OnboardDialog(UserDataAccessors accessors)
            : base(ComponentDialogId)
        {
            userDataAccessors = accessors;
            commonResponseBuilder = new CommonResponseBuilder();

            // Define the conversation flow using a waterfall model.
            var waterfallSteps = new WaterfallStep[]
            {
                NamePrompt,
                PrimaryEailPrompt,
                SecondaryEmailPrompt,
                LocationPrompt,
                ConfirmBeforeSave,
                EndOnboardingDialog,
            };

            AddDialog(new WaterfallDialog(ComponentDialogId, waterfallSteps));

            AddDialog(new TextPrompt(OnboardingView.NAME_PROMPT));
            AddDialog(new TextPrompt(OnboardingView.PRIMARY_EMAIL_PROMPT));
            AddDialog(new TextPrompt(OnboardingView.SECONDARY_EMAIL_PROMPT));
            AddDialog(new TextPrompt(OnboardingView.LOCATION_PROMPT));
            AddDialog(new ConfirmPrompt(CONFIRM_PROMPT, null, Culture.English)
            { Style = ListStyle.SuggestedAction });
        }

        public OnboardingView View => new OnboardingView();

        public static void Register(DialogSet dialogs, UserDataAccessors accessors)
        {
            dialogs.Add(new OnboardDialog(accessors));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> NamePrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            ////var userData = await this.userDataAccessors.UserDataState.GetAsync(sc.Context, () => new UserData());

            ////if ((userData.Name == null) || (userData.Name == string.Empty))
            ////{
            var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = View.RenderTemplate(sc.Context, "en", NAME_PROMPT).Result.Text } };
            return await sc.PromptAsync(NAME_PROMPT, options);
            ////}
            ////else
            ////{
            ////    return await sc.NextAsync();
            ////}
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> PrimaryEailPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            ////var userData = await this.userDataAccessors.UserDataState.GetAsync(sc.Context, () => new UserData());

            ////if ((userData.Name == null) || (userData.Name == string.Empty))
            ////{
            ////    object name = sc.ActiveDialog.State["name"] = sc.Result;
            ////}
            var name = sc.ActiveDialog.State["name"] = sc.Result;
            ////await this.View.ReplyWith(sc.Context, OnboardingView.HAVE_NAME, new { name });

            var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = View.RenderTemplate(sc.Context, "en", OnboardingView.PRIMARY_EMAIL_PROMPT).Result.Text } };
            return await sc.PromptAsync(PRIMARY_EMAIL_PROMPT, options);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> SecondaryEmailPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var email = sc.ActiveDialog.State["primaryMail"] = sc.Result;
            ////await this.View.ReplyWith(sc.Context, OnboardingView.HAVE_EMAIL, new { email });

            var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = View.RenderTemplate(sc.Context, "en", OnboardingView.SECONDARY_EMAIL_PROMPT).Result.Text } };
            return await sc.PromptAsync(SECONDARY_EMAIL_PROMPT, options);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> LocationPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var email = sc.ActiveDialog.State["secondaryMail"] = sc.Result;

            ////await this.View.ReplyWith(sc.Context, OnboardingView.HAVE_SECONDARY_EMAIL, new { email });

            var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = View.RenderTemplate(sc.Context, "en", OnboardingView.LOCATION_PROMPT).Result.Text } };
            return await sc.PromptAsync(LOCATION_PROMPT, options);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> ConfirmBeforeSave(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            ////try
            ////{
            var name = sc.ActiveDialog.State["name"];
            var primaryMail = sc.ActiveDialog.State["primaryMail"];
            var secondaryMail = sc.ActiveDialog.State["secondaryMail"];
            var location = sc.ActiveDialog.State["location"] = sc.Result;

            var informationCard = new BasicInfoData
            {
                NameInfo = "Name: " + name,
                LocationInfo = "Location: " + location,
                PrimaryEmailInfo = "PrimaryEmail: " + primaryMail,
                SecondaryEmailInfo = "SecondaryEmail: " + secondaryMail,
            };
            var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(CommonResponses.ConfirmUserInfo, "Resources/Cards/OnboardInfoCard.json", informationCard, commonResponseBuilder);

            return await sc.PromptAsync(CONFIRM_PROMPT, new PromptOptions { Prompt = replyMessage, RetryPrompt = sc.Context.Activity.CreateReply(CommonResponses.ConfirmUserInfo, commonResponseBuilder), });
            ////}
            ////catch
            ////{
            ////    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CommonResponses.ErrorMessage, this.commonResponseBuilder));
            ////    return await sc.CancelAllDialogsAsync();
            ////}
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<DialogTurnResult> EndOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var name = sc.ActiveDialog.State["name"];
            var primaryMail = sc.ActiveDialog.State["primaryMail"];
            var secondaryMail = sc.ActiveDialog.State["secondaryMail"];
            var location = sc.ActiveDialog.State["location"];

            // Save data into user data
            var userData = await userDataAccessors.UserDataState.GetAsync(sc.Context, () => new UserData());
            userData.Name = (string)name;
            userData.PrimaryMail = (string)primaryMail;
            userData.SecondaryMail = (string)secondaryMail;
            userData.Location = (string)location;

            await View.ReplyWith(sc.Context, OnboardingView.HAVE_LOCATION, new { name, location });

            await sc.Context.SendActivityAsync(new Activity(type: ActivityTypes.EndOfConversation));

            return await sc.EndDialogAsync();
        }
    }
}
