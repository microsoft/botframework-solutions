---
category: Reference
subcategory: Samples
title: Virtual Assistant Client (Android) comprehensive feature set
description: Detailed information on the features of the Virtual Assistant Client on Android
order: 1
---


# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Activity UI for testing and demonstrating a bot on the Direct Line Speech channel

* Interaction
    * User may send requests to bot via voice or text
    * Responses from the bot can render text responses and Adaptive Card attachments (single and carousel)
    * Bot responses are audible (controlled by 'Media' volume bar)
    * Volume rocket adjusts 'Media' volume bar without having to select it
    * Adaptive Cards amy be clickable to send a response to bot
    * The listening mode adapts to a bot's input hints and user's input method
    * User may restart a new conversation with the bot
    * User may use "Show Assistant Settings" as a shortcut to the default assist app menu

* Appearance
    * Newest response is rendered at the bottom of the conversation history (scrolled automatically)
    * User may elect to show a threaded conversation (by default only bot responses shown)
    * User may adjust dark/light color scheme

## Settings UI to configure a bot endpoint and experience

* A user can configure:
    * Speech Service subscription key
    * Speech Service subscrition key region
    * Direct Line Speech secret key
    * User From.Id value
    * Locale
    * Chat History line-count
    * Timezone
    * Bot Speech Bubble Background Color
    * User Speech Bubble Background Color
    * Bot Text Color
    * User Text Color
    * GPS Sent on (read-only)
    * Send GPS Location (resent `VA.Location` event activity with GPS coordinates)


## Widgets for demonstrating a native chat experience

 * Users may add resizable widgets to their homescreen by long-pressing and scrolling to the Virtual Assistant Client app
 1. Microphone
    * Trigger the service to listen to user requests
 1. User utterance (request)
    * Echo the user's request in text format
 1. Bot response (response)
    * Show the text response from the bot - does not show Adaptive Cards


## Always-on background service

* Stores state client side
* Interface between clients (widget and Activity UI) and service
* Interface with plug-in apps (via AIDL and broadcasts)
* Sends GPS location periodically
* Receives bot responses and plays audio without showing the Activity UI
* Open default apps (navigation and music) as necessary to fulfill user's request
    * Navigation: attempts to open Waze first. If unavailable, opens via Google Maps.
    * Music: opens Spotify.