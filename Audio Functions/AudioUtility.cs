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

    public static AudioMixerGroup GetMixerGroup(string groupName)
    {
        AudioMixer masterMixer = Resources.Load("Master") as AudioMixer;
        AudioMixerGroup mixerGroup = masterMixer.FindMatchingGroups(groupName)[0];
        return mixerGroup;
    }

    private static IEnumerator FadeAudioSource(AudioSource source, float fadetime, float targetVol, float curveShape, bool stopAfterFade)
    {
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, 1f - curveShape, 0, 1f - curveShape);
        keys[1] = new Keyframe(1, 1, 1f - curveShape, 0f, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);

        if (source.gameObject.GetComponent<AudioSource>())
        {
 source = source.gameObject.GetComponent<AudioSource>();
        }

        float startVol = ConvertAtoDb(source.volume);
        float currentFadeVolume = startVol;
        float currentTime = 0f;

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            currentFadeVolume = Mathf.Lerp(startVol, targetVol, animcur.Evaluate(currentTime / fadetime));
            source.volume = ConvertDbtoAmplitude(currentFadeVolume);
            yield return null;
        }

        if (stopAfterFade)
        {
            yield return new WaitForSeconds(fadetime);
            source.Stop();
        }

        yield break;
    }

}

