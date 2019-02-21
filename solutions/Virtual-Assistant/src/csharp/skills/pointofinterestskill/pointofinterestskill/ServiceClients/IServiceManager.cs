// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Skills;

namespace PointOfInterestSkill.ServiceClients
{
    public interface IServiceManager
    {
        IGeoSpatialService InitMapsService(SkillConfigurationBase services, string locale = "en-us");

        IGeoSpatialService InitAddressMapsService(SkillConfigurationBase services, string locale = "en-us");

        IGeoSpatialService InitRoutingMapsService(SkillConfigurationBase services, string locale = "en-us");
    }
}