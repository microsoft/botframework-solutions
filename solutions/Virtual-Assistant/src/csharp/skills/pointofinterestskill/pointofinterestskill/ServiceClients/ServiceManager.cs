// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PointOfInterestSkill.ServiceClients
{
    public class ServiceManager : IServiceManager
    {
        public IGeoSpatialService InitMapsService(string key, string locale = "en")
        {
            return new AzureMapsGeoSpatialService(key, locale);
        }
    }
}