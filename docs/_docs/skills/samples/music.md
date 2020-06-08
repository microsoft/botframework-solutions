---
category: Skills
subcategory: Samples
language: experimental_skills
title: Music Skill
description: Music Skill provides the ability to select music to be played from Spotify.
order: 7
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Music skill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/experimental/musicskill) integrates with [Spotify](https://developer.spotify.com/documentation/web-api/libraries/) to look up playlists and artists and open the Spotify app via URI.
This is dependent on the [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) wrapper for the Spotify Web API.

## Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

## Configuration
{:.no_toc}

1. Get your own client id and secret when you [create a Spotify client](https://developer.spotify.com/dashboard/).
1. Provide these values in your `appsettings.json` file.

```
  "spotifyClientId": "{YOUR_SPOTIFY_CLIENT_ID}",
  "spotifyClientSecret": "{YOUR_SPOTIFY_CLIENT_SECRET}"
```
