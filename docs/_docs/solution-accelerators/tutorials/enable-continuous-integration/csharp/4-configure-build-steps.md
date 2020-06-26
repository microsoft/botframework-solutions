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

To use the YAML file which contains the pipeline's information, select **Pipelines** from the **Pipelines options** of your Azure Organization.

Select the **New Pipeline** option to create a new pipeline.

The creation's process consists of 4 steps:
    1. **Connect**: select YAML as host of the Pipeline's code
    1. **Select**: select a repository
    1. **Configure**: configure the pipeline consuming an Azure Pipeline YAML file
    1. **Review**: review the pipeline YAML

1. **Connect**: select YAML as host of the pipeline's code
We will choose where our Pipeline’s code is. In this case, we will use GitHub YAML as we have the structure in that file.

    ![Screenshot highlighting where to create a new build pipeline]({{site.baseurl}}/assets/images/configure-new-pipeline.png)

1. **Select**: select a repository
Once we are in the Build window, we should choose where our Pipeline’s code is. In this case, we will use botframework-solutions repository as we have our YAML file there.

    ![Select a repository]({{site.baseurl}}/assets/images/select-repository.png)

1. **Configure**: configure the pipeline consuming an Azure Pipeline YAML file
As soon as we have our YAML ready to be imported, we will select the existing Azure Pipeline YAML file.
After this, we will provide the location of the YAML file, including the branch where the file is.

    ![Configure your pipeline]({{site.baseurl}}/assets/images/configure-pipeline.png)

1. **Review**: review the pipeline YAML
The YAML file will be correctly imported so you can create, customize and update the pipeline’s variables in order to run the pipeline.

    ![Select an existing YAML file]({{site.baseurl}}/assets/images/review-pipeline.png)

1. The build pipeline is created successfully and you will see the configuration of the **YAML** file. Now select **Run** to start the build process.

    ![Run the build process]({{site.baseurl}}/assets/images/run-build.png)