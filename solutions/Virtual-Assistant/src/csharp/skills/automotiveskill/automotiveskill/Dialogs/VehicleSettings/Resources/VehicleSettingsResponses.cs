// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace AutomotiveSkill.Dialogs.VehicleSettings.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class VehicleSettingsResponses
    {
        private static readonly ResponseManager _responseManager;

        static VehicleSettingsResponses()
        {
            var dir = Path.GetDirectoryName(typeof(VehicleSettingsResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\VehicleSettings\Resources");
            _responseManager = new ResponseManager(resDir, "VehicleSettingsResponses");
        }

        // Generated accessors
        public static BotResponse VehicleSettingsMissingSettingName => GetBotResponse();

        public static BotResponse VehicleSettingsSettingNameSelection => GetBotResponse();

        public static BotResponse VehicleSettingsMissingSettingValue => GetBotResponse();

        public static BotResponse VehicleSettingsSettingValueSelection => GetBotResponse();

        public static BotResponse VehicleSettingsSettingValueSelectionPre => GetBotResponse();

        public static BotResponse VehicleSettingsSettingValueSelectionPost => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeConfirmation => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeConfirmationWithCategory => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeConfirmationDenied => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeNoOpValue => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeNoOpAmount => GetBotResponse();

        public static BotResponse VehicleSettingsSettingChangeUnsupported => GetBotResponse();

        public static BotResponse VehicleSettingsChangingRelativeAmount => GetBotResponse();

        public static BotResponse VehicleSettingsChangingAmount => GetBotResponse();

        public static BotResponse VehicleSettingsChangingValue => GetBotResponse();

        public static BotResponse VehicleSettingsChangingValueKnown => GetBotResponse();

        public static BotResponse VehicleSettingsCheckingStatus => GetBotResponse();

        public static BotResponse VehicleSettingsCheckingStatusValueSuccess => GetBotResponse();

        public static BotResponse VehicleSettingsCheckingStatusAmountSuccess => GetBotResponse();

        public static BotResponse VehicleSettingsCheckingStatusUnsupported => GetBotResponse();

        public static BotResponse VehicleSettingsOutOfDomain => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}