---
layout: tutorial
category: Skills
subcategory: Connect to a sample
title: Download and install
order: 2
---

# Tutorial: {{page.subcategory}} 

## {{ page.title }}

> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the **latest** version.  
1. Download and install [Node Package manager](https://nodejs.org/en/).
   > Node version 10.14.1 or higher is required for the Bot Framework CLI
1. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
1. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities:

   ```
   npm install -g botdispatch @microsoft/botframework-cli
   ```

1. [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1#runtime-2.1.0): ^2.1.0

1. Install Botskills CLI tool:
   
   ```
   npm install -g botskills@latest
   ```

1. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)