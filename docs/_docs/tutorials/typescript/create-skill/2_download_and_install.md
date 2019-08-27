---
category: Tutorials
subcategory: Create a skill
language: TypeScript
title: Download and install
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Download and install

> It's important to ensure all of the following prerequisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Download and install the [Node Package Manager (NPM)](https://nodejs.org/en/).
2. Download and install PowerShell Core version 6 (required for cross platform deployment support).
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
3. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities: 
   
   ```
   npm install -g botdispatch ludown@1.2.0 luis-apis qnamaker luisgen@2.0.2
   ```

4. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

5. Install [Yeoman](http://yeoman.io)

   ```
   npm install -g yo
   ```

6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest).