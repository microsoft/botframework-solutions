---
layout: tutorial
category: Solution Accelerators
subcategory: View analytics with Power BI
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}}

## {{ page.title }}

### Purpose
The Virtual Assistant analytics sample provides a Power BI template that can be used to understand how your bot is performing.

<div id="powerbi-carousel" class="carousel slide" data-ride="carousel">
  <div class="carousel-inner">
    <div class="carousel-item active">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-1.png" class="card-img-top" alt="Overall usage">
        <div class="card-body">
            <h4 class="card-title">Overall usage</h4>
            <p class="card-text">Personalize your experience for your brand and customers.</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-2.png" class="card-img-top" alt="All dialogs overview">
        <div class="card-body">
            <h4 class="card-title">All dialogs overview</h4>
            <p class="card-text">All dialogs' popularity and status based off of SDK telemetry</p>
        </div>
        </div>
    </div>
        <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-3.png" class="card-img-top" alt="Dialog overview">
        <div class="card-body">
            <h4 class="card-title">Dialog overview</h4>
            <p class="card-text">Review a specific dialog's popularity and status</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-4.png" class="card-img-top" alt="LUIS intents">
        <div class="card-body">
            <h4 class="card-title">LUIS intents</h4>
            <p class="card-text">A count of LUIS intents per day</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-5.png" class="card-img-top" alt="All conversation metrics">
        <div class="card-body">
            <h4 class="card-title">All conversation metrics</h4>
            <p class="card-text">Highlights the average number of conversations per unique user and the average duration by day</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-6.png" class="card-img-top" alt="Conversations drill down">
        <div class="card-body">
            <h4 class="card-title">Conversations drill down</h4>
            <p class="card-text">Per conversation, this highlights the dialogs triggered and common utterances</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-7.png" class="card-img-top" alt="Transcript">
        <div class="card-body">
            <h4 class="card-title">Transcript</h4>
            <p class="card-text">Review interactions, sessions, and the transcript between a bot and its users</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-8.png" class="card-img-top" alt="Demographics">
        <div class="card-body">
            <h4 class="card-title">Demographics</h4>
            <p class="card-text">See where users are connecting to your bot</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-9.png" class="card-img-top" alt="Word cloud">
        <div class="card-body">
            <h4 class="card-title">Word Cloud</h4>
            <p class="card-text">Commonly user queries</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-10.png" class="card-img-top" alt="Sentiment analysis">
        <div class="card-body">
            <h4 class="card-title">Sentiment analysis</h4>
            <p class="card-text">Average user sentiment results provided by LUIS</p>
        </div>
        </div>
    </div>
    <div class="carousel-item">
    <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-11.png" class="card-img-top" alt="QnA Maker insights">
        <div class="card-body">
            <h4 class="card-title">QnA Maker insights</h4>
            <p class="card-text">Insights on matched user queries with QnA Maker</p>
        </div>
        </div>
    </div>
        <div class="carousel-item">
        <div class="card">
        <img src="{{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-12.png" class="card-img-top" alt="User feedback">
        <div class="card-body">
            <h4 class="card-title">User feedback</h4>
            <p class="card-text">Insights on user submitted feedback</p>
        </div>
        </div>
    </div>
  </div>
  <a class="carousel-control-prev" href="#powerbi-carousel" role="button" data-slide="prev">
    <span class="carousel-control-prev-icon" aria-hidden="true"></span>
    <span class="sr-only">Previous</span>
  </a>
  <a class="carousel-control-next" href="#powerbi-carousel" role="button" data-slide="next">
    <span class="carousel-control-next-icon" aria-hidden="true"></span>
    <span class="sr-only">Next</span>
  </a>
</div>


### Prerequisites
* [Install Power BI Desktop](https://powerbi.microsoft.com/desktop/)
* [Download the Virtual Assistant analytics Power BI template]({{site.baseurl}}/assets/analytics/virtual-assistant-analytics-sample.pbit)
* [Create a Virtual Assistant]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro) to deploy your Azure resources

### Time To Complete
10 minutes

### Scenario
A Power BI dashboard showing Application Insights telemetry captured from a Virtual Assistant.