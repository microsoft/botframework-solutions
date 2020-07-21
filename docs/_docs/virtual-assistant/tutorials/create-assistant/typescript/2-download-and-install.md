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
1. Download and install [PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-6).
1. Download and install Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions because the Virtual Assistant makes use of the latest capabilities: 

   ```shell
   npm install -g botdispatch @microsoft/botframework-cli
   ```

1. Install Botskills CLI tool:
   
   ```
   npm install -g botskills@latest
   ```

1. Install [Yeoman](http://yeoman.io)

   ```shell
   npm install -g yo
   ```

1. Download and install the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest) (Minimum version 2.2.0 required).
1. Download and install the [Bot Framework Emulator](https://aka.ms/botframework-emulator).