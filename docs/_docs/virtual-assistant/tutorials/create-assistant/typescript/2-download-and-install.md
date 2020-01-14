---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: typescript
title: Download and install
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Download and install

1. Download and install the [Node Package Manager (NPM)](https://nodejs.org/en/).
4. Download and install [PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-6).
3. Download and install Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions because the Virtual Assistant makes use of the latest capabilities: 

   ```shell
   npm install -g botdispatch ludown luis-apis luisgen qnamaker@1.3.1
   ```

4. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

5. Install [Yeoman](http://yeoman.io)

   ```shell
   npm install -g yo
   ```

6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest).
8. Download and install the [Bot Framework Emulator](https://aka.ms/botframework-emulator).