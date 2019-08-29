package com.microsoft.bot.builder.solutions.virtualassistant.activities.main;

import android.content.Context;
import android.media.AudioAttributes;
import android.media.SoundPool;
import android.util.Log;

import com.microsoft.bot.builder.solutions.virtualassistant.R;

/**
 * Sound-effect Manager
 *
 * How it works:
 *  1. sound is loaded
 *  2. onLoadComplete triggers playing the sound
 */
public class SfxManager implements SoundPool.OnLoadCompleteListener{

    // CONSTANTS
    private final String LOGTAG = getClass().getSimpleName();
    private final static int MAX_SIMULTANEOUS_SFX = 5;
    private final static float PLAY_RATE = 1.0f;
    private final static float VOLUME = 0.5f;

    // STATE
    private SoundPool mSoundPool;
    private Context context;

    public SfxManager initialize(Context context){
        this.context = context;
        initSoundPool();
        return this;
    }

    private void initSoundPool(){
        AudioAttributes audioAttributes = new AudioAttributes.Builder()
                .setContentType(AudioAttributes.CONTENT_TYPE_MUSIC)
                .setUsage(AudioAttributes.USAGE_GAME)
                .build();
        mSoundPool = new SoundPool.Builder()
                .setMaxStreams(MAX_SIMULTANEOUS_SFX)
                .setAudioAttributes(audioAttributes)
                .build();
        mSoundPool.setOnLoadCompleteListener(this);
    }

    @Override
    public void onLoadComplete(SoundPool soundPool, int sampleId, int status) {
        if (status == 0) {
            Log.d(LOGTAG, "playing Sound effect sampleID: " + sampleId);
            int streamID = soundPool.play(sampleId, VOLUME, VOLUME, 1, 0, PLAY_RATE);
        } else {
            Log.d(LOGTAG, "loading failed, status: " + status);
        }
    }

    /**
     * Unload all SFX
     * Note: after this call, the Soundpool must be re-init
     */
    public void reset(){
        mSoundPool.release();
        mSoundPool = null;
    }

    /**
     * Play a sound in its own MediaPlayer on its own thread
     * @param resRawId
     * @return the sound id
     */
    private int playSfx(int resRawId) {
        final int soundId = mSoundPool.load(context, resRawId, 1);
        return soundId;
    }

    public void playEarconDisambigError() {
        playSfx(R.raw.earcon_disambig_error);
    }

    public void playEarconDoneListening() {
        playSfx(R.raw.earcon_done_listening);
    }

    public void playEarconListening() {
        playSfx(R.raw.earcon_listening);
    }

    public void playEarconProcessing() {
        playSfx(R.raw.earcon_processing);
    }

    public void playEarconResults() {
        playSfx(R.raw.earcon_results);
    }
}
