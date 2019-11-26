// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Moq;

namespace ToDoSkill.Tests.API.Fakes
{
    public static class MockConfiguration
    {
        public static IConfiguration GetConfiguration()
        {
            var customizedListTypeItem = new Mock<IConfigurationSection>();
            customizedListTypeItem.Setup(x => x.Value).Returns("Homework");

            var customizedListType = new Mock<IConfigurationSection>();
            customizedListType.Setup(x => x.GetChildren()).Returns(new List<IConfigurationSection> { customizedListTypeItem.Object });

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x.GetSection("customizeListTypes")).Returns(customizedListType.Object);

            return configuration.Object;
        }
    }
}
