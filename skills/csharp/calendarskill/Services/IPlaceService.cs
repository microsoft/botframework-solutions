// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::CalendarSkill.Models;

    /// <summary>
    /// The place API interface.
    /// </summary>
    public interface IPlaceService
    {
        /// <summary>
        /// Get the meetings with the specified words in the title.
        /// </summary>
        /// <param name="title">the meeting title should contains the title.</param>
        /// <returns>the meetings list.</returns>
        Task<List<PlaceModel>> GetMeetingRoomByTitleAsync(string title);

        Task<List<PlaceModel>> GetMeetingRoomAsync();
    }
}
