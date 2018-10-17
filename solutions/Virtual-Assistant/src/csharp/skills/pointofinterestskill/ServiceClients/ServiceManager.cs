// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PointOfInterestSkill
{
    public class ServiceManager : IServiceManager
    {
        public IGeoSpatialService InitMapsService(string key)
        {
            return new AzureMapsGeoSpatialService(key);
        }
    }
}
