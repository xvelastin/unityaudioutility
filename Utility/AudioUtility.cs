using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Contains often-used values and functions.
/// </summary>
public static class AudioUtility
{
    /// <summary>
    /// Returns the universal minimum volume value in decibels.
    /// </summary>
    public static float MINIMUM_VOLUME = -60.0f;
    
    /// <summary>
    /// Returns the universal maximum volume value in decibels.
    /// </summary>
    public static float MAXIMUM_VOLUME = 0.0f;

    /// <summary>
    /// Converts amplitude (0-1) as used in native Unity to the logarithmic decibel scale.
    /// </summary>
    /// <param name="amplitude"></param>
    /// <returns></returns>
    public static float ToDecibels(float amplitude)
    {
        amplitude = Mathf.Clamp(amplitude, ToAmplitude(MINIMUM_VOLUME), 1f);
        return Mathf.Clamp(20 * Mathf.Log(amplitude) / Mathf.Log(10), MINIMUM_VOLUME, MAXIMUM_VOLUME);
    }

    /// <summary>
    /// Converts decibels to amplitude (0-1) for use in Unity's Audio Source component.
    /// </summary>
    /// <param name="decibels"></param>
    /// <returns></returns>
    public static float ToAmplitude(float decibels)
    {
        return decibels > MINIMUM_VOLUME ? Mathf.Pow(10, decibels / 20) : 0;
    }
    
    /// <summary>
    /// Converts playback speed to semitones.
    /// </summary>
    /// <param name="speed"></param>
    /// <returns></returns>
    public static float ToSemitones(float speed) => 12 * Mathf.Log(speed, 2);
    
    /// <summary>
    /// Converts semitones to playback speed ie. the pitch value in Unity's Audio Source component.
    /// </summary>
    /// <param name="semitones"></param>
    /// <returns></returns>
    public static float ToPitch(float semitones) => Mathf.Pow(2, semitones / 12);

    /// <summary>
    /// Interpolates between the volume of an Audio Source and a target volume over a given duration with a configurable fade curve. Remember to cancel any ongoing fades when triggering.
    /// </summary>
    /// <param name="source">The Audio Source to be faded.</param>
    /// <param name="targetVolume">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
    /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>   
    /// <param name="curveShape">Defines the bend of the fade curve, ie. the rate of change of the volume over time, from exponential (0) to s-curve (0.5) to logarithmic (1).</param>
    /// <param name="stopAfterFade">If true, stops the Audio Source at the end of the fade.</param>
    private static IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float fadeTime, float curveShape = 0.5f, bool stopAfterFade = false)
    {
        curveShape = Mathf.Clamp(curveShape, 0.0f, 1.0f);
        if (fadeTime <= 0.0f)
        {
            source.volume = targetVolume;
            yield break;
        }

        var fadeCurve = DrawFadeCurve(curveShape);

        float startingVolume = ToDecibels(source.volume);
        float currentFadeVolume = startingVolume;
        float currentTime = 0f;
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            currentFadeVolume = Mathf.Lerp(startingVolume, targetVolume, fadeCurve.Evaluate(currentTime / fadeTime));
            source.volume = ToAmplitude(currentFadeVolume);
            yield return null;
        }

        if (stopAfterFade)
        {
            yield return new WaitForSeconds(fadeTime);
            source.Stop();
        }

        yield break;
    }
    
    /// <summary>
    /// Returns a curve which can be used to evaluate a fade over time.
    /// </summary>
    /// <param name="curveShape">A value between 0-1 which defines the curvature of the fade. 0 = starts slow, gets faster: good for natural fades. 1 = starts fast, gets slower: good for transitions. 0.5 = fast in the middle, slow at the start and end (an s-curve): good for most situations.</param>
    /// <returns></returns>
    public static AnimationCurve DrawFadeCurve(float curveShape)
    {
        curveShape = Mathf.Clamp01(curveShape);
        var keys = new Keyframe[2];
        const float curveBendFactor = 3.0f;
            
        curveShape = 1 - curveShape;
        if (curveShape < 0.5f)
        {
            keys[0] = new Keyframe(0, 0, 0, Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor);
            keys[1] = new Keyframe(1, 1);
        }
        else
        {
            keys[0] = new Keyframe(0, 0);
            keys[1] = new Keyframe(1, 1, -Mathf.Cos(curveShape * Mathf.PI) * curveBendFactor, 0);
        }
            
        return new AnimationCurve(keys);
    }
}