// By blubberbaleen. Find more at https://github.com/xvelastin/unityaudioutility //
// v1.0 //
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All-in-one audiosource controller in decibels. 0db = 1.0 Volume, negative floor is around -80db. Public methods:
/// - SetInputGain(): sets volume going into the component.
/// - SetOutputGain(): sets volume coming out of the component (best practice is to just use input gain and to leave output gain to set the overall volume of the source - especially useful if you aren't using AudioMixerGroups).
/// - FadeTo(): fades to a given volume (in decibels) over time. A fade curve argument applies a bend to the curve from linear (0) to exponential (1). Linear curves are good for most individual bits of audio, S-Curves (0.5) are good for music and larger textures, exponentials are good for crossfades.
/// - PlayLoopWithInterval(): loops the clips in the clip list with an optional wait time (interval). Interval and pitch randomisation available per component in the inspector.
/// 
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class AudioSourceController : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;

    [Header("Gain")] // Processing stage for audio.

    [Tooltip("Sets an offset to the sound level coming out of the AudioSourceController. Note: because of the way Unity's AudioSource works, the real level will be capped at the maximum amplitude of the sound file.")] 
    [SerializeField] [Range(-81, 24)] float outputGain = 0f;

    [Tooltip("The current gain offset being applied from other scripts by the public SetGain() method. Read-only.")]
    [SerializeField] [Range(-81, 24)] float inputGain;

    [Header("Fader")]
    // Controls fades.
    [Tooltip("Sets an initial fade-in time. A value of zero will snap to the output level.")]
    [Min(0)] [SerializeField] float FadeInOnAwakeTime = 0f;
   
    float fadeVolume;
    bool isFading;

    [Header("Clip Player")] // Choosing and playing back clips with variations.
    [Tooltip("Load at least one clip here for the AudioSourceController to function.")]
    [SerializeField] List<AudioClip> playlist = new List<AudioClip>();
    [Tooltip("The same as the native AudioSource Play On Awake function.")]
    [SerializeField] bool playOnAwake = false;
    [Tooltip("Loops the clip.")]
    [SerializeField] bool loop = false;
    [Tooltip("If there's more than one clip in the list, randomly chooses a new one on each loop.")]
    [SerializeField] bool newClipPerPlay = false;
    [Tooltip("Sets a wait time between clip plays.")]
    [SerializeField] [Min(0)] float intervalBetweenPlays = 0f;
    [Tooltip("Sets a randomisation offset (+/-) to the interval, which can prevent certain sounds from becoming too mechanical.")]
    [SerializeField] float intervalRand = 0f;

    [Tooltip("Sets a clip offset for all clips in the list.")]
    [SerializeField] [Range(-4f, 4f)] float pitch = 1f;
    [Tooltip("Sets a randomisation offset (+/-) to the pitch value above.")]
    [SerializeField] [Range(-1f, 1f)] float pitchRand = 0f;
    bool playerIsPlaying;
    /*
        [Header("3D Optimisation")]
        [Tooltip("If your audio listener isn't at the main camera, set it here.")]
        [SerializeField] AudioListener audioListener;
        [Tooltip("Especially for lightweight (eg. WebGL) builds, stop this audiosource if it falls outside of the source's max distance to cut down on voice usage.")]
        [SerializeField] bool stopSourceIfFar;
        bool sourceHasBeenStopped;
        float fadeVolumeWhenStopped;
        bool wasLoopingWhenStopped;
    */
    public void SetInputGain(float value)
    {
        inputGain = value;
        UpdateParams();

    }
    public void SetOutputGain(float value)
    {
        outputGain = value;
        UpdateParams();
    }

    #region Initialisation


    private void Start()
    {
        // disables inspector fiddling
        inputGain = 0;

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
        
        // Takes over audiosource functions.
        if (audioSource.isPlaying) 
            audioSource.Stop();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.0f;
        fadeVolume = AudioUtility.ConvertAtoDb(audioSource.volume);

        // Chooses/plays clips as set.
        if (playlist.Count == 0)
        { 
            if (audioSource.clip != null)
            {
                playlist.Add(audioSource.clip);
            }
            else
            {
                Debug.LogError(this + "on " + gameObject.name + ": You must attach at least one AudioClip to the AudioSource or the AudioSourceController");
            }        
        }


        if (newClipPerPlay)
            audioSource.clip = AudioUtility.RandomClipFromList(playlist);
        else
            audioSource.clip = playlist[0];

        if (playOnAwake)
        {
            audioSource.pitch = pitch + Random.Range(-pitchRand, pitchRand);
            audioSource.Play();
        }

        if (loop)
        {
            PlayLoopWithInterval();
        }
        
        if (FadeInOnAwakeTime > 0.0f)
        {
            FadeTo(outputGain + inputGain, FadeInOnAwakeTime, 1.0f, false);
        }

      /*  if (!audioListener)
            audioListener = Camera.main.gameObject.GetComponent<AudioListener>();*/

    }

    #endregion

    private void GetAudioSourceVolume()
    {
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        fadeVolume = AudioUtility.ConvertAtoDb(audioSource.volume);
        Debug.Log("fadeVolume = " + fadeVolume);
    }
   
    #region Player/Looper

    public void PlayLoopWithInterval()
    {
        loop = true;
        StartCoroutine(ClipLooper(intervalBetweenPlays));
    }
    public void PlayLoopWithInterval(float interval)
    {
        loop = true;
        StartCoroutine(ClipLooper(interval));
    }
    IEnumerator ClipLooper (float interval)
    {
        while (true)
        {
            if (!playerIsPlaying)
            {
                AudioClip newClip;
                if (newClipPerPlay)
                    newClip = AudioUtility.RandomClipFromList(playlist);
                else newClip = audioSource.clip;

                float newClipPitch = pitch + Random.Range(-pitchRand, pitchRand);
                audioSource.pitch = newClipPitch;

                audioSource.clip = newClip;
                audioSource.Play();
                playerIsPlaying = true;

                float newInterval = Mathf.Clamp(interval + Random.Range(-intervalRand, intervalRand), 0, interval + intervalRand);
                newInterval += (audioSource.clip.length / newClipPitch);
                yield return new WaitForSeconds(newInterval);                
                
                playerIsPlaying = false;
                
                yield return null;
            }
            yield return null;
        }
    }

    #endregion

    #region Fader

    public void FadeTo(float targetVol, float fadetime, float curveShape, bool stopAfterFade)
    {
        // Uses an AnimationCurve
        // curveShape 0.0 = linear; curveShape 0.5 = s-curve; curveshape 1.0 (exponential).
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, 1f - curveShape, 0, 1f - curveShape);
        keys[1] = new Keyframe(1, 1, 1f - curveShape, 0f, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);

        if (isFading)
        {
            StopCoroutine(StartFadeInDb(fadetime, targetVol, animcur, stopAfterFade));
            isFading = false;
        }
        StartCoroutine(StartFadeInDb(fadetime, targetVol, animcur, stopAfterFade));
        isFading = true;
    }


    private IEnumerator StartFadeInDb(float fadetime, float targetVol, AnimationCurve animcur, bool stopAfterFade)
    {
        GetAudioSourceVolume();
        float currentTime = 0f;
        float startVol = fadeVolume;

        Debug.Log("Fading to " + targetVol + " from " + startVol + " over " + fadetime);

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            fadeVolume = Mathf.Lerp(startVol, targetVol, animcur.Evaluate(currentTime / fadetime));

            UpdateParams();
            yield return null;
        }

        isFading = false;

        if (stopAfterFade)
        {
            yield return new WaitForSeconds(fadetime);
            audioSource.Stop();
        }

        yield break;
    }

    private float GetGain()
    {
        return inputGain + fadeVolume + outputGain;
    }

    private void UpdateParams()
    {
        float currentVol = GetGain();
        audioSource.volume = AudioUtility.ConvertDbtoA(currentVol);
    }


    #endregion
   /* #region Optimisation Station
    private void Update()
    {
        if (stopSourceIfFar) StopSourceIfFar();
    }

    void StopSourceIfFar()
    {
        if (!sourceHasBeenStopped && 
            Vector3.Distance(transform.position, audioListener.gameObject.transform.position) > source.maxDistance)
        {
            fadeVolumeWhenStopped = fadeVolume;
            FadeTo(-81, 1f, 1f, true);

            wasLoopingWhenStopped = loop;
            loop = false;

            sourceHasBeenStopped = true;
            Debug.Log(this.gameObject.name + " out of its audiosource's max distance. Stopping to conserve voices.");
        }
        else
        {
            loop = wasLoopingWhenStopped;
            FadeTo(fadeVolumeWhenStopped, 1f, 1f, false);

            source.Play();
            sourceHasBeenStopped = false;
        }
    }
    #endregion*/






    private void OnDisable()
    {
        StopAllCoroutines();
    }

}
