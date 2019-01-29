// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill.Common;
using AutomotiveSkill.Dialogs.Shared;
using AutomotiveSkill.Dialogs.VehicleSettings.Resources;
using AutomotiveSkill.Models;
using AutomotiveSkill.ServiceClients;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text;

namespace AutomotiveSkill.Dialogs.VehicleSettings
{
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

        private IHttpContextAccessor _httpContext;

        public VehicleSettingsDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<AutomotiveSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(VehicleSettingsDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            // Initialise supporting LUIS models for followup questions
            vehicleSettingNameSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_name"];
            vehicleSettingValueSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_value"];

            // JSON resource files provided metatadata as to the available car settings, names and the values that can be set
            var resDir = Path.Combine(
                Path.GetDirectoryName(typeof(VehicleSettingsDialog).Assembly.Location),
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
            AddDialog(new WaterfallDialog(Actions.ProcessVehicleSettingChange, processVehicleSettingChangeWaterfall) { TelemetryClient = telemetryClient });

            // Prompts
            AddDialog(new ChoicePrompt(Actions.SettingNameSelectionPrompt, SettingNameSelectionValidator, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
            AddDialog(new ChoicePrompt(Actions.SettingValueSelectionPrompt, SettingValueSelectionValidator, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });

            AddDialog(new ConfirmPrompt(Actions.SettingConfirmationPrompt));

            // Set starting dialog for component
            InitialDialogId = Actions.ProcessVehicleSettingChange;

            // Used to resolve image paths (local or hosted)
            _httpContext = httpContext;
        }

        /// <summary>
        /// Top level processing, is the user trying to check or change a setting?.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        public async Task<DialogTurnResult> ProcessSetting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context, () => new AutomotiveSkillState());

            // Ensure we don't have state from a previous instantiation
            state.Changes.Clear();
            state.Entities.Clear();

            var luisResult = state.VehicleSettingsLuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            switch (topIntent.Value)
            {
                case Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE:
                case Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_DECLARATIVE:

                    // Process the LUIS result and add entities to the State accessors for ease of access
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

                    // Perform post-processing on the entities, if it's declarative we indicate for special processing (opposite of the condition they've expressed)
                    settingFilter.PostProcessSettingName(state, topIntent.Value == Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_DECLARATIVE ? true : false);

                    // Perform content logic and remove entities that don't make sense
                    settingFilter.ApplyContentLogic(state);

                    var settingNames = state.GetUniqueSettingNames();
                    if (!settingNames.Any())
                    {
                        // missing setting name
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsMissingSettingName));
                        return await sc.EndDialogAsync();
                    }
                    else if (settingNames.Count() > 1)
                    {
                        // If we have more than one setting name matching prompt the user to choose
                        var options = new PromptOptions()
                        {
                            Choices = new List<Choice>(),
                        };

                        for (var i = 0; i < settingNames.Count; ++i)
                        {
                            var item = settingNames[i];
                            var choice = new Choice()
                            {
                                Value = item,
                                Synonyms = new List<string> { (i + 1).ToString(), item },
                            };
                            options.Choices.Add(choice);
                        }

                        var card = new HeroCard
                        {
                            Images = new List<CardImage> { new CardImage(GetSettingCardImageUri("settingcog.jpg")) },
                            Text = "Please choose from one of the available settings shown below",
                            Buttons = options.Choices.Select(choice =>
                                new CardAction(ActionTypes.ImBack, choice.Value, value: choice.Value)).ToList(),
                        };

                        options.Prompt = (Activity)MessageFactory.Attachment(card.ToAttachment());

                        return await sc.PromptAsync(Actions.SettingNameSelectionPrompt, options);
                    }
                    else
                    {
                        // Only one setting detected so move on to next stage
                        return await sc.NextAsync();
                    }

                case Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_CHECK:
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply("The skill doesn't support checking vehicle settings quite yet!"));
                    return await sc.EndDialogAsync(true, cancellationToken);

                default:
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                    return await sc.EndDialogAsync(true, cancellationToken);
            }
        }

        /// <summary>
        /// When we've had to prompt the user to clarify the setting name we need to validate the input and run the pre-processing again.
        /// </summary>
        /// <param name="promptContext">Prompt context to validate.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Whether setting was validated.</returns>
        private async Task<bool> SettingNameSelectionValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            if (promptContext.Recognized != null && promptContext.Recognized.Succeeded)
            {
                // The response from the user might be the exact setting name or more likely something like "first one" or "last one" so we need to ensure the activity text (used by the LUIS recognizer) is correct
                // No way to identify this situation so we override
                string settingChoice = promptContext.Recognized.Value.Value;
                promptContext.Context.Activity.Text = settingChoice;

                // Use the value selection LUIS model to perform validation of the users entered setting value
                VehicleSettingsNameSelection nameSelectionResult = await vehicleSettingNameSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsNameSelection>(promptContext.Context, CancellationToken.None);

                if (nameSelectionResult.Entities.SETTING != null)
                {
                    // We have a clarified setting so remove the previous entity extraction and change identification work
                    if (state.Entities.ContainsKey(nameof(nameSelectionResult.Entities.SETTING)))
                    {
                        state.Entities.Remove(nameof(nameSelectionResult.Entities.SETTING));
                    }

                    state.Changes.Clear();

                    state.Entities.Add(nameof(nameSelectionResult.Entities.SETTING), nameSelectionResult.Entities.SETTING);

                    // Perform post-processing on the entities
                    settingFilter.PostProcessSettingName(state);

                    // Perform content logic and remove entities that don't make sense
                    settingFilter.ApplyContentLogic(state);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Once we have a setting we need to process the corresponding value.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> ProcessVehicleSettingsChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            if (state.Changes.Any())
            {
                var settingValues = state.GetUniqueSettingValues();
                if (!settingValues.Any())
                {
                    // This shouldn't happen because the SettingFilter would just add all possible values to let the user select from them.
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                    return await sc.EndDialogAsync();
                }
                else
                {
                    // We have found multiple settings which we need to prompt the user to resolve
                    if (settingValues.Count() > 1)
                    {
                        string settingName = state.Changes.First().SettingName;

                        // If we have more than one setting name matching prompt the user to choose
                        var options = new PromptOptions()
                        {
                            Choices = new List<Choice>(),
                        };

                        for (var i = 0; i < settingValues.Count; ++i)
                        {
                            var item = settingValues[i];
                            var choice = new Choice()
                            {
                                Value = item,
                                Synonyms = new List<string> { (i + 1).ToString(), item },
                            };
                            options.Choices.Add(choice);
                        }

                        BotResponse promptTemplate = VehicleSettingsResponses.VehicleSettingsSettingValueSelectionPre;
                        var promptReplacements = new StringDictionary { { "settingName", settingName } };
                        options.Prompt = sc.Context.Activity.CreateReply(promptTemplate, ResponseBuilder, promptReplacements);

                        var card = new HeroCard
                        {
                            Images = new List<CardImage> { new CardImage(GetSettingCardImageUri("settingcog.jpg")) },
                            Text = options.Prompt.Text,
                            Buttons = options.Choices.Select(choice =>
                                new CardAction(ActionTypes.ImBack, choice.Value, value: choice.Value)).ToList(),
                        };

                        options.Prompt.Attachments.Add(card.ToAttachment());

                        return await sc.PromptAsync(Actions.SettingValueSelectionPrompt, options);
                    }
                    else
                    {
                        // We only have one setting value so proceed to next step
                        return await sc.NextAsync();
                    }
                }
            }
            else
            {
                // No setting value was understood
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                return await sc.EndDialogAsync();
            }
        }

        /// <summary>
        /// Take the users input for setting validation and validate it matches the chosen setting - e.g. off for park assist or 21c for temperature.
        /// </summary>
        /// <param name="promptContext">Prompt Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Whether prompt value was validated.</returns>
        private async Task<bool> SettingValueSelectionValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            if (promptContext.Recognized != null && promptContext.Recognized.Succeeded)
            {
                // The response from the user might be the exact setting name or more likely something like "first one" or "last one" so we need to ensure the activity text (used by the LUIS recognizer) is correct
                // No way to identify this situation so we override
                string valueChoice = promptContext.Recognized.Value.Value;
                promptContext.Context.Activity.Text = valueChoice;

                // Use the value selection LUIS model to perform validation of the users entered setting value
                VehicleSettingsValueSelection valueSelectionResult = await vehicleSettingValueSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsValueSelection>(promptContext.Context, CancellationToken.None);

                List<string> valueEntities = new List<string>();
                if (valueSelectionResult.Entities.VALUE != null)
                {
                    valueEntities.AddRange(valueSelectionResult.Entities.VALUE);
                }
                else if (valueSelectionResult.Entities.SETTING != null)
                {
                    valueEntities.AddRange(valueSelectionResult.Entities.SETTING);
                }

                var selectedValue = settingFilter.ApplySelectionToSettingValues(state, valueEntities);
                // We identified a setting value, proceed
                if (selectedValue != null)
                {
                    state.Changes = selectedValue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Process the change that we are about to perform. If required the user is prompted for confirmation.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        private async Task<DialogTurnResult> ProcessChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                        var promptReplacements = new StringDictionary
                        {
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

        private async Task<DialogTurnResult> SendChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    var promptReplacements = new StringDictionary
                    {
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

        private async Task SendActionToDevice(WaterfallStepContext sc, SettingChange setting, StringDictionary settingDetail)
        {
            // remove whitespace to create Event Name and prefix with Automotive Skill
            string reducedName = $"AutomotiveSkill.{Regex.Replace(setting.SettingName, @"\s+", string.Empty)}";

            var actionEvent = sc.Context.Activity.CreateReply();
            actionEvent.Type = ActivityTypes.Event;
            actionEvent.Name = reducedName;
            actionEvent.Value = settingDetail;

            await sc.Context.SendActivityAsync(actionEvent);
        }

        private string GetSettingCardImageUri(string imagePath)
        {
            string serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
            return $"{serverUrl}/images/{imagePath}";
        }
    }
}