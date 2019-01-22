using System;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using static Google.Apis.Gmail.v1.UsersResource;

namespace EmailSkillTest.API.Fakes.Google
{
    public class MockUsersResource
    {
        public class MockGetProfileRequest : GetProfileRequest, IClientServiceRequest<Profile>
        {
            public MockGetProfileRequest(IClientService service, string userId)
                : base(service, userId)
            {
            }

            public new Profile Execute()
            {
                if (UserId != "me")
                {
                    throw new Exception("User ID not support");
                }

                var profile = new Profile
                {
                    EmailAddress = "test@test.com"
                };

                return profile;
            }
        }
    }
}