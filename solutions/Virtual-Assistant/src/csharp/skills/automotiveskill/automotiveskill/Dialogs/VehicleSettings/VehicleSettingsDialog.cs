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

            // Retrieve teh settings and names JSON files supporting the skill processing
            var dir = Path.GetDirectoryName(typeof(VehicleSettingsDialog).Assembly.Location);
            var resDir = Path.Combine(dir, "Dialogs\\VehicleSettings\\Resources\\");

            settingList = new SettingList(resDir + "available_settings.json", resDir + "setting_alternative_names.json");
            settingFilter = new SettingFilter(settingList);

            // Setting Change waterfall
            var settingChange = new WaterfallStep[]
            {
                ProcessVehicleSettingsChange,
                ProcessChange,
                SendChange
            };
            AddDialog(new WaterfallDialog(Actions.ProcessSetting, settingChange));

            // Prompts
            AddDialog(new TextPrompt(Actions.SettingSelectionPrompt, settingSelection));
            AddDialog(new TextPrompt(Actions.SettingConfirmationPrompt, settingConfirmation));

            // Set starting dialog for component
            InitialDialogId = Actions.ProcessSetting;
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
            var topIntent = luisResult?.TopIntent().intent;

            if (topIntent == null)
            {
                return await sc.EndDialogAsync(true);
            }

            switch (topIntent.Value)
            {
                case VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE:

                    return await sc.BeginDialogAsync(Actions.ProcessSetting);

                    break;

                case VehicleSettings.Intent.VEHICLE_SETTINGS_CHECK:

                    //return await ProcessVehicleSettingsCheck(sc);

                    break;

                default:
                    // Out of domain - TODO - Validate why this is needed
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                    return await sc.EndDialogAsync(true, cancellationToken);
            }

            return await sc.EndDialogAsync(null, cancellationToken);
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

            settingFilter.Filter(state, new RecognizerResultWrapper(state.RawLuis));

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

        /// <summary>
        /// Process the change that we are about to perform. If required the user is prompted for confirmation
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DialogTurnResult> ProcessChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            // Any state changes?
            if (state.Changes.Any())
            {
                // TODO Detect no-op changes before asking for confirmation.
                var change = state.Changes[0];
                switch (change.OperationStatus)
                {
                    // The change will have no affect.
                    case SettingOperationStatus.NO_OP:

                        var tokens = new StringDictionary
                        {
                            { "settingName", change.SettingName }
                        };

                        BotResponse noOpTemplate;
                        if (change.Amount != null)
                        {
                            noOpTemplate = VehicleSettingsResponses.VehicleSettingsSettingChangeNoOpAmount;
                            tokens["amount"] = change.Amount.Amount.ToString();
                            tokens["unit"] = UnitToString(change.Amount.Unit);
                        }
                        else
                        {
                            noOpTemplate = VehicleSettingsResponses.VehicleSettingsSettingChangeNoOpValue;
                            tokens["value"] = change.Value;
                        }

                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(noOpTemplate, ResponseBuilder, tokens));
                        state.DialogStateType = VehicleSettingStage.None;

                        return await sc.EndDialogAsync();

                    // Make the actual change.
                    case SettingOperationStatus.TO_DO:

                        var availableSetting = this.settingList.FindSetting(change.SettingName);
                        var availableSettingValue = this.settingList.FindSettingValue(availableSetting, change.Value);

                        // Check confirmation first.
                        if (availableSettingValue != null && availableSettingValue.RequiresConfirmation && !change.IsConfirmed)
                        {
                            if (state.RawLuis != null && "SETTING_CHANGE_CONFIRMATION_NO".Equals(state.RawLuis.GetTopScoringIntent().intent))
                            {
                                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmationDenied));
                                state.DialogStateType = VehicleSettingStage.None;
                                return await sc.EndDialogAsync();
                            }

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
                            var prompt = sc.Context.Activity.CreateReply(promptTemplate, ResponseBuilder, promptReplacements);
                            return await sc.PromptAsync(Actions.SettingConfirmationPrompt, new PromptOptions { Prompt = prompt });
                        }
                        
                        break;

                    // Setting changed successfully.
                    case SettingOperationStatus.SUCCESSFUL:
                        // There is nothing to say. Could relay to the user that it was successful.
                        state.DialogStateType = VehicleSettingStage.None;
                        return await sc.EndDialogAsync();
                        break;

                    // All the unsuccessful & unsupported cases. TODO add more specific dialog responses.
                    default:
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported, ResponseBuilder, new StringDictionary { { "settingName", change.SettingName } }));
                        state.DialogStateType = VehicleSettingStage.None;
                        return await sc.EndDialogAsync();
                }

                return await sc.NextAsync();
            }
            else
            {
                //?
                return await sc.EndDialogAsync();
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

                    var actionEvent = sc.Context.Activity.CreateReply();
                    actionEvent.Type = ActivityTypes.Event;
                    actionEvent.Name = change.SettingName;
                    actionEvent.Value = promptReplacements;
                    await sc.Context.SendActivityAsync(actionEvent);

                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(
                        VehicleSettingsResponses.VehicleSettingsChangingRelativeAmount, ResponseBuilder));
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(
                        VehicleSettingsResponses.VehicleSettingsChangingAmount, ResponseBuilder, promptReplacements));

                    var actionEvent = sc.Context.Activity.CreateReply();
                    actionEvent.Type = ActivityTypes.Event;
                    actionEvent.Name = change.SettingName;
                    actionEvent.Value = promptReplacements;

                    await sc.Context.SendActivityAsync(actionEvent);
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

                var actionEvent = sc.Context.Activity.CreateReply();
                actionEvent.Type = ActivityTypes.Event;
                actionEvent.Name = change.SettingName;
                actionEvent.Value = promptReplacements;

                await sc.Context.SendActivityAsync(actionEvent);

                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(promptTemplate, ResponseBuilder, promptReplacements));

            }

            return await sc.EndDialogAsync();
        }

        private async Task<bool> settingSelection(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);
            await RunLuisForFollowUp(promptContext.Context, state);

            settingFilter.Filter(state, new RecognizerResultWrapper(state.RawLuis));

            return true;
        }

        private async Task<bool> settingConfirmation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return true;
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

        private async Task<DialogTurnResult> ProcessVehicleSettingsCheck(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            var setting = state.Statuses[0];
            switch (setting.OperationStatus)
            {
                // Have the status.
                case SettingOperationStatus.SUCCESSFUL:
                case SettingOperationStatus.NO_OP:
                    var tokens = new StringDictionary
                        {
                            { "settingName", setting.SettingName }
                        };
                    BotResponse successTemplate;
                    if (setting.Amount != null)
                    {
                        successTemplate = VehicleSettingsResponses.VehicleSettingsCheckingStatusAmountSuccess;
                        tokens["amount"] = setting.Amount.Amount.ToString();
                        tokens["unit"] = UnitToString(setting.Amount.Unit);
                    }
                    else
                    {
                        successTemplate = VehicleSettingsResponses.VehicleSettingsCheckingStatusValueSuccess;
                        tokens["value"] = setting.Value;
                    }
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(successTemplate, ResponseBuilder, tokens));
                    state.DialogStateType = VehicleSettingStage.None;
                    return await sc.EndDialogAsync();
                    break;
                // Need to fetch the status.
                case SettingOperationStatus.TO_DO:
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsCheckingStatus, ResponseBuilder, new StringDictionary { { "settingName", setting.SettingName } }));
                    //await sc.PromptAsync(VehicleSettingsDialog.EventPromptDialog, new PromptOptions { Prompt = SettingsEvents.CreateSettingStatusRequestEvent(setting.SettingName) });
                    // TODO FIXME Remove the temperature & unsupported mocks. Remove the mock once the client actually starts sending these events back.
                    //return await MockStatusResponse(dc, setting);
                    // All the unsuccessful & unsupported cases. TODO add more specific dialog responses.
                    break;
                default:
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsCheckingStatusUnsupported, ResponseBuilder, new StringDictionary { { "settingName", setting.SettingName } }));
                    state.DialogStateType = VehicleSettingStage.None;
                    return await sc.EndDialogAsync();
            }

            return await sc.EndDialogAsync(null, cancellationToken);
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
                state.RawLuis = luisResult;
                state.AddRecognizerResult(luisResult, false);
            }
        }

    }

    public class VehicleSettingsCardDataBase : CardDataBase
    { }
}
