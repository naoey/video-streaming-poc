import React from 'react';

import './VideoPlayer.scss';

export default function VideoPlayer() {
    const [isBuffering, setBuffering] = React.useState(false);
    const [videoSrc, setVideoSrc] = React.useState<string | undefined>();
    
    const livePlayer = React.useRef<HTMLVideoElement | null>(null);
    const pollInterval = React.useRef<NodeJS.Timeout | null>(null);
    
    const beginBufferPoll = async (currentTime: number) => {
        const p = livePlayer.current as HTMLVideoElement;
        
        if (p.duration - p.currentTime > 5)
            return;
        
        console.debug('Beginning buffering...');
        
        if (pollInterval.current) {
            clearTimeout(pollInterval.current);
            pollInterval.current = null;
        }
        
        setBuffering(true);
        
        const response = await fetch('/poll');
        const json = await response.json();
        
        const hasData = json.length - p.duration > 5 || (isNaN(p.duration) && json.length > 5);
        
        console.log(
            'Determining ready state for stream.',
            'Polled length',
            json.length,
            'Current duration',
            p.duration,
            'Result',
            hasData,
        );
        
        if (hasData) {
            if (isNaN(p.duration)) {
                console.debug('Stream is not yet initialised, setting video src.', 'isBuffering', isBuffering)
                // we are loading for the first time, set the element's src
                setVideoSrc('/stream');
                
                await p.load();
                await p.play();
                
                console.debug('Playback began');
            } else {
                console.debug('Stream is already running. Reloading video...');
                
                // reload the video element to update
                await p.load();
                await p.play();
                
                console.debug('Reload complete!')
            }
            
            setBuffering(false);
            console.info('Length update complete!');
        } else {
            pollInterval.current = setTimeout(() => beginBufferPoll(currentTime), 2000);
        }
    };
    
    const initialiseStream = async () => {
        await fetch('/stream', {
            method: 'POST',
        });
        
        beginBufferPoll(0);
    };
    
    React.useEffect(() => {
        if (livePlayer.current !== null) {
            const p = livePlayer.current as HTMLVideoElement;
            
            initialiseStream();
            
            p.addEventListener('timeupdate', function () {
                const needToBuffer = this.duration - this.currentTime <= 5;

                console.debug(
                    'Playback position updated, determining need to re-buffer.',
                    'Local duration',
                    this.duration,
                    'Playback time',
                    this.currentTime,
                    'Result',
                    needToBuffer,
                );

                if (needToBuffer && pollInterval.current === null) {
                    p.pause();
                    beginBufferPoll(this.currentTime);
                }
            });
            
            p.addEventListener('durationchange', function () {
                console.info('Duration changed to', this.duration);
            });
            
            return () => {
                if (pollInterval.current)
                    clearTimeout(pollInterval.current);
            }
        }
    });
    
    return (
        <div className='player-wrapper'>
            <h1>Live Video</h1>
            <video ref={livePlayer} className='player' src={videoSrc} controls />
            
            {
                isBuffering
                    ? <h3>Buffering...</h3>
                    : null
            }
        </div>
    );
}