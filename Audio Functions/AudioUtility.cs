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
    /// Returns the universal minimum db value.
    /// </summary>
    public static float minimum = -70.0f;

    /// <summary>
    /// Converts amplitude (0-1) as used in native Unity to the logarithmic decibel scale.
    /// </summary>
    /// <param name="amplitude"></param>
    /// <returns></returns>
    public static float ConvertAmplitudetoDb(float amplitude)
    {
        amplitude = Mathf.Clamp(amplitude, ConvertDbtoAmplitude(minimum), 1f);
        return 20 * Mathf.Log(amplitude) / Mathf.Log(10);
    }

    /// <summary>
    /// Converts decibels to amplitude (0-1) for use in Unity's Audio Source component.
    /// </summary>
    /// <param name="decibels"></param>
    /// <returns></returns>
    public static float ConvertDbtoAmplitude(float decibels)
    {
        return Mathf.Pow(10, decibels / 20);
    }

    public static float MapToRange(float value, float originalStart, float originalEnd, float newStart, float newEnd)
    {
        // credit to Wim Coenen, https://stackoverflow.com/questions/4229662/convert-numbers-within-a-range-to-numbers-within-another-range //
        double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
        return (float)(newStart + ((value - originalStart) * scale));
    }

    /// <summary>
    /// Gets the Audio Mixer Group of a given string name. Note this relies on the main mixer being called "Master".
    /// </summary>
    /// <param name="groupName">The name of the Audio Mixer Group to be returned.</param>
    /// <returns>The Audio Mixer Group.</returns>
    public static AudioMixerGroup GetMixerGroup(string groupName)
    {
        AudioMixer masterMixer = Resources.Load("Master") as AudioMixer;
        AudioMixerGroup mixerGroup = masterMixer.FindMatchingGroups(groupName)[0];
        return mixerGroup;
    }

    /// <summary>
    /// Interpolates between the volume of an Audio Source and a target volume over a given duration with a configurable fade curve. It is almost identical to the full implementation in the Audio Source Controller script - however, unlike that script, it will not cancel a previous fade, so overlaps will happen unless handled correctly.
    /// </summary>
    /// <param name="source">The Audio Source to be faded.</param>
    /// <param name="targetVolume">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
    /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>   
    /// <param name="curveShape">Defines the bend of the fade curve, ie. the rate of change of the volume over time, from exponential (0) to s-curve (0.5) to logarithmic (1).</param>
    /// <param name="stopAfterFade">If true, stops the Audio Source at the end of the fade.</param>
    private static IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float fadeTime, float curveShape, bool stopAfterFade)
    {
        curveShape = Mathf.Clamp(curveShape, 0.0f, 1.0f);
        if (fadeTime <= 0.0f)
        {
            source.volume = targetVolume;
            yield break;
        }

        // Creates an Animation Curve with Curve Shape to evaluate the fade over time.
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, Mathf.Sin(curveShape), 0, 1.0f - curveShape);
        keys[1] = new Keyframe(1, 1, 1 - curveShape, 0, curveShape, 0);
        AnimationCurve fadeCurve = new AnimationCurve(keys);

        float startingVolume = ConvertAmplitudetoDb(source.volume);
        float currentFadeVolume = startingVolume;
        float currentTime = 0f;
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            currentFadeVolume = Mathf.Lerp(startingVolume, targetVolume, fadeCurve.Evaluate(currentTime / fadeTime));
            source.volume = ConvertDbtoAmplitude(currentFadeVolume);
            yield return null;
        }

        if (stopAfterFade)
        {
            yield return new WaitForSeconds(fadeTime);
            source.Stop();
        }

        yield break;
    }
}