# Virtual Assistant and Skill Template Deployment

## Deployment Steps 

> Applies to Virtual Assistant Template and Skill Template (C# and Typescript variants)

1. In **PowerShell Core** (pwsh.exe), change to the project directory.
1. Run the following command:
    ```
    .\Deployment\Scripts\deploy.ps1
    ```
2. Provide values for the following parameters to the script, note that PowerShell uses a single `-` to denote parameters: e.g. `\Deployment\Scripts\deploy.ps1 -name 'YOUR_UNIQUE_BOT_NAME' -location 'westus' -appPassword 'YOUR_AD_APP' -luisAuthoringKey 'YOUR_AUTHORING_KEY'`

    Parameter | Description | Required
    --------- | ----------- | --------
    name | Name for your bot. By default this name will be used as the base name for all your Azure Resources. | **Yes**
    location | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    appPassword | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    luisAuthoringKey | The authoring key for your LUIS account. It can be found at https://www.luis.ai/user/settings or https://www.luis.ai/user/settings | **Yes**
    resourceGroup | Name of the Azure Resource Group. Default value is name parameter. | No
    appId | The appId of an existing MSA App Registration. If left blank, a new app will be provisioned automatically. | No
    parametersFile | A .json file that can overwrite the default values of the Azure Resource Manager template. | No
    outFolder | Output directory for created appsettings.json and cognitivemodels.json files. Default value is current directory. | No

> There is a known issue with some users whereby you might experience the following error when running deployment `Could not provision Microsoft App Registration automatically. Please provide the -appId and -appPassword arguments for an existing app and try again`. In this situation, please create your own [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) and manually create a new application retrieving the ApplicationID and Password/Secret. Run the above deployment script again but provide two new arguments `appId` and `appPassword` passing the values you've just retrieved.

> NOTE: Take special care when providing the appSecret step above as special characters (e.g. @) can cause parse issues. Ensure you wrap these parameters in single quotes.

## Customising deployment using the parameters file

## Creating your own ARM template

We have provided a comprehensive ARM template to deploy all required capabilities which can be customised through the parameters file detailed in the previous section. If however you want to make more substantial changes - such as re-using existing deployed services please refer to the [Azure Resource Manager template documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authoring-templates) for guidance and you can refer to our template for reference.