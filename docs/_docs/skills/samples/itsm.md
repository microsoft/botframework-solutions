---
category: Skills
subcategory: Samples
language: experimental_skills
title: IT Service Managment (ITSM) Skill
description: IT Service Management Skill provides ability to work with typical Help Desk Ticketing scenarios for ServiceNow.
order: 6
toc: true
---

# {{ page.title }}
{:.no_toc}

The [IT Service Management skill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/experimental/itsmskill) provides a basic skill that provides ticket and knowledge base related capabilities and supports SerivceNow.

This skill demonstrates the following scenarios:
- Create a ticket: *Create a ticket for my broken laptop*
- Show ticket: *What's the status of my incident*
- Update Ticket: *Change ticket's urgency to high*
- Close a ticket: *Close my ticket*
- Find Knowledgebase item: *Search knowledgebase for error lost connection*

An example transcript file demonstrating the Skill in action can be found [here]({{site.baseurl}}/assets/transcripts/skills-itsm.transcript), you can use the Bot Framework Emulator to open transcripts.

## Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

## Configuration
{:.no_toc}

To test this skill you will need to follow the ServiceNow configuration steps shown below:

- Create a ServiceNow instance in the [ServiceNow Developer Site](https://developer.servicenow.com/app.do#!/instance).
- Update this configuration entry in your `appsettings.json` file with your Service Now instance URL:
`"serviceNowUrl": "{YOUR_SERVICENOW_INSTANCE_URL}`
- Create a [scripted REST API](https://docs.servicenow.com/bundle/geneva-servicenow-platform/page/integrate/custom_web_services/task/t_CreateAScriptedRESTService.html) to get current user's sys_id and please raise an issue if simpler way is found
    - In System Web Services/Scripted REST APIs, click New to create an API
    - In API's Resources, click New to add a resource
    - In the resource, select GET for HTTP method and input `(function process(/*RESTAPIRequest*/ request, /*RESTAPIResponse*/ response) { return gs.getUserID(); })(request, response);` in Script
    - Update the serviceNowGetUserId of appsetting.json: `"serviceNowGetUserId": "YOUR_API_NAMESPACE/YOUR_API_ID"`
    ![ServiceNow Developer Portal Screenshot]({{site.baseurl}}/assets/images/itsm_servicenow_developer_portal.png)
- Register an Application and OAuth configuration by following [these instructions](https://docs.servicenow.com/bundle/london-platform-administration/page/administer/security/task/t_CreateEndpointforExternalClients.html#t_CreateEndpointforExternalClients). Keep the generated Client ID and Client Secret to be used in the following OAuth Connection step.
    - Redirect URL is https://token.botframework.com/.auth/web/redirect
- Add an OAuth Connection in the Settings pane of your Web App Bot named 'ServiceNow' using Service Provider 'Generic Oauth 2'
    - Set Authorization URL to the following, replacing YOUR_INSTANCE with your instance name: https://YOUR_INSTANCE.service-now.com/oauth_auth.do
    - Set Token URL, Refresh URL to the following, replacing YOUR_INSTANCE with your instance name: https://YOUR_INSTANCE.service-now.com/oauth_token.do
    - No Scopes are needed
    - Click Test Connection to verify the connection works as expected.

To test this skill with your Virtual Assistant one manual step is required over and above the usual skill connection steps.

- Add OAuth Connection to your Virtual Assistant manually as per the step above. This connection type cannot be automatically configured as part of botskills.
