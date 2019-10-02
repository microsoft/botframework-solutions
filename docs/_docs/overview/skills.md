---
category: Overview
title: What is a Bot Framework Skill?
order: 2
---

# {{ page.title }}
Bot Framework Skills are re-usable conversational skill building-blocks covering conversational use-cases enabling you to add extensive functionality to a Bot within minutes. Skills include LUIS models, Dialogs and Integration code and delivered in source code form enabling you to customize and extend as required. At this time we provide Calendar, Email, To Do, Point of Interest skills and a number of other experimental skills.

A Skill is like a standard conversational bot but with the ability to be plugged in to a broader solution. This can be a complex Virtual Assistant or perhaps an Enterprise Bot seeking to stitch together multiple bots within an organization.

Apart from some minor differences that enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach. Skills for common scenarios like productivity and navigation to be used as-is or customized however a customer prefers.

>The Skill implementations currently provided are in C# only but the remote invocation nature of the Skills does enable you to invoke C# based Skills from a typescript Bot project.

## Available Skill samples

The following Skill samples are available out of the box, each with deployment steps required to deploy and configure Skills for your use.

<div class="card-group">
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Calendar Skill</h4>
            <p class="card-text">Get up and running with the Calendar Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Learn more</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Email Skill</h4>
            <p class="card-text">Get up and running with the Email Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Learn more</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">To Do Skill</h4>
            <p class="card-text">Get up and running with the To Do Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Learn more</a>
        </div>
    </div>
        <div class="card">
        <div class="card-body">
            <h4 class="card-title">Point of Interest Skill</h4>
            <p class="card-text">Get up and running with the Point of Interest Skill sample.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Learn more</a>
        </div>
    </div>
            <div class="card">
        <div class="card-body">
            <h4 class="card-title">Experimental Skills</h4>
            <p class="card-text">Get up and running with the additional experimental Skill samples.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Learn more</a>
        </div>
    </div>
</div>


## Next steps
{:.no_toc}

<div class="card-group">
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Create a Skill (C#)</h4>
            <p class="card-text">Get up and running with the solution accelerator.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/csharp/create-skill/1_intro" class="btn btn-primary">Get Started</a>
        </div>
    </div>
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Create a Skill (TypeScript)</h4>
            <p class="card-text">Personalize your experience for your brand and customers.</p>
        </div>
        <div class="card-footer" style="display: flex; justify-content: center;">
            <a href="{{site.baseurl}}/tutorials/typescript/create-skill/1_intro" class="btn btn-primary">Get Started</a>
        </div>
    </div>
</div>
