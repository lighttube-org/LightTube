# Configuration

The configuration file can be found in the following paths:

| Operating System | Path                                    |
| :--------------- | :-------------------------------------- |
| Linux / Docker   | /etc/lighttube.yml                      |
| Windows          | %APPDATA%/lighttube/config.yml          |
| Other            | Current working directory/lighttube.yml |

# Configuration File

## `Interface`

This part configures how LightTube looks

[string] `MessageOfTheDay`: This is the message that shows up in the home page.

## `Credentials`

This part configures how LightTube receives playback data from YouTube.

> WARNING: So far we haven't received any account bans for using these cookies in LightTube, but please don't use your own account's cookies.

[bool] `UseCredentials`: Set to true if you want to use the cookies listed below
[string] `Sapisid`: The `SAPISID` cookie from your browser
[string] `Psid`: The `__Secure-3PSID` cookie from your browser

## `Database`

Self explanatory, settings of the database LightTube connects to

[string] `MongoConnectionString`: Connection string of the MongoDB database

## `Cache`

Limits to the memory cache

[string] `PlayerCacheSize`: Amount of player data that the cache can hold
