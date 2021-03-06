using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region description
/// <summary> 
/// Contains coroutines that interpolate audio volumes logarithmically using decibels (db) rather than Unity's native amplitude. The resulting fades are much easier on the ear.
/// </summary>
/// 
// # How To Use:
/// FadeAudioInDb.Fade takes 3 or 4 constructors: audiosource, fade time, target volume, and curve shape. The latter is a float value from 0-1 specifying the curvature of the fade curve. A value of 0 will give a constant fade, a value of 0.5 gives an s-curve, and 1 gives a weighted curve. This value affects the speed of the volume change. There's no 'right' curve, as it depends on the sound and its context.
/// 
// # Changelog :
/// FadeAudio(Db) by blubberbaleen
/// 0.1 | 24 Jan 21 | Script created.
/// 0.2 | 6 Mar 21 | Added curveShape argument for Curve function.
/// 
// # Feedback to xavier@xaviervelastin.com.
#endregion

public static class FadeAudioInDb
{  
    public static IEnumerator Fade(AudioSource audioSource, float fadeTime, float targetVolume)
    {     
        // No curveShape argument denotes a constant fade in decibels from the current audiosource volume to the targetvolume level.
        float currentTime = 0f;
        float startDb = ConvertAtoDb(audioSource.volume);
        float targetDb = ConvertAtoDb(targetVolume);


        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = ConvertDbtoA(Mathf.Lerp(startDb, targetDb, currentTime / fadeTime));            
            yield return null;           
        }
        yield break;
    }

    public static IEnumerator Fade(AudioSource audioSource, float fadeTime, float targetVolume, float curveShape)
    {
        float startDb = ConvertAtoDb(audioSource.volume);
        float targetDb = ConvertAtoDb(targetVolume);

        // curveShape handler: creates a new Animation Curve with values from curveShape. At curveShape = 0.0, it's flat (linear: good for most audio); at 0.5, it's an S-Curve (starts and ends slowly, good for fade-outs); at 1.0, it's exponential (starts quickly then slows down, good for crossfades).    
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, 1f - curveShape, 0, 1f - curveShape);
        keys[1] = new Keyframe(1, 1, 1f - curveShape, 0f, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);

        float currentTime = 0f;
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = ConvertDbtoA(Mathf.Lerp(startDb, targetDb, animcur.Evaluate(currentTime / fadeTime)));
            yield return null;
        }
        yield break;
    }

    private static float ConvertAtoDb(float amp)
    {
        amp = Mathf.Clamp(amp, ConvertDbtoA(-70f), 1f);
        float db = 20 * Mathf.Log(amp) / Mathf.Log(10);
        return db;
    }

    private static float ConvertDbtoA(float db)
    {
        float amp = Mathf.Pow(10, db / 20);
        return amp;
    }

}
