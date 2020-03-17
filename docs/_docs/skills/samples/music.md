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

The [Music skill]({{site.repo}}/tree/master/skills/csharp/experimental/musicskill) integrates with [Spotify](https://developer.spotify.com/documentation/web-api/libraries/) to look up playlists and artists and open the Spotify app via URI.
This is dependent on the [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) wrapper for the Spotify Web API.

This skill has a very limited LUIS model (available in English and Chinese) and demonstrates a simple scenarios:

- Play music by given info, e.g. artist name
  - _play lady gaga_
  - _play iron maiden_
  - _play million reasons_


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

### From assistant to user
{:.no_toc}

This Skill supports an outgoing **OpenDefaultApp** Event Activity that provides a Spotify URI like *spotify:playlist:5xpSGrkfR2MsYDfkjJixFb* for chat clients to determine how to play the music.

```json
{ 
   "type":"event",
   "name":"OpenDefaultApp",
   "value":{ 
      "MusicUri":"{Spotify URI to play}"
   }
}
```