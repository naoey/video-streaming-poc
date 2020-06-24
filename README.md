# Video stream builder PoC

An app built to accept a series of images and build it into a live video stream that is also retained as a video on demand
resource on disk. This repository contains code to demonstrate building video streams on-the-fly from input images and playing
it back in a browser.

## Running the app

### Requirements

The app can be run on Windows, macOS or any Linux distribution supported by .NET Core.

1. NodeJS >= 12.16.1
2. yarn >= 1.22.4
3. .NET Core SDK >= 3.1.200
4. ffmpeg >= 4.2.2 **(must be accessible on `$PATH`)**

### Configuration

The following variables are required to be set in the executing session for the app to function:
- `VAP_OUTPUT_ROOT`: The directory where video stream files are outputted to and where the webserver for serving the videos to the UI must run.
- `VAP_STATIC_SERVER_HOST`: The host address of the web server serving the outputted video stream files publicly. Is prepended to `publicUri` fields of
[`StreamInfo`](./video-streaming-poc/Streams/StreamInfo.cs)s so that clients can access the playlists.

The following variables are optional and will use defaut values if omitted:
- `VAP_STREAM_SEGMENT_DURATION = 10`: The value passed through to ffmpeg as the HLS segment duration. The number of new images required to build
a stream segment will be this value multiplied by the stream's `fps` value.

### Setup

All the following commands assume the repository root as current working directory.

1. `dotnet restore` to restore NuGet dependencies.
2. `cd video-streaming-poc/ClientApp; yarn install; cd -;` to install JavaScript dependencies.
3. `dotnet run --project video-streaming-poc/video-streaming-poc.csproj` to run the application.

An additional web server is required to serve the video files generated by the application. `http-server` is a quick and simple option:
1. `yarn global install http-server`
2. `cd $VAP_OUTPUT_ROOT`
3. `http-server -p 13000 --cors`

Note that `VAP_STATIC_SERVER_HOST` must be set to point to this webserver (in this case the value would be `http://localhost:13000`).

Any web server can be used for this purpose, only point of note being CORS must be allow access from the web app.

### Client

Playing back the live stream requires a compatible browser that is supported for HLS playback by video.js or any application that can play back HLS streams.

Web app included int he app can be accessed at `https://localhost:5001`. Navigate to the "Live stream" link in the navbar. When streams are available a list is
displayed in the UI and clicking any of them begins playback of that stream.

### Server

Once the app is running, the API and web application will be accessible at `https://localhost:5001`.

## REST API

The app provides 4 REST endpoints for managing video streams.

### `GET /streams`

Returns an array of [`StreamInfo`](./video-streaming-poc/Streams/StreamInfo.cs) objects representing a list of currently running streams.

### `POST /stream`

Instantiates a new stream with the source directory for input images. The directory must already exist and contain a manifest file specifying
required stream parameters. The lifetime of the stream is determined by the `IsAlive` value of the manifest file. The stream is finalised and terminated
when that value becomes `false`. It must be `true` when this API is being called to instantiate a stream.

Request JSON model is [`StreamInfo`](./video-streaming-poc/Streams/StreamInfo.cs). Example request JSON:
```js
{
  // Mandatory field. Directory must exist already and contain a file at its root named
  // "manifest" which contains the stream manifest data.
  "fileSystemInputPath": "/Users/naoey/Desktop/streams_test_inputs/input_01"
}
```

Refer to the [building streams](#building-streams) section for more information on manifest formats and building video streams.

Returns the [`StreamInfo`](./video-streaming-poc/Streams/StreamInfo.cs) object of the newly instantiated stream.

### `GET /streams/{id}`

Returns the [`StreamInfo`](./video-streaming-poc/Streams/StreamInfo.cs) for a given stream's `id`.

### `DELETE /streams/{id}`

Finalises and terminates a stream identified by `id`. Same effect as changing `IsAlive` to null in the stream input manifest.

Returns nothing.

# Building streams

## Input manifest

Streams can be initialised by creating a source directory for images that are to be built into a video. The directory must contain a UTF-8 encoded text file
named `manifest` that contains information needed to create and control and stream lifetime.

Sample manifest file:
```txt
Name=Test Stream 1
Fps=10
IsAlive=true
```

- `Name`: Stream name. Unused at present.
- `Fps`: Frames per second value to be used for this stream. Passed through directly to ffmpeg.
- `IsAlive`: Whether the stream is still ongoing. Change this value to `false` to finalise and terminate the stream.

The manifest is reloaded periodically to determine whether the stream is still alive. It must exist for the lifetime of the stream. The `Name` and `Fps` values
are only read during initialisation and any subsequent changes are ignored.

## Source images

Once the input directory and manifest are created, the stream is ready to start building video segments as images arrive int he directory. The images  must be named in numerically
indicating their frame position in the video.

Every new file added to the directory triggers an update. Images already existing in the directory are ignored until the directory gets updated with new images. Every update the app collects all files with index less than the index of the
last processed image. If the count in this collection equals or exceeds `VAP_STREAM_SEGMENT_DURATION * Fps`, then these images are processed into a video segment and outputted. The last processed index is then set to the index of the last image
in that collection.

**tl;dr** output images named in their frame order as `0.png`, `1.png` and so on. If `VAP_STREAM_SEGMENT_DURATION = 10` and `Fps = 10`, then every 100 new images
a video segment is built and outputted.

All images must be PNG and of th same size and pixel format.

## Output playlist

All video segments are outputted into a directory named with the stream's `id` along with the `.m3u8` HLS playlist file. The playlist is finalised once the stream has
been cleanly terminated so it can be played back as a video on demand once the live stream has ended.