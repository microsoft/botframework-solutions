// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
{
    using global::AutomotiveSkill.Dialogs.VehicleSettings.Resources;
    using Luis;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Cards;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Skills;
    using System;
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

        private readonly SettingList settingList;
        private readonly SettingFilter settingFilter;

        public VehicleSettingsDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<AutomotiveSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(VehicleSettingsDialog), services, accessor, serviceManager)
        {
            // Initialise supporting LUIS models for followup questions
            vehicleSettingNameSelectionLuisRecognizer = services.LuisServices["vehiclesettings_name_selection"];
            vehicleSettingValueSelectionLuisRecognizer = services.LuisServices["vehiclesettings_value_selection"];

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
            AddDialog(new ConfirmPrompt(Actions.SettingConfirmationPrompt));

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
            var luisResult = state.VehicleSettingsLuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            switch (topIntent.Value)
            {
                case VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE:

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

            var luisResult = state.VehicleSettingsLuisResult;
            if (luisResult.Entities.AMOUNT != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.AMOUNT), luisResult.Entities.AMOUNT);
            }

            if (luisResult.Entities.INDEX != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.INDEX), luisResult.Entities.INDEX);
            }

            if (luisResult.Entities.SETTING != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.SETTING), luisResult.Entities.SETTING);
            }

            if (luisResult.Entities.TYPE != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.TYPE), luisResult.Entities.TYPE);
            }

            if (luisResult.Entities.UNIT != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.UNIT), luisResult.Entities.UNIT);
            }

            if (luisResult.Entities.VALUE != null)
            {
                state.Entities.Add(nameof(luisResult.Entities.VALUE), luisResult.Entities.VALUE);
            }

            settingFilter.PostProcessSettings(state, state.VehicleSettingsLuisResult);
            settingFilter.ApplyContentLogic(state);

            var settingValues = state.GetUniqueSettingValues();
            if (!settingValues.Any())
            {
                // This shouldn't happen because the SettingFilter would just add all possible values to let the user select from them.
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsMissingSettingValue));
                return await sc.EndDialogAsync();
            }
            else if (settingValues.Count() > 1)
            {
                // We have found multiple setting values which we need to prompt the user to resolve
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

                // Prompt the user on the setting value
                var prompt = sc.Context.Activity.CreateAdaptiveCardReply(
                    VehicleSettingsResponses.VehicleSettingsSettingValueSelection,
                    "Dialogs/VehicleSettings/Resources/Cards/ListSelection.json",
                    new VehicleSettingsCardDataBase(),
                    null,
                    promptReplacements);

                return await sc.PromptAsync(Actions.SettingSelectionPrompt, new PromptOptions { Prompt = prompt });            
            }

            // We only have one setting value so proceed to next step
            return await sc.NextAsync();
        }

        private async Task<bool> SettingSelectionValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            await RunLuisForFollowUp(promptContext.Context, VehicleSettingStage.ValueSelection, state);

            settingFilter.Filter(state,VehicleSettingStage.ValueSelection, state.VehicleSettingsLuisResult);

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
                        // No confirmation required so we skip to sending the change
                        return await sc.NextAsync();
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                    return await sc.EndDialogAsync();
                }
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                return await sc.EndDialogAsync();
            }
        }

        public async Task<DialogTurnResult> SendChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            var settingChangeConfirmed = false;
            // If we skip the ConfirmPrompt due to no confirmation needed then Result will be NULL
            if (sc.Result == null)
            {
                settingChangeConfirmed = true;
            }
            else
            {
                settingChangeConfirmed = (bool)sc.Result;
            }

            if (settingChangeConfirmed)
            {      
                var change = state.Changes[0];

                // If the change involves an amount then we add this to the change event
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
                            promptReplacements["increasingDecreasing"] = VehicleSettingsStrings.DECREASING;
                            promptReplacements["amount"] = (-change.Amount.Amount).ToString();
                        }
                        else
                        {
                            promptReplacements["increasingDecreasing"] = VehicleSettingsStrings.INCREASING;
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
                    // Binary event (on/off)

                    BotResponse promptTemplate;
                    var promptReplacements = new StringDictionary { { "settingName", change.SettingName } };
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
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmationDenied));
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

        private async Task RunLuisForFollowUp(ITurnContext query, VehicleSettingStage settingStage, AutomotiveSkillState state)
        {
            if (settingStage == VehicleSettingStage.NameSelection)
            {
                state.NameSelectionLuisResult = await vehicleSettingNameSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsNameSelection>(query, CancellationToken.None);
            }
            else if (settingStage == VehicleSettingStage.ValueSelection)
            {
                state.ValueSelectionLuisResult = await vehicleSettingValueSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsValueSelection>(query, CancellationToken.None);
            }                             
        }

    }

    public class VehicleSettingsCardDataBase : CardDataBase
    { }
}
