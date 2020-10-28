---
category: Virtual Assistant
subcategory: Handbook
title: Configure an Azure Application Gateway
description: This explains how to configure and use a bot sample through the public IP of an Azure Application Gateway.
order: 14
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Prerequisites

-  Have a deployed [Virtual Assistant]({{site.baseurl}}/overview/virtual-assistant-solution/)
-  The [BotFramework Emulator](https://github.com/microsoft/BotFramework-Emulator)

## Create an Azure Application Gateway

Follow the document [Tutorial: Create an application gateway with a Web Application Firewall using the Azure portal](https://docs.microsoft.com/en-us/azure/web-application-firewall/ag/application-gateway-web-application-firewall-portal) up to *Review + create tab* section.

You should end up with the following resources in your Azure resource group:

![]({{site.baseurl}}/assets/images/virtualassistant-gateway-resources.png)

## Configure the Gateway's backend pool

Now it is necessary to link your deployed bot to the backend pool of your application gateway. For this follow the steps detailed in [Add App service as backend pool](https://docs.microsoft.com/en-us/azure/application-gateway/configure-web-app-portal#add-app-service-as-backend-pool) and in [Edit HTTP settings for App Service](https://docs.microsoft.com/en-us/azure/application-gateway/configure-web-app-portal#edit-http-settings-for-app-service).

> If you are using a TypeScript bot sample, check [How to configure a TypeScript bot with an Application Gateway]({{site.baseurl}}/help/known-issues#configuring-a-gateway's-health-probe-with-a-typescript-bot).

After overriding the hostname, you can use the gateway's public IP to access your bot.

![]({{site.baseurl}}/assets/images/virtualassistant-gateway-emulator.png)

## Further Reading

- [Deploying a Virtual Assistant](https://microsoft.github.io/botframework-solutions/virtual-assistant/tutorials/deploy-assistant/cli/1-intro/)
- [Troubleshoot backend health issues in Application Gateway](https://docs.microsoft.com/en-us/azure/application-gateway/application-gateway-backend-health-troubleshooting)
- [Troubleshooting bad gateway errors in Application Gateway](https://docs.microsoft.com/en-us/azure/application-gateway/application-gateway-troubleshooting-502)
