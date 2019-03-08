// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace AutomotiveSkill.Dialogs.VehicleSettings.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class VehicleSettingsResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string VehicleSettingsMissingSettingName = "VehicleSettingsMissingSettingName";
        public const string VehicleSettingsSettingNameSelection = "VehicleSettingsSettingNameSelection";
        public const string VehicleSettingsMissingSettingValue = "VehicleSettingsMissingSettingValue";
        public const string VehicleSettingsSettingValueSelection = "VehicleSettingsSettingValueSelection";
        public const string VehicleSettingsSettingChangeConfirmation = "VehicleSettingsSettingChangeConfirmation";
        public const string VehicleSettingsSettingChangeConfirmationDenied = "VehicleSettingsSettingChangeConfirmationDenied";
        public const string VehicleSettingsSettingChangeNoOpValue = "VehicleSettingsSettingChangeNoOpValue";
        public const string VehicleSettingsSettingChangeNoOpAmount = "VehicleSettingsSettingChangeNoOpAmount";
        public const string VehicleSettingsSettingChangeUnsupported = "VehicleSettingsSettingChangeUnsupported";
        public const string VehicleSettingsChangingRelativeAmount = "VehicleSettingsChangingRelativeAmount";
        public const string VehicleSettingsChangingAmount = "VehicleSettingsChangingAmount";
        public const string VehicleSettingsChangingValue = "VehicleSettingsChangingValue";
        public const string VehicleSettingsChangingValueKnown = "VehicleSettingsChangingValueKnown";
        public const string VehicleSettingsCheckingStatus = "VehicleSettingsCheckingStatus";
        public const string VehicleSettingsCheckingStatusValueSuccess = "VehicleSettingsCheckingStatusValueSuccess";
        public const string VehicleSettingsCheckingStatusAmountSuccess = "VehicleSettingsCheckingStatusAmountSuccess";
        public const string VehicleSettingsCheckingStatusUnsupported = "VehicleSettingsCheckingStatusUnsupported";
        public const string VehicleSettingsOutOfDomain = "VehicleSettingsOutOfDomain";
    }
}