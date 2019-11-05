---
layout: tutorial
category: Virtual Assistant
subcategory: Customize
language: TypeScript
title: Edit your responses
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Edit your responses

Each dialog within your assistant contains a set of responses stored in supporting resource (`.json`) files inside the `./src/locales/` directory. You can use Visual Studio Code to modify how your assistant responds by editing the corresponding `.json` file for the desired language.

![]({{ site.baseurl }}/assets/images/quickstart-virtualassistant-editresponses-json.png)

This approach supports multi-lingual responses using the i18next library to manage the different resources needed to each language. Read more on [i18next's documentation.](https://www.i18next.com/)