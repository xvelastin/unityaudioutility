// By blubberbaleen. Find more at https://github.com/xvelastin/unityaudioutility //
// Unique version created for Pangea, Limbik Theatre, 2021 //

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All-in-one audiosource controller. Note: This version is only meant to be instantiated as part of a prefab.
/// The audiosourcecontroller takes over ALL functions from the audiosource component, including clip, to make it simpler.
/// 
/// 
/// - Play(): plays the audioclip with the given settings.
/// - FadeTo(): fades to a given volume (in decibels) over time. 
/// - Pause(): pauses the clip, which lets it be resumed later.
/// - Stop(): stops the clip, which means it can be unloaded from memory.
/// 
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]


public class Pangea_AudioSourceController : MonoBehaviour
{
    [Header("Player")]
    // Controls playback of clips.
    [Tooltip("Drag a clip here for the AudioSourceController to function.")]
     AudioSource source;
    public AudioClip clip;

    [Tooltip("Loops the clip indefinitely.")]
    public bool loop = false;

    public bool playOnAwake = true;

    [Header("Volume Levels")] 
    // Two gain stages:
    // - overallVolume represents an overall static trim value, and shouldn't be touched.
    // - startingVolume is only referred to at initialisation.
    [Tooltip("Use this to apply an offset to the overall sound level of this audio source. \n\nNote: the final real value will be capped at 1.0 amplitude because of the way Unity's AudioSource works, so a positive level is best used to offset negative values from the fade or gain level.")]
    [Range(-81, 24)] [SerializeField] float overallVolume = 0f;

    [Tooltip("Use this to set the volume before any fade is triggered without changing the overall trim of the clip. For example, set to minimum for anything you want to fade in from silence.")]
    [Range(-81, 24)] public float startingVolume;

    // values for fader    
     float fadeVolume;
     bool isFading;

    #region Initialisation
    // Initialisation is done in Awake(), to avoid clashing with Start() stuff in other scripts.

    private void Awake()
    {
        if (!source)
            source = GetComponent<AudioSource>();

        if (!clip)
            Debug.LogError("You need to assign a clip to " + gameObject.name + "'s AudioSourceController");

        // Takes over all audiosource functions and initialises to default.
        if (source.isPlaying)
            source.Stop();
        source.loop = loop;
        source.playOnAwake = playOnAwake;
        source.volume = AudioUtility.ConvertDbtoA(startingVolume);
        source.pitch = 1.0f;
        source.panStereo = 0.0f;
        source.spatialBlend = 0.0f;
        source.clip = clip;

        Play();
        fadeVolume = AudioUtility.ConvertAtoDb(source.volume);
        FadeTo(startingVolume, 0.0f, 0.0f);

    }

    #endregion

    private void GetAudioSourceVolume()
    {
        if (!source)
            source = GetComponent<AudioSource>();

        fadeVolume = AudioUtility.ConvertAtoDb(source.volume);

    }

    #region Fader
    /// <summary>
    /// Begins a fade on the attached Audiosource component.
    /// </summary>
    /// <param name="targetVol">The target volume (in dB)</param>
    /// <param name="fadetime">The time it takes for the fade to go from the current volume to the target volume.</param>
    /// <param name="curveShape">Applies a bend to the curve from linear (0) to exponential (1). Linear curves are good for most individual bits of audio, S-Curves (0.5) are good for music and larger textures, exponentials are good for crossfades.</param>
    public void FadeTo(float targetVol, float fadetime, float curveShape)
    {
        curveShape = Mathf.Clamp(curveShape, 0.0f, 1.0f);

        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, 1f - curveShape, 0, 1f - curveShape);
        keys[1] = new Keyframe(1, 1, 1f - curveShape, 0f, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);

        if (isFading)
        {
            StopCoroutine(StartFadeInDb(fadetime, targetVol, animcur));
            isFading = false;
        }
        StartCoroutine(StartFadeInDb(fadetime, targetVol, animcur));
        isFading = true;
    }

    private IEnumerator StartFadeInDb(float fadetime, float targetVol, AnimationCurve animcur)
    {
        GetAudioSourceVolume();
        float currentTime = 0f;
        float startVol = fadeVolume;

        Debug.Log("AudioSourceController on " + gameObject.name + ": Fading from " 
            + startVol + " to " + targetVol + " over " + fadetime);

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            fadeVolume = Mathf.Lerp(startVol, targetVol, animcur.Evaluate(currentTime / fadetime));

            UpdateParams();
            yield return null;
        }

        isFading = false;
        yield break;
    }

    private float GetGain()
    {
        return fadeVolume + overallVolume;
    }

    private void UpdateParams()
    {
        float currentVol = GetGain();
        source.volume = AudioUtility.ConvertDbtoA(currentVol);
    }


    #endregion

    #region Player
    // Pretty unnecessary really, but neater to leave all of the functions here.
    public void Play()
    {
        Debug.Log("AudioSourceController on " + gameObject.name + ": Play triggered.");
        source.Play();
    }

    public void Pause()
    {
        Debug.Log("AudioSourceController on " + gameObject.name + ": Pause triggered.");
        source.Pause();
    }

    public void Stop()
    {
        Debug.Log("AudioSourceController on " + gameObject.name + ": Stop triggered.");
        source.Stop();
    }



    #endregion

    private void OnDisable()
    {
        StopAllCoroutines();
    }

}
