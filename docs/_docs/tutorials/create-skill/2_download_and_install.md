---
category: Tutorials
subcategory: Create a skill
language: csharp
title: Download and install
order: 2
---

# Tutorial: Create a Skill

## Download and install

> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Install the [Skill Template](https://marketplace.visualstudio.com/items?itemName=BotBuilder.BotSkillTemplate). *Note that Visual Studio on Mac doesn't support VSIX packages, instead [clone the Skill Template sample from our repository](https://github.com/microsoft/botframework-solutions/tree/master/templates/Skill-Template/csharp/Sample).*
2. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the latest version.  
3. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
4. Download and install PowerShell Core version 6 (required for cross platform deployment support):
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on MacOS](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-6)
   * [Download PowerShell Core on Linux](https://aka.ms/getps6-linux)
5. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities:

   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
   ```

6. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

7. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)