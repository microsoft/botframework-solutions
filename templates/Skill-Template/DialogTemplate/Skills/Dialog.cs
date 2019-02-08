// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using $safeprojectname$.Dialogs.Shared;
using $safeprojectname$.ServiceClients;

namespace $rootnamespace$
{
    public class $safeitemname$ : SkillDialogBase
    {
        public $safeitemname$(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<SkillConversationState> conversationStateAccessor,
            IStatePropertyAccessor<SkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof($safeitemname$), services, responseManager, conversationStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            var dialog = new WaterfallStep[]
            {
                PromptForName,
                EndDialog
            };

            InitialDialogId = nameof($safeitemname$);
            AddDialog(new WaterfallDialog(nameof($safeitemname$), dialog));
            AddDialog(new TextPrompt($safeitemname$Responses.NamePrompt));
        }

        public async Task<DialogTurnResult> PromptForName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($safeitemname$Responses.NamePrompt, new PromptOptions()
            {
                Prompt = ResponseManager.GetResponse($safeitemname$Responses.NamePrompt),
            });
        }

        public async Task<DialogTurnResult> EndDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(ResponseManager.GetResponse($safeitemname$Responses.HaveNameMessage, new StringDictionary { { "Name", (string)stepContext.Result } }));
            return await stepContext.EndDialogAsync();
        }
    }
}