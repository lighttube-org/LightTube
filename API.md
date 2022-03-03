# LightTube XML API

## GET `/api/player`

Gets player data. Contains YouTube stream urls (without a proxy), and video information.

### Params

| Name | Required | Description     |
| ---- | -------- | --------------- |
| `v`  | true     | ID of the video |

## GET `/api/video`

Gets all available video data and recommendations for the next video. Returns a mostly empty result in age-restricted videos.

### Params

| Name | Required | Description     |
| ---- | -------- | --------------- |
| `v`  | true     | ID of the video |

## GET `/api/search`

Does a YouTube search. Can paginate using a continuation key.

### Params

| Name           | Required                               | Description                       |
| -------------- | -------------------------------------- | --------------------------------- |
| `query`        | true, if `continuation` is not present | Query to search with              |
| `continuation` | true, if `query` is not present        | Continuation key of a search page |

## GET `/api/channel`

Gets the YouTube channel page. Can paginate using a continuation key.

### Params

| Name           | Required                               | Description                                                  |
| -------------- | -------------------------------------- | ------------------------------------------------------------ |
| `id`           | true, if `continuation` is not present | ID of the YouTube channel                                    |
| `tab`          | false                                  | The tab to load the page of. One of `home`, `videos`, `playlists`, `community`, `channels`, `about` |
| `continuation` | true, if `id` is not present           | Continuation key of a search page                            |

## GET `/api/playlist`

Gets videos from a playlist. Can paginate using a continuation key.

### Params

| Name           | Required                               | Description                       |
| -------------- | -------------------------------------- | --------------------------------- |
| `id`           | true, if `continuation` is not present | ID of the playlist                |
| `continuation` | true, if `id` is not present           | Continuation key of a search page |

## GET `/api/locals`

Gets all available languages & regions

## Applying localization

Using the `X-Content-Language` and `X-Content-Region` headers, or `hl` (language) and `gl` (region) cookies, you can change the language of the API results. You can get the available language and region IDs from `/api/locals` endpoint