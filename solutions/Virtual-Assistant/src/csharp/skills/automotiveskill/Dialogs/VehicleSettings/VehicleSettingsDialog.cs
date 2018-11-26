// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using global::AutomotiveSkill.Dialogs.VehicleSettings.Resources;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Cards;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Skills;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class VehicleSettingsDialog : AutomotiveSkillDialog
    {    
        private static readonly Regex WordRequiresAn = new Regex("^([aio]|e(?!u)|u(?![^aeoiu])).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WordCharacter = new Regex("^\\w", RegexOptions.Compiled);
        private static readonly IReadOnlyDictionary<string, string> SettingValueToSpeakableIngForm = new Dictionary<string, string>
        {
            { "decrease", "Decreasing" },
            { "increase", "Increasing" },
        };

        private readonly IRecognizer vehicleSettingNameSelectionLuisRecognizer;
        private readonly IRecognizer vehicleSettingValueSelectionLuisRecognizer;
        private readonly IRecognizer vehicleSettingChangeConfirmationLuisRecognizer;

        private readonly SettingList settingList;
        private readonly SettingFilter settingFilter;

        public VehicleSettingsDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<AutomotiveSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(VehicleSettingsDialog), services, accessor, serviceManager)
        {
            // Initialise supporting LUIS models for followup questions
            vehicleSettingNameSelectionLuisRecognizer = services.LuisServices["vehiclesettings_name_selection"];
            vehicleSettingValueSelectionLuisRecognizer = services.LuisServices["vehiclesettings_value_selection"];
            vehicleSettingChangeConfirmationLuisRecognizer = services.LuisServices["vehiclesettings_change_confirmation"];

            // JSON resource files provided metatadata as to the available car settings, names and the values that can be set
            var resDir = Path.Combine(Path.GetDirectoryName(typeof(VehicleSettingsDialog).Assembly.Location), 
                "Dialogs\\VehicleSettings\\Resources\\");

            settingList = new SettingList(resDir + "available_settings.json", resDir + "setting_alternative_names.json");
            settingFilter = new SettingFilter(settingList);
           
            // Setting Change waterfall
            var processVehicleSettingChangeWaterfall = new WaterfallStep[]
            {
                ProcessSetting,
                ProcessVehicleSettingsChange,
                ProcessChange,
                SendChange
            };
            AddDialog(new WaterfallDialog(Actions.ProcessVehicleSettingChange, processVehicleSettingChangeWaterfall));        

            // Prompts
            AddDialog(new TextPrompt(Actions.SettingSelectionPrompt, SettingSelectionValidator));
            AddDialog(new TextPrompt(Actions.SettingConfirmationPrompt, SettingConfirmationValidator));

            // Set starting dialog for component
            InitialDialogId = Actions.ProcessVehicleSettingChange;
        }
       
        /// <summary>
        /// Top level processing, is the user trying to check or change a setting?
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DialogTurnResult> ProcessSetting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            var luisResult = state.LuisResult;
            var topIntent = luisResult?.GetTopScoringIntent();

            if (topIntent == null)
            {
                return await sc.EndDialogAsync(true);
            }

            switch (topIntent.Value.intent)
            {
                case "VEHICLE_SETTINGS_CHANGE":

                    return await sc.NextAsync();

                default:
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                    return await sc.EndDialogAsync(true, cancellationToken);
            }
        }

        /// <summary>
        /// Process a request to change a setting on a vehicle
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private async Task<DialogTurnResult> ProcessVehicleSettingsChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            state.DialogStateType = VehicleSettingStage.None;

            settingFilter.Filter(state, new RecognizerResultWrapper(state.LuisResult));

            var settingValues = state.GetUniqueSettingValues();
            if (!settingValues.Any())
            {
                // This shouldn't happen because the SettingFilter would just add all possible values to let the user select from them.
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsMissingSettingValue));
                state.DialogStateType = VehicleSettingStage.None;
                return await sc.EndDialogAsync();
            }
            else if (settingValues.Count() > 1)
            {
                // We have found multiple setting values which we need to prompt the user to resolve

                state.DialogStateType = VehicleSettingStage.ValueSelection;
                string settingName = string.Empty;

                if (state.Changes.Any())
                {
                    settingName = state.Changes[0].SettingName;
                }

                var promptReplacements = new StringDictionary {
                        { "settingName", settingName },
                        { "postText", VehicleSettingsResponses.VehicleSettingsSettingValueSelectionPost.Reply.Text },
                    };

                promptReplacements.Add("preText", new BotResponseBuilder().Format(
                    VehicleSettingsResponses.VehicleSettingsSettingValueSelectionPre.Reply.Text, promptReplacements));

                for (var i = 0; i < settingValues.Count(); ++i)
                {
                    promptReplacements.Add($"item{i}Text", $"{i + 1}. {settingValues[i]}");
                }

                // Prompt the user on the setting calue
                var prompt = sc.Context.Activity.CreateAdaptiveCardReply(
                    VehicleSettingsResponses.VehicleSettingsSettingValueSelection,
                    "Dialogs/VehicleSettings/Resources/Cards/ListSelection.json",
                    new VehicleSettingsCardDataBase(),
                    null,
                    promptReplacements);

                return await sc.PromptAsync(Actions.SettingSelectionPrompt, new PromptOptions { Prompt = prompt });            
            }

            // We only hae one setting value so proceed to next step
            return await sc.NextAsync();
        }

        private async Task<bool> SettingSelectionValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            await RunLuisForFollowUp(promptContext.Context, state);

            settingFilter.Filter(state, new RecognizerResultWrapper(state.LuisResult));

            return true;
        }

        /// <summary>
        /// Process the change that we are about to perform. If required the user is prompted for confirmation
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DialogTurnResult> ProcessChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            // Perform the request change
            if (state.Changes.Any())
            {
                var change = state.Changes[0];
                if (change.OperationStatus == SettingOperationStatus.TO_DO)
                {
                    // TODO - Validation of change would go here, for now we just apply the change

                    var availableSetting = this.settingList.FindSetting(change.SettingName);
                    var availableSettingValue = this.settingList.FindSettingValue(availableSetting, change.Value);

                    // Check confirmation first.
                    if (availableSettingValue != null && availableSettingValue.RequiresConfirmation)
                    {                        
                        state.DialogStateType = VehicleSettingStage.ChangeConfirmation;
                        var promptTemplate = VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmation;
                        var promptReplacements = new StringDictionary {
                                    { "settingName", change.SettingName },
                                    { "value", change.Value },
                                };

                        if (availableSetting != null && availableSetting.Categories != null && availableSetting.Categories.Any())
                        {
                            promptTemplate = VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmationWithCategory;
                            promptReplacements.Add("category", availableSetting.Categories[0]);
                            if (WordRequiresAn.Match(promptReplacements["category"]).Success)
                            {
                                promptReplacements.Add("aOrAnBeforeCategory", "an");
                            }
                            else
                            {
                                promptReplacements.Add("aOrAnBeforeCategory", "a");
                            }
                        }

                        // TODO - Explore moving to ConfirmPrompt following usability testing
                        var prompt = sc.Context.Activity.CreateReply(promptTemplate, ResponseBuilder, promptReplacements);
                        return await sc.PromptAsync(Actions.SettingConfirmationPrompt, new PromptOptions { Prompt = prompt });
                    }
                    else
                    {
                        // No confirmation required
                        return await sc.NextAsync();
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                    state.DialogStateType = VehicleSettingStage.None;
                    return await sc.EndDialogAsync();
                }
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                state.DialogStateType = VehicleSettingStage.None;
                return await sc.EndDialogAsync();
            }
        }

        private async Task<bool> SettingConfirmationValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            await RunLuisForFollowUp(promptContext.Context, state);

            if (state.LuisResult != null && "SETTING_CHANGE_CONFIRMATION_NO".Equals(state.LuisResult.GetTopScoringIntent().intent))
            {
                await promptContext.Context.SendActivityAsync(promptContext.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmationDenied));
                state.DialogStateType = VehicleSettingStage.None;

                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<DialogTurnResult> SendChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var change = state.Changes[0];

            // Sending the change.
            if (change.Amount != null)
            {
                var promptReplacements = new StringDictionary {
                                        { "settingName", change.SettingName },
                                        { "amount", change.Amount.Amount.ToString() },
                                        { "unit", UnitToString(change.Amount.Unit) },
                                    };
                if (change.IsRelativeAmount)
                {
                    if (change.Amount.Amount < 0)
                    {
                        promptReplacements["increasingDecreasing"] = "Decreasing";
                        promptReplacements["amount"] = (-change.Amount.Amount).ToString();
                    }
                    else
                    {
                        promptReplacements["increasingDecreasing"] = "Increasing";
                    }

                    // Send an event to the device along with the text confirmation
                    await SendActionToDevice(sc, change, promptReplacements);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(
                        VehicleSettingsResponses.VehicleSettingsChangingRelativeAmount, ResponseBuilder));
                }
                else
                {
                    // Send an event to the device along with the text confirmation
                    await SendActionToDevice(sc, change, promptReplacements);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(
                        VehicleSettingsResponses.VehicleSettingsChangingAmount, ResponseBuilder, promptReplacements));
                }
            }
            else
            {
                BotResponse promptTemplate;
                var promptReplacements = new StringDictionary { { "settingName", change.SettingName }};
                if (SettingValueToSpeakableIngForm.TryGetValue(change.Value.ToLowerInvariant(), out var valueIngForm))
                {
                    promptTemplate = VehicleSettingsResponses.VehicleSettingsChangingValueKnown;
                    promptReplacements["valueIngForm"] = valueIngForm;
                }
                else
                {
                    promptTemplate = VehicleSettingsResponses.VehicleSettingsChangingValue;
                    promptReplacements["value"] = change.Value;
                }

                // Send an event to the device along with the text confirmation
                await SendActionToDevice(sc, change, promptReplacements);

                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(promptTemplate, ResponseBuilder, promptReplacements));

            }

            return await sc.EndDialogAsync();
        }    

        private string UnitToString(string unit)
        {
            if (unit != null && WordCharacter.Match(unit).Success)
            {
                return $" {unit}";
            }
            else
            {
                return unit;
            }
        }

        private async Task SendActionToDevice(WaterfallStepContext sc, SettingChange setting, StringDictionary settingDetail )
        {
            var actionEvent = sc.Context.Activity.CreateReply();
            actionEvent.Type = ActivityTypes.Event;
            actionEvent.Name = setting.SettingName;
            actionEvent.Value = settingDetail;

            await sc.Context.SendActivityAsync(actionEvent);
        }

        private async Task RunLuisForFollowUp(ITurnContext query, AutomotiveSkillState state)
        {
            IRecognizer luisRecognizer = null;
            if (state.DialogStateType == VehicleSettingStage.NameSelection)
            {
                luisRecognizer = vehicleSettingNameSelectionLuisRecognizer;
            }
            else if (state.DialogStateType == VehicleSettingStage.ValueSelection)
            {
                luisRecognizer = vehicleSettingValueSelectionLuisRecognizer;
            }
            else if (state.DialogStateType == VehicleSettingStage.ChangeConfirmation)
            {
                luisRecognizer = vehicleSettingChangeConfirmationLuisRecognizer;
            }
            
            if (luisRecognizer != null)
            {
                var luisResult = await luisRecognizer.RecognizeAsync(query, CancellationToken.None);
                state.LuisResult = luisResult;
                state.AddRecognizerResult(luisResult, false);
            }
        }

    }

    public class VehicleSettingsCardDataBase : CardDataBase
    { }
}
