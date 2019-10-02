---
category: Virtual Assistant
subcategory: Create a Virtual Assistant
language: C#
title: Download and install
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Download and install

> It's important to ensure all of the following prerequisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Download and install Visual Studio (2017 or 2019) for PC or Mac
1. Download and install the [Virtual Assistant Template](https://marketplace.visualstudio.com/items?itemName=BotBuilder.VirtualAssistantTemplate). *Note that Visual Studio on Mac doesn't support VSIX packages, instead [clone the Virtual Assistant sample from our repository](https://github.com/microsoft/botframework-solutions/tree/master/templates/Virtual-Assistant-Template/csharp/Sample).*
2. Download and install [.NET Core SDK](https://www.microsoft.com/net/download).  
3. Download and install [Node Package manager](https://nodejs.org/en/).
4. Download and install PowerShell Core version 6 (required for cross platform deployment support):
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on MacOS](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-macos?view=powershell-6)
   * [Download PowerShell Core on Linux](https://aka.ms/getps6-linux)
5. Download and install the Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of the latest capabilities:

   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
   ```
6. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

7. Download and install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest) **(Version 2.0.64 minimum required)**.
8. Download and install the [Bot Framework Emulator](https://aka.ms/botframework-emulator).