// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace VirtualAssistantSample.Controllers
{
    [Route("api/shareduserdata")]
    [ApiController]
    public class SharedUserDataController : ControllerBase
    {
        private readonly IOptions<Models.UserSharedSkillproperties> _optionAccessor;

        public SharedUserDataController(IOptions<Models.UserSharedSkillproperties> optionsAccessor)
        {
            _optionAccessor = optionsAccessor;
        }

        [HttpGet]
        public IActionResult SharedUserData()
        {
            return Content(JsonConvert.SerializeObject(_optionAccessor.Value), "application/json");
        }
    }
}