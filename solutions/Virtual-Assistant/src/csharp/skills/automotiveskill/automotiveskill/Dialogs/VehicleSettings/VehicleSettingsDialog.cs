// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text;

namespace AutomotiveSkill.Dialogs.VehicleSettings
{
    public class VehicleSettingsDialog : AutomotiveSkillDialog
    {
        private const string FallbackSettingImageFileName = "Black_Car.png";
        private const string AvailableSettingsFileName = "available_settings.json";
        private const string AlternativeSettingsFileName = "setting_alternative_names.json";

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
            ResponseManager responseManager,
            IStatePropertyAccessor<AutomotiveSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(VehicleSettingsDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            // Initialise supporting LUIS models for followup questions
            vehicleSettingNameSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_name"];
            vehicleSettingValueSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_value"];

            // Initialise supporting LUIS models for followup questions
            vehicleSettingNameSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_name"];
            vehicleSettingValueSelectionLuisRecognizer = services.LocaleConfigurations["en"].LuisServices["settings_value"];

            // Supporting setting files are stored as embeddded resources
            Assembly resourceAssembly = typeof(VehicleSettingsDialog).Assembly;

            var settingFile = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(AvailableSettingsFileName))
                .First();

            var alternativeSettingFileName = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains(AlternativeSettingsFileName))
                .First();

            if (string.IsNullOrEmpty(settingFile) || string.IsNullOrEmpty(alternativeSettingFileName))
            {
                throw new FileNotFoundException($"Unable to find Available Setting and/or Alternative Names files in \"{resourceAssembly.FullName}\" assembly.");
            }

            settingList = new SettingList(resourceAssembly, settingFile, alternativeSettingFileName);
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

