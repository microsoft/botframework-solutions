﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using Microsoft.Bot.Builder;
using Moq;

namespace EmailSkill.Tests.API.Fakes
{
    public class MockEmailStateAccessor
    {
        private readonly Mock<IStatePropertyAccessor<EmailSkillState>> mockEmailStateAccessor;

        public MockEmailStateAccessor()
        {
            this.mockEmailStateAccessor = new Mock<IStatePropertyAccessor<EmailSkillState>>();
            this.InitializeDefaultData();
            this.SetMockBehavior();
        }

        public EmailSkillState MockEmailSkillState { get; set; }

        public void InitializeDefaultData()
        {
            this.MockEmailSkillState = new EmailSkillState
            {
                FindContactInfor = new EmailSkillState.FindContactInformation()
            };

            this.MockEmailSkillState.FindContactInfor.Contacts = new List<Microsoft.Graph.Recipient>();
        }

        public void SetMockBehavior()
        {
            this.mockEmailStateAccessor.Setup(f => f.GetAsync(It.IsAny<ITurnContext>(), null, default(CancellationToken))).Returns(Task.FromResult(this.MockEmailSkillState));
        }

        public Mock<IStatePropertyAccessor<EmailSkillState>> GetMock()
        {
            return this.mockEmailStateAccessor;
        }
    }
}
