import React from 'react';
import videojs, {VideoJsPlayer, VideoJsPlayerOptions} from "video.js";

import 'video.js/dist/video-js.min.css';

import './VideoPlayer.scss';
import StreamInfo from "../../models/StreamInfo";

export default function VideoPlayer() {
    const videoNode = React.useRef<null | HTMLVideoElement>(null);
    const player = React.useRef<null | VideoJsPlayer>(null);
    const streamsRefreshInterval = React.useRef<null | NodeJS.Timeout>(null);
    
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
    
    const selectStream = (stream: StreamInfo) => {
        setSelectedStream(stream);
    };
    
    React.useEffect(() => {
        if (!selectedStream && player.current) {
            player.current.dispose();
            player.current = null;
            
            return;
        }
        
        if (selectedStream === null)
            return;

        const videoOptions: VideoJsPlayerOptions = {
            controls: true,
            muted: true,
            autoplay: true,
            sources: [{
                src: selectedStream.publicUri,
                type: 'application/vnd.apple.mpegurl',
            }],
        };

        player.current = videojs(videoNode.current, videoOptions);
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