import React from 'react';
import videojs, {VideoJsPlayer} from "video.js";
import StreamInfo from "../../models/StreamInfo";

import 'video.js/dist/video-js.min.css';

import './VideoPlayer.scss';

export default function VideoPlayer() {
    const videoNode = React.useRef<null | HTMLVideoElement>(null);
    const player = React.useRef<null | VideoJsPlayer>(null);
    const streamsRefreshInterval = React.useRef<null | NodeJS.Timeout>(null);
    const streamLoadRetryTimeout = React.useRef<null | NodeJS.Timeout>(null);

    const [selectedStream, setSelectedStream] = React.useState<null | StreamInfo>(null);
    const [streamsList, setStreamsList] = React.useState<StreamInfo[]>([]);

    const loadStreamsList = async () => {
        try {
            const response = await fetch('/streams');
            const json = await response.json();

            setStreamsList(json);
        } catch (e) {
            alert(e.message);
            console.error(e);
        }
    };

    const loadStream = async (stream: StreamInfo) => {
        if (player.current && player.current.src().length > 0) {
            await player.current.pause();
            player.current.src([]);
        }

        try {
            const response = await fetch(stream.publicUri);

            if (!player.current)
                player.current = videojs(videoNode.current, {
                    controls: true,
                    sources: [],
                });

            if (response.status === 200) {
                // Stream is present and can be added as a source
                player.current.src(stream.publicUri);
                await player.current.play();
            } else {
                // Stream failed to load for whatever reason, set a delay to retry once more
                streamLoadRetryTimeout.current = setTimeout(() => loadStream(stream), 2500);
            }
        } catch (e) {
            console.error(e);
        }
    }

    const selectStream = (stream: StreamInfo) => {
        setSelectedStream(stream);
    };

    React.useEffect(() => {
        if (!selectedStream)
            return;

        loadStream(selectedStream);
    }, [selectedStream]);

    React.useEffect(() => {
        loadStreamsList();

        setInterval(loadStreamsList, 3000);

        return () => {
            if (player.current)
                player.current.dispose();

            if (streamsRefreshInterval.current)
                clearInterval(streamsRefreshInterval.current);
        };
    }, []);

    return (
        <div className='player-wrapper'>
            <ul className='streams-list'>
                {
                    streamsList.map(s => (
                        <li key={s.id}>
                            <button onClick={() => selectStream(s)}>
                                {s.name}
                            </button>
                        </li>
                    ))
                }
            </ul>

            <div className='current-stream-info'>
                <h3>{selectedStream ? selectedStream.name : null}</h3>
            </div>

            <div data-vjs-player>
                <video
                    ref={videoNode}
                    className='video-js'
                />
            </div>
        </div>
    );
}
