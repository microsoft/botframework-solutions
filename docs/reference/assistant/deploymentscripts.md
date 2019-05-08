# Reference: PowerShell Deployment Scripts

A number of PowerShell scripts are provided in the Virtual Assistant Template to help deploy and configure your different resources. Please find details on each script's purpose, parameters, and outputs below.

## deploy.ps1

This script orchestrates the deployment of all Azure Resources and Cognitive Models to get the Virtual Assistant running.

### Parameters

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| name | The name for your Azure resources. | Yes |
| location | The region for your Azure resource group and resources. | Yes |
| appPassword | The password for your Microsoft App Registration. If `-appId` is provided this should be the password for your existing Microsoft App Registration. Otherwise, a new registration will be created using this password. | Yes |
| luisAuthoringRegion | The region to deploy LUIS apps`| Yes |
| luisAuthoringKey | The authoring key for the LUIS portal. Must be valid key for `-luisAuthoringRegion`. | Yes |
| resourceGroup | The name for your Azure resource group. Default value is the name parameter. | No
| appId | The application Id for your Microsoft App Registration. | No |
| parametersFile | Optional configuration file for ARM Template deployment. | No |
| languages | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| outFolder | Location to save `appsettings.json` and `cognitivemodels.json` configuration files. Defaults to current directory. | No |
| logFile | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |

## deploy_cognitive_models.ps1

This script deploys all the language models found in `Deployment\Resources\LU` and the knowledgebases found in `Deployment\Resources\QnA`. Finally it creates a Dispatch model to dispatch between all cognitive models.

### Parameters

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| name | The base name for all Cognitive Models. Model language and name will be appended. (e.g MyAssistanten_General )| Yes |
| luisAuthoringRegion | The region to deploy LUIS apps`| Yes |
| luisAuthoringKey | The authoring key for the LUIS portal. Must be valid key for `-luisAuthoringRegion`. | Yes |
| qnaSubscriptionKey | The subscription key for the QnA Maker service. Can be found in the Azure Portal. | Yes |
| languages | Specifies which languages to deploy cognitive models in a comma separated string (e.g. "en-us,de-de,es-es"). Defaults to "en-us". | No |
| outFolder | Location to save `cognitivemodels.json` configuration file. Defaults to current directory. | No |
| logFile | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |

## update_cognitive_models.ps1

This script updates your hosted language models and knowledgebases based on local .lu files. Or, it can update your local .lu files based on your current models. Finally, it refreshes your dispatch model with the latest changes.

| Parameter | Description | Required? |
| --------- | ----------- | --------- |
| configFile | The folder path to the cognitivemodels.json file. Defaults to current directory. | No |
| RemoteToLocal | Flag indicating that local files should be updated based on hosted models. Defaults to false. | No |
| dispatchFolder | The folder path to the .dispatch file. Defaults to `Deployment\Resources\Dispatch` | No |
| luisFolder | The folder path to the .lu files for your LUIS models. Defaults to `Deployment\Resources\LU` | No |
| qnaFolder | The folder path to the .lu files for your QnA Maker knowledgebases. Defaults to `Deployment\Resources\QnA` | No |
| lgOutFolder | The folder path output LuisGen file for your Dispatch model. Defaults `.\Services` | No |
| logFile | Log file for any errors that occur during script execution. Defaults to `Deployment` folder | No |