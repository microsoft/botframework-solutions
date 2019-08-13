# IT Service Managerment Experimental Skill

To test this skill, one has to setup the following:

* Create a ServiceNow instance in https://developer.servicenow.com/app.do#!/instance and update the serviceNowUrl of appsettings.json
* Set up a scripted REST API for current user's sys_id following https://community.servicenow.com/community?id=community_question&sys_id=52efcb88db1ddb084816f3231f9619c7 and update the serviceNowGetUserId of appsetting.json
	- Please raise an issue if simpler way is found
* Set up endpoint (https://docs.servicenow.com/bundle/london-platform-administration/page/administer/security/task/t_CreateEndpointforExternalClients.html#t_CreateEndpointforExternalClients) for Client id and Client secret in the following OAuth Connection
    - Redirect URL is https://token.botframework.com/.auth/web/redirect
* Add an OAuth Connection in the Settings of Web App Bot named 'ServiceNow' with Service Provider 'Generic Oauth 2'
    - Authorization URL as https://instance.service-now.com/oauth_auth.do
    - Token URL, Refresh URL as https://instance.service-now.com/oauth_token.do

Once this skill is done, these will be moved into the Experimental Skill [documentation page](/docs/reference/skills/experimental.md).
