---
category: Overview
title: What is a Bot Framework Skill?
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

Bot Framework Skills are re-usable conversational skill building-blocks covering conversational use-cases enabling you to add extensive functionality to a Bot within minutes. Skills include LUIS models, Dialogs and Integration code and delivered in source code form enabling you to customize and extend as required. At this time we provide Calendar, Email, To Do, Point of Interest skills and a number of other experimental skills.

A Skill is like a standard conversational bot but with the ability to be plugged in to a broader solution. This can be a complex Virtual Assistant or perhaps an Enterprise Bot seeking to stitch together multiple bots within an organization.

Apart from some minor differences that enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach. Skills for common scenarios like productivity and navigation to be used as-is or customized however a customer prefers.

>The Skill implementations currently provided are in C# only but the remote invocation nature of the Skills does enable you to invoke C# based Skills from a typescript Bot project.

## Available Skill samples

The following Skill samples are available out of the box, each with deployment steps required to deploy and configure Skills for your use.

<div class="card-deck">
    <div class="card">
        <div class="card-body">
        <img src="{{site.baseurl}}/assets/images/icons/calendar-skill.png" alt="Calendar icon" width="48px">
            <h4 class="card-title no_toc">Calendar Skill</h4>
            <p class="card-text">Get up and running with the Calendar Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/skills/samples/productivity-calendar" class="btn btn-primary">Learn more</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
        <img src="{{site.baseurl}}/assets/images/icons/email-skill.png" alt="Email icon" width="48px">
            <h4 class="card-title no_toc">Email Skill</h4>
            <p class="card-text">Get up and running with the Email Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/skills/samples/productivity-email" class="btn btn-primary">Learn more</a>
        </div>
    </div>
</div>

<div class="card-deck">
    <div class="card">
        <div class="card-body">
        <img src="{{site.baseurl}}/assets/images/icons/todo-skill.png" alt="To Do icon" width="48px">
            <h4 class="card-title no_toc">To Do Skill</h4>
            <p class="card-text">Get up and running with the To Do Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/skills/samples/productivity-todo" class="btn btn-primary">Learn more</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
        <img src="{{site.baseurl}}/assets/images/icons/point-of-interest-skill.png" alt="Point of Interest icon" width="48px">
            <h4 class="card-title no_toc">Point of Interest Skill</h4>
            <p class="card-text">Get up and running with the Point of Interest Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/skills/samples/pointofinterest" class="btn btn-primary">Learn more</a>
        </div>
    </div>
</div>

<div class="card-deck">
    <div class="card">
        <div class="card-body">
            <img src="{{site.baseurl}}/assets/images/icons/experimental-skill.png" alt="Experimental icon" width="48px">
            <h4 class="card-title no_toc">Experimental Skills</h4>
            <p class="card-text">Get up and running with the additional experimental Skill samples.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/skills/samples/experimental" class="btn btn-primary">Learn more</a>
        </div>
    </div>
</div>

## Next steps

<div class="card-deck">
    <div class="card">
        <div class="card-body">
            <img src="{{site.baseurl}}/assets/images/icons/csharp.png" alt="C# icon" width="48px">
            <h4 class="card-title no_toc">Create a Skill</h4>
            <p class="card-text">Get up and running with the solution accelerator.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Get started</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
            <img src="{{site.baseurl}}/assets/images/icons/typescript.png" alt="Typescript icon" width="48px">
            <h4 class="card-title no_toc">Create a Skill</h4>
            <p class="card-text">Personalize your experience for your brand and customers.</p>
        </div>
        <div class="card-footer no_toc" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/typescript/create-skill/1_intro" class="btn btn-primary">Get started</a>
        </div>
    </div>
</div>
