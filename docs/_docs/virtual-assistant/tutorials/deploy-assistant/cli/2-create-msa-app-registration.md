---
layout: tutorial
category: Virtual Assistant
subcategory: Deploy
language: cli
title: Create Microsoft App registration
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{page.title}}

Run the following command to create your app registration:

```
az ad app create `
    --display-name 'your-app-name' `
    --password 'your-app-pw' `
    --available-to-other-tenants `
    --reply-urls 'https://token.botframework.com/.auth/web/redirect'
```
