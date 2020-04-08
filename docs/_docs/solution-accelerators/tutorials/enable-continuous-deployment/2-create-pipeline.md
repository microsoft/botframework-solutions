---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous deployment
title: Create a release pipeline
order: 2
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}
{:.no_toc}

1. Visit your **Azure DevOps** team, select **Releases**, then **New** > **New release pipeline**.

 ![Create Release Pipeline 1]({{site.baseurl}}/assets/images/create_release_pipeline_1.png)

1. Select the option to add an artifact, this will show different options to manage the configuration like:
 - Project
 - Name
 - Specific artifact (*Note: you can only select one after a build pipeline has been successfully executed)

 ![Create Release Pipeline 2]({{site.baseurl}}/assets/images/create_release_pipeline_2.png)

1. After the artifact is selected, configure the deployment stage by selecting Azure App Service deployment.

 ![Create Release Pipeline 3]({{site.baseurl}}/assets/images/create_release_pipeline_3.png)

1. In this case to verify the functionality of release configuration, we added an example task to check if the release configuration works without problems and verify the artifact configuration was compressed successfully in the build pipeline with the project information.

 ![Create Release Pipeline 4]({{site.baseurl}}/assets/images/create_release_pipeline_4.png)

1. Select **Create new release**, verify that the correct stage and artifact will be run and select **Create**.

 ![Create Release Pipeline 5]({{site.baseurl}}/assets/images/create_release_pipeline_5.png)

1. After the release is executed, check the log of each task added to the Agent Job. This will show you if any tasks run into warnings or errors.

 ![Create Release Pipeline 6]({{site.baseurl}}/assets/images/create_release_pipeline_6.png)
