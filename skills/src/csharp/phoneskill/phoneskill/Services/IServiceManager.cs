// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using PhoneSkill.Common;
using PhoneSkill.Models;

namespace PhoneSkill.Services
{
    public interface IServiceManager
    {
        IContactProvider GetContactProvider(string token, ContactSource source);
    }
}
