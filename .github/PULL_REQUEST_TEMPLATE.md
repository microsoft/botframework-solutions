<!--- This repository only accepts pull requests related to open issues, please link the open issue in description below. See https://help.github.com/articles/closing-issues-using-keywords/ to learn about automation. 
For example - Close #123: Description goes here. -->
### Purpose
*What is the context of this pull request? Why is it being done?*

### Changes
*Are there any changes that need to be called out as significant or particularly difficult to grasp? (Include illustrative screenshots for context if applicable.)*

### Tests
*Is this covered by existing tests or new ones? If no, why not?*

### Feature Plan
*Are there any remaining steps or dependencies before this issue can be fully resolved? If so, describe and link to any relevant pull requests or issues.*

### Checklist

#### General
- [ ] I have commented my code, particularly in hard-to-understand areas	
- [ ] I have added or updated the appropriate tests	
- [ ] I have updated related documentation

#### Bots
- [ ] I have validated that new and updated responses use appropriate [Speak](https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-text-to-speech?view=azure-bot-service-3.0) and [InputHint](https://docs.microsoft.com/en-us/azure/bot-service/dotnet/bot-builder-dotnet-add-input-hints?view=azure-bot-service-3.0) properties to ensure a high-quality speech-first experience
- [ ] I have replicated language model changes across the English, French, Italian, German, Spanish, and Chinese `.lu` files and validated that deployment is successful

#### Deployment Scripts
- [ ] I have replicated my changes in the **Virtual Assistant Template** and **Sample** projects
- [ ] I have replicated my changes in the **Skill Template** and **Sample** projects
