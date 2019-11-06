// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBookingSkill.Utilities
{
    public interface IUrlResolver
    {
        string ServerUrl { get; }

        string GetImageUrl(string imagePath);
    }
}
