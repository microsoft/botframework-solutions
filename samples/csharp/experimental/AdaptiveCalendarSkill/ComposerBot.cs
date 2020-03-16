// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;

namespace Microsoft.Bot.Builder.ComposerBot.Json
{
    public class ComposerBot : ActivityHandler
    {
        private readonly ResourceExplorer resourceExplorer;
        private readonly UserState userState;
        private DialogManager dialogManager;
        private readonly ConversationState conversationState;
        private readonly IStatePropertyAccessor<DialogState> dialogState;
        private readonly ISourceMap sourceMap;
        private readonly string rootDialogFile;

        public ComposerBot(string rootDialogFile, ConversationState conversationState, UserState userState, ResourceExplorer resourceExplorer, ISourceMap sourceMap)
        {
            this.conversationState = conversationState;
            this.userState = userState;
            this.dialogState = conversationState.CreateProperty<DialogState>("DialogState");
            this.sourceMap = sourceMap;
            this.resourceExplorer = resourceExplorer;
            this.rootDialogFile = rootDialogFile;
            LoadRootDialogAsync();
        }
        
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await this.userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private void LoadRootDialogAsync()
        {
            var rootFile = resourceExplorer.GetResource(rootDialogFile);
            var rootDialog = resourceExplorer.LoadType<Dialog>(rootFile); 
            this.dialogManager = new DialogManager(rootDialog)
                                .UseResourceExplorer(resourceExplorer)
                                .UseLanguageGeneration();
        }       
    }
}
