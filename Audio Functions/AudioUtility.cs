using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioUtility
{
    /// <summary>
    /// # atodb(float amplitude), returns db value
    /// # dbtoa(float db), returns amplitude
    /// # getmixergroup(string groupName), returns first group
    /// #
    /// 
    /// created by blubberbaleen, improved by bemore//
    /// </summary>


    public static float MinSoundLevel()
    {
        return -81f;
    }




    public static float ConvertAtoDb(float amp)
    {
        amp = Mathf.Clamp(amp, ConvertDbtoA(MinSoundLevel()), 1f);
        return 20 * Mathf.Log(amp) / Mathf.Log(10);
    }

    public static float ConvertDbtoA(float db)
    {
        return Mathf.Pow(10, db / 20);
    }



    public static AudioClip RandomClipFromArray(AudioClip[] cliplist)
    {
        return cliplist[Mathf.Clamp(0, Random.Range(0, cliplist.Length), cliplist.Length)];
    }
    public static AudioClip RandomClipFromList(List<AudioClip> cliplist)
    {
        return cliplist[Mathf.Clamp(0, Random.Range(0, cliplist.Count), cliplist.Count)];
    }



    public static float ScaleValue(float value, float originalStart, float originalEnd, float newStart, float newEnd)
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
            source = source.gameObject.GetComponent<AudioSource>();

        float startVol = ConvertAtoDb(source.volume);
        float currentFadeVolume = startVol;
        float currentTime = 0f;

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            currentFadeVolume = Mathf.Lerp(startVol, targetVol, animcur.Evaluate(currentTime / fadetime));
            source.volume = ConvertDbtoA(currentFadeVolume);
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

