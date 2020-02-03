---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous integration
title: Configure the build steps
language: csharp
order: 4
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{ page.title }}
{:.no_toc}

1. With the **YAML** file configured, visit your **Azure DevOps** team, select **Pipelines**, then **Builds**.
![Screenshot highlighting how to access Builds on Azure Pipelines]({{site.baseurl}}/assets/images/pipelines-build.png)

1. Select the **New** option and connect to where your code is hosted. For this tutorial we will use **Github (YAML)**.
![Screenshot highlighting where to create a new build pipeline]({{site.baseurl}}/assets/images/configure-new-pipeline.png)

1. Under **Connect**, select your repository.
![Select a repository]({{site.baseurl}}/assets/images/select-repository.png)

1. Under **Configure**, select **Exsiting Azure Pipelines YAML file**.
![Configure your pipeline]({{site.baseurl}}/assets/images/configure-pipeline.png)

1. Provide the location of the **YAML** file created earlier, also including the correct branch if necessary.
![Select an existing YAML file]({{site.baseurl}}/assets/images/branch-yaml.png)

1. The build pipeline is created successfully and you will see the configuration of the **YAML** file. Now select **Run** to start the build process.
![Run the build process]({{site.baseurl}}/assets/images/run-pipeline.png)
