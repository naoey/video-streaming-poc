import React from 'react';
import videojs, {VideoJsPlayer, VideoJsPlayerOptions} from "video.js";

import 'video.js/dist/video-js.min.css';

import './VideoPlayer.scss';

export default function VideoPlayer() {
    const videoNode = React.useRef<null | HTMLVideoElement>(null);
    const player = React.useRef<null | VideoJsPlayer>(null);
    
    React.useEffect(() => {
        if (videoNode.current === null)
            return;
        
        const videoOptions: VideoJsPlayerOptions = {
            controls: true,
            sources: [{
                src: 'http://localhost:11000/output.m3u8',
                type: 'application/vnd.apple.mpegurl',
            }],
        };
        
        player.current = videojs(videoNode.current, videoOptions);
        
        return () => {
            if (player.current)
                player.current.dispose();
        };
    });
    
    return (
        <div className='player-wrapper'>
            <div data-vjs-player>
                <video
                    ref={videoNode}
                    className='video-js'
                />
            </div>
        </div>
    );
}