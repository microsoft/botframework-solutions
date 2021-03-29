---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous deployment
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}
{:.no_toc}

### Purpose
To run a release pipeline, you must generate an artifact based on a recent build. But what is an artifact? It's a compressed version of the project or solution which contains all of the necessary information to create the base of a release pipeline configuration.

### Prerequisites
- Enable continuous integration by creating a build pipeline with either:
- [C# resources]({{site.baseurl}}/solution-accelerators/tutorials/enable-continuous-integration/csharp/1-intro/)
- [TypeScript resources]({{site.baseurl}}/solution-accelerators/tutorials/enable-continuous-integration/typescript/1-intro/)

### Time To Complete
15 minutes

### Scenario
A personalized release pipeline in **Azure DevOps**. This tutorial is based on a sample Virtual Assistant in the [Bot Framework Solutions repository]({{site.repo}}).

For further information, read [Release Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/?view=azure-devops).