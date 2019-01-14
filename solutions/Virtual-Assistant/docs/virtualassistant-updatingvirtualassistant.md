# Updating an existing Virtual Assistant Deployment

In order to update an existing Virtual Assistant deployment with the latest language models from the repository, run the following PowerShell script:

```
...PowerShell.exe -ExecutionPolicy Bypass -File DeploymentScripts\update_published_models.ps1
```

By default, this will update all domain models for all language configuration files in your `LocaleConfigurations` folder. If you want to update a specific file for a specific language, add the `-locales` and `-domains` parameters like so:

```
...PowerShell.exe -ExecutionPolicy Bypass -File DeploymentScripts\update_published_models.ps1 -locales "en-us" -domains "general,calendar,email,todo,pointofinterest,dispatch"
```

This script updates your published models and saves the previous version with the id `backup`. In case of any issues with the updates models, you can revert your changes by making `backup` the active version in the LUIS portal.