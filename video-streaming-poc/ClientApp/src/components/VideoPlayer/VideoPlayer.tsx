import React from 'react';

import './VideoPlayer.scss';

export default function VideoPlayer() {
    const player = React.useRef<HTMLVideoElement | null>(null);
    const livePlayer = React.useRef<HTMLVideoElement | null>(null);
    
    React.useEffect(() => {
        if (player.current !== null) {
            const p = player.current as HTMLVideoElement;
            
            p.play();
        }
        
        if (livePlayer.current !== null) {
            const p = livePlayer.current as HTMLVideoElement;
            
            p.play();
        }
    });
    
    return (
        <div className='player-wrapper'>
            {/*<h1>Video Player</h1>*/}
            {/*<video ref={player} className='player' src='/video-stream' controls />*/}
            
            <h1>Live Video</h1>
            <video ref={livePlayer} className='player' src='/create-video' controls />
        </div>
    );
}