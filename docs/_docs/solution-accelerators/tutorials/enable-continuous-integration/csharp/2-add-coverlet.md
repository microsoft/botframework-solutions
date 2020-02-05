---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous integration
title: Add Coverlet to the project
language: csharp
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}
{:.no_toc}

First add **Coverlet** package (named **coverlet.msbuild**) to your project. This will enable the **Code Coverage** report to show in your build pipeline.

1. In **Visual Studio**, open your project that needs the **Coverlet** package.

1. Go to **Tools** > **NuGet Package Manager** > Manage NuGet Packages for Solution...**
![Manage NuGet Packages for Solution]({{site.baseurl}}/assets/images/tools.png)

1. Select **Browse** and search for **Coverlet**, installing the **coverlet.msbuild** package to your project.
![Add NuGet Package]({{site.baseurl}}/assets/images/add-nuget.png)

1. Build the solution and **Coverlet** will be added as a dependency in the project
