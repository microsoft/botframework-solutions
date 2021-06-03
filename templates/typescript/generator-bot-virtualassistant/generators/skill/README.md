# Creation of a TypeScript Skill using the generator

## Create your Skill project

1. Execute the generator to create a TypeScript Skill
    ```bash
    yo bot-virtualassistant:skill
    ```

### Generate the skill using prompts
  - `What's the name of your skill? (sample-skill)`
    > The name of your skill (also used as your project's name and for the root folder's name)
  - `What's the description of your skill? ()`
    > The description of your skill
  - `Which languages will your skill use? (by default takes all the languages`
    - [x] Chinese (`zh-cn`)
    - [x] Deutsch (`de-de`)
    - [x] English (`en-us`)
    - [x] French (`fr-fr`)
    - [x] Italian (`it-it`)
    - [x] Spanish (`es-es`)
  - `Do you want to change the new skill's location?`
    > A confirmation to change the destination for the generation
    - `Where do you want to generate the skill? (by default takes the path where you are running the generator)`
        > The destination path for the generation
  - `Looking good. Shall I go ahead and create your new skill?`
    > Final confirmation for creating the desired skill

### Generate the sample using CLI parameters

| Option | Description |
|--------|-------------|
| -n, --skillName [name] | Unique name of new skill (by default takes `sample-skill`) |
| -d, --skillDesc [description] | Description of the new skill (by default is empty) |
| -l, --skillLang [languages] | Languages for the new skill. Possible values are `de-de`, `en-us`, `es-es`, `fr-fr`, `it-it`, `zh-cn` (by default takes all the languages) |
| -p, --skillGenerationPath [path] | Destination path for the new skill (by default takes the path where you are runnning the generator) |
| --noPrompt | Indicates to avoid the prompts |

#### Example

```bash
yo bot-virtualassistant:skill -n "Skill" -d "A description for my new skill" -l "en-us,es-es" -p "<SKILL_GENERATION_PATH>" --noPrompt
```

You can check the summary in your screen:
```bash
Current values for the new skill:
Name: skill
Description: A description for my new skill
Selected languages: en-us,es-es
Path: <SKILL_GENERATION_PATH>
```

## What files were created?
    | - deployment                          // Files for deployment and provisioning
        | - resources                       // Resources for deployment and provisioning
            | - LU                          // Files for deploying LUIS language models
                | - general.lu              // General language model (e.g. Cancel, Help, Escalate, etc.)
                | - skill.lu                // Sample language model for your skill
            | - template.json               // ARM Deployment template
            | - parameters.template.json    // ARM Deployment parameters file
        | - scripts                         // PowerShell scripts for deployment and provisioning
            | - deploy.ps1                  // Deploys and provisions Azure resources and cognitive models
            | - deploy_cognitive_models.ps1 // Deploys and provisions cognitive models only
            | - update_cognitive_models.ps1 // Updates existing cognitive models
            | - luis_functions.ps1          // Functions used for deploying and updating LUIS models
            | - qna_functions.ps1           // Functions used for deploying and updating QnA Maker knowledgebases
            | - publish.ps1                 // Script to publish your Bot to Azure
    | - pipeline                            // Files for setting up an deployment pipeline in Azure DevOps
        | - skill.yml                       // Sample build pipeline template for Azure DevOps
    | - src                                 // Folder which contains all the Skill code before compilation
        | - adapters                        // BotAdapter implementations for configuring Middleware
            | - defaultAdapter.ts           // Configures basic middleware
        | - bots                            // IBot implementations for initializing dialog stack
            | - defaultActivityHandler.ts   // Initializes the dialog stack with a primary dialog (e.g. mainDialog)
        | - dialogs                         // Bot Framework Dialogs
            | - mainDialog.ts               // Dialog for routing incoming messages
            | - sampleAction.ts             // Sample action which prompts user for name
            | - sampleDialog.ts             // Sample dialog which prompts user for name
            | - skillDialogBase.ts          // Dialog base class for shared steps and config
        | - manifest                        // Source of manifests
            | - manifest-1.0.json           // Manifest version 1.0
            | - manifest-1.1.json           // Manifest version 1.1
        | - models                          // Data models
            | - skillState.ts               // Model for storing skill state
            | - stateProperties.ts          // Constants for state property keys
        | - responses                       // Classes and files for representing bot responses
            | - AllResponses.lg             // Combined language generation templates
            | - MainResponses.lg            // Language generation templates for Main Dialog responses
            | - SampleResponses.lg          // Language generation templates for Sample Dialog responses
        | - services                        // Configuration for connected services and service clients
            | - botServices.ts              // Class representation of service clients and recognizers
            | - botSettings.ts              // Class representation of configuration files
        | - appsettings.json                // Configuration for application and Azure services
        | - cognitivemodels.json            // Configuration for language models, knowledgebases, and dispatch model
        | - index.ts                        // Initializes dependencies

## License

MIT © [Microsoft](http://dev.botframework.com)