            var luisResult = state.VehicleSettingsLuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            switch (topIntent.Value)
            {
                case Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE:
                case Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_DECLARATIVE:

                    // Perform post-processing on the entities, if it's declarative we indicate for special processing (opposite of the condition they've expressed)
                    settingFilter.PostProcessSettingName(state, topIntent.Value == Luis.VehicleSettings.Intent.VEHICLE_SETTINGS_DECLARATIVE ? true : false);

                    // Perform content logic and remove entities that don't make sense
                    settingFilter.ApplyContentLogic(state);

                    var settingNames = state.GetUniqueSettingNames();
                    if (!settingNames.Any())
                    {
                        // missing setting name
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsMissingSettingName));
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
                            List<string> synonyms = new List<string>();
                            synonyms.Add(item);
                            synonyms.Add((i + 1).ToString());
                            synonyms.AddRange(settingList.GetAlternativeNamesForSetting(item));
                            var choice = new Choice()
                            {
                                Value = item,
                                Synonyms = synonyms,
                            };
                            options.Choices.Add(choice);
                        }

                        options.Prompt = ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsSettingNameSelection);

                        var card = new ThumbnailCard
                        {
                            Images = new List<CardImage> { new CardImage(GetSettingCardImageUri(FallbackSettingImageFileName)) },
                            Text = options.Prompt.Text,
                            Buttons = options.Choices.Select(choice =>
                                new CardAction(ActionTypes.ImBack, choice.Value, value: choice.Value)).ToList(),
                        };

                        options.Prompt.Attachments.Add(card.ToAttachment());

                        // Default Text property is clumsy for speech
                        options.Prompt.Speak = $"{options.Prompt.Text} {GetSpeakableOptions(options.Choices)}";

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
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
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

            // Use the name selection LUIS model to perform validation of the user's entered setting name
            VehicleSettingsNameSelection nameSelectionResult = await vehicleSettingNameSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsNameSelection>(promptContext.Context, CancellationToken.None);
            state.AddRecognizerResult(nameSelectionResult);

            List<string> selectedSettingNames = new List<string>();
            if (nameSelectionResult.Entities.SETTING != null)
            {
                selectedSettingNames.AddRange(nameSelectionResult.Entities.SETTING);
            }
            else if (promptContext.Recognized.Value != null && promptContext.Recognized.Value.Value != null)
            {
                selectedSettingNames.Add(promptContext.Recognized.Value.Value);
            }

            if (selectedSettingNames.Any())
            {
                var selectedChanges = settingFilter.ApplySelectionToSettings(state, selectedSettingNames, state.Changes);

                if (selectedChanges != null)
                {
                    state.Changes = selectedChanges;
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
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsMissingSettingValue));
                    return await sc.EndDialogAsync();
                }
                else
                {
                    // We have found multiple setting values, which we need to prompt the user to resolve
                    if (settingValues.Count() > 1)
                    {
                        string settingName = state.Changes.First().SettingName;
                        var setting = this.settingList.FindSetting(settingName);

                        // If an image filename is provided we'll use it otherwise fall back to the generic car one
                        string imageName = setting.ImageFileName ?? FallbackSettingImageFileName;

                        // If we have more than one setting value matching, prompt the user to choose
                        var options = new PromptOptions()
                        {
                            Choices = new List<Choice>(),
                        };

                        for (var i = 0; i < settingValues.Count; ++i)
                        {
                            var item = settingValues[i];
                            List<string> synonyms = new List<string>();
                            synonyms.Add(item);
                            synonyms.Add((i + 1).ToString());
                            synonyms.AddRange(settingList.GetAlternativeNamesForSettingValue(settingName, item));
                            var choice = new Choice()
                            {
                                Value = item,
                                Synonyms = synonyms,
                            };
                            options.Choices.Add(choice);
                        }

                        var promptReplacements = new StringDictionary { { "settingName", settingName } };
                        options.Prompt = ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsSettingValueSelection, promptReplacements);

                        var card = new ThumbnailCard
                        {
                            Text = options.Prompt.Text,
                            Images = new List<CardImage> { new CardImage(GetSettingCardImageUri(imageName)) },
                            Buttons = options.Choices.Select(choice =>
                                new CardAction(ActionTypes.ImBack, choice.Value, value: choice.Value)).ToList(),
                        };

                        options.Prompt.Attachments.Add(card.ToAttachment());

                        // Default Text property is clumsy for speech
                        options.Prompt.Speak = $"{options.Prompt.Text} {GetSpeakableOptions(options.Choices)}";

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
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsOutOfDomain));
                return await sc.EndDialogAsync();
            }
        }

        /// <summary>
        /// Take the users input for setting value selection and validate it matches the chosen setting value - e.g. Off for Parking Assistance.
        /// </summary>
        /// <param name="promptContext">Prompt Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Whether prompt value was validated.</returns>
        private async Task<bool> SettingValueSelectionValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(promptContext.Context);

            // Use the value selection LUIS model to perform validation of the users entered setting value
            VehicleSettingsValueSelection valueSelectionResult = await vehicleSettingValueSelectionLuisRecognizer.RecognizeAsync<VehicleSettingsValueSelection>(promptContext.Context, CancellationToken.None);
            state.AddRecognizerResult(valueSelectionResult);

            List<string> valueEntities = new List<string>();
            if (valueSelectionResult.Entities.VALUE != null)
            {
                valueEntities.AddRange(valueSelectionResult.Entities.VALUE);
            }
            else if (valueSelectionResult.Entities.SETTING != null)
            {
                valueEntities.AddRange(valueSelectionResult.Entities.SETTING);
            }
            else if (promptContext.Recognized.Value != null && promptContext.Recognized.Value.Value != null)
            {
                valueEntities.Add(promptContext.Recognized.Value.Value);
            }

            if (valueEntities.Any())
            {
                var selectedChanges = settingFilter.ApplySelectionToSettingValues(state, valueEntities);

                // We identified a setting value, proceed
                if (selectedChanges != null)
                {
                    state.Changes = selectedChanges;
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

                        // TODO - Explore moving to ConfirmPrompt following usability testing
                        var prompt = ResponseManager.GetResponse(promptTemplate, promptReplacements);
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
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                    return await sc.EndDialogAsync();
                }
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsSettingChangeUnsupported));
                return await sc.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> SendChange(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);

            var change = state.Changes[0];
            var settingChangeConfirmed = false;

            // If we skip the ConfirmPrompt due to no confirmation needed then Result will be NULL
            if (sc.Result == null)
            {
                settingChangeConfirmed = true;
            }
            else
            {
                settingChangeConfirmed = (bool)sc.Result;
                change.IsConfirmed = settingChangeConfirmed;
            }

            if (settingChangeConfirmed)
            {
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

                        // Send an event to the device along with the text
                        await SendActionToDevice(sc, change);

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                            VehicleSettingsResponses.VehicleSettingsChangingRelativeAmount, promptReplacements));
                    }
                    else
                    {
                        // Send an event to the device along with the text
                        await SendActionToDevice(sc, change);

                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(
                            VehicleSettingsResponses.VehicleSettingsChangingAmount, promptReplacements));
                    }
                }
                else
                {
                    // Nominal (non-numeric) change (e.g., on/off)
                    string promptTemplate;
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

                    // Send an event to the device along with the text
                    await SendActionToDevice(sc, change);

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(promptTemplate, promptReplacements));
                }
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(VehicleSettingsResponses.VehicleSettingsSettingChangeConfirmationDenied));
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

        /// <summary>
        /// Send an event activity to communicate to the client which change to make to the actual setting.
        /// This event is meant to be processed by client code rather than shown to the user.
        /// </summary>
        /// <param name="sc">The WaterfallStepContext.</param>
        /// <param name="change">The change that we want the client to make.</param>
        /// <returns>A Task.</returns>
        private async Task SendActionToDevice(WaterfallStepContext sc, SettingChange change)
        {
            var actionEvent = sc.Context.Activity.CreateReply();
            actionEvent.Type = ActivityTypes.Event;
            // The name of the event is the intent (changing vs checking, the latter of which is not yet supported).
            actionEvent.Name = "AutomotiveSkill.SettingChange";
            actionEvent.Value = change;

            await sc.Context.SendActivityAsync(actionEvent);
        }

        private string GetSettingCardImageUri(string imagePath)
        {
            // If we are in local mode we leverage the HttpContext to get the current path to the image assets
            if (_httpContext != null)
            {
                string serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
                return $"{serverUrl}/images/{imagePath}";
            }
            else
            {
                // In skill-mode we don't have HttpContext and require skills to provide their own storage for assets
                Services.Properties.TryGetValue("ImageAssetLocation", out var imageUri);

                var imageUriStr = (string)imageUri;
                if (string.IsNullOrWhiteSpace(imageUriStr))
                {
                    throw new Exception("ImageAssetLocation Uri not configured on the skill.");
                }
                else
                {
                    return $"{imageUriStr}/{imagePath}";
                }
            }
        }

        private string GetSpeakableOptions(IList<Choice> choices)
        {
            IList<string> speakableChoices = new List<string>();
            for (int i = 0; i < choices.Count(); ++i)
            {
                // The dash makes the voice take a short break, which is what a human would do when reading out a numbered list.
                speakableChoices.Add($"{(i + 1).ToString()} - {choices[i].Value.ToString()}.");
            }

            return string.Join(", ", speakableChoices);
        }
    }
}