using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioUtility
{
    /// <summary>
    /// # atodb(float amplitude), returns db value
    /// # dbtoa(float db), returns amplitude
    /// 
    /// created by blubberbaleen, improved by bemore//
    /// </summary>

    private const float referenceAmplitude = 0.00001f;
    public static float ConvertAtoDb(float amp)
    {
        amp = Mathf.Clamp(amp, ConvertDbtoA(-70f), 1f);
        return 20 * Mathf.Log(amp) / Mathf.Log(10);
    }

    public static float ConvertDbtoA(float db)
    {
        return Mathf.Pow(10, db / 20);
    }


    public static AudioMixerGroup GetMixerGroup(string groupName)
    {
        AudioMixer masterMixer = Resources.Load("Master") as AudioMixer;
        AudioMixerGroup mixerGroup = masterMixer.FindMatchingGroups(groupName)[0];
        return mixerGroup;
    }

}

