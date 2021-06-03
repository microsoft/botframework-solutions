---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous integration
title: Intro
language: csharp
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}
{:.no_toc}

### Purpose
Learn how to create a build pipeline on **Azure DevOps** using a **YAML** file as configuration, as it's an easy way to configure one or many specific branches with an existing **YAML** or creating a new one. You can add different scripts using the tools of **Azure Pipelines** or manually write the different tasks your build pipeline needs to execute.

### Prerequisites
- [Sign up for Azure DevOps](https://docs.microsoft.com/en-us/azure/devops/user-guide/sign-up-invite-teammates?view=azure-devops). 
- Select a project to create a build pipeline for either:
  - [Create a Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro)
  - [Create a Skill]({{site.baseurl}}/skills/tutorials/create-skill/csharp/1-intro)
- Create a repository for you to host your source code.

### Time To Complete
15 minutes

### Scenario
A personalized build pipeline in **Azure DevOps** usign a **YAML** file. This tutorial is based on a sample Virtual Assistant in the [Bot Framework Solutions repository]({{site.repo}}).

For further information, read [What is Azure Pipelines?](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops).