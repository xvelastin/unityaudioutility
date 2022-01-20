// v1.2.2 - 4 Oct 2021 //
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All-in-one script for advanced playback and volume control of Audio Source components without middleware, suitable for WebGL targets. A two-part gain stage allows for setting a fixed maximum and changing level in decibels. The Fader allows for smooth, dynamic fading for all situations. The Clip Player provides controls for looping, randomisation and modulation of multiple Audio Clips. Each instance of Audio Source Controller controls a single Audio Source which must be on the same Game Object.
/// </summary>

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class AudioSourceController : MonoBehaviour
{
    public AudioSource source;

    [Header("Gain")]
    [Range(-70, 12)] public float outputGain = 0.0f;
    [Range(-70, 12)] public float inputGain = 0.0f;
    [Range(-70, 12)] public float startingVolume = 0.0f;

    [Header("Fader")]
    [Range(-70, 12)] public float faderVolume = 0.0f;
    [Min(0)] public float fadeInOnAwakeTime = 0.0f;    
    public bool isFading = false;

    [Header("Clip Player")]
    public List<AudioClip> playlist = new List<AudioClip>();
    public bool playOnAwake = false;
    public bool loopClips = false;
    public bool newClipPerPlay = false;
    [Min(0)] public float intervalBetweenPlays = 0.0f;
    public float intervalRand = 0.0f;
    [Range(-3f, 3f)] public float pitch = 1f;
    [Range(-1f, 1f)] public float pitchRand = 0f;
    public bool isPlaying { get { return source.isPlaying; } }
    public bool looperIsLooping;

    private IEnumerator fadeCoroutine;
    private IEnumerator loopCoroutine;
    private bool hasInit = false;
    

    #region Initialisation
    private void Awake()
    {
        inputGain = 0.0f; // Disables inspector fiddling; input gain is intended for runtime use.
        source = GetComponent<AudioSource>();

        // Takes over audiosource functions and get ready for own initialisation.
        if (source.isPlaying)
        {
            source.Stop();
        }
        source.loop = false;
        source.playOnAwake = false;
        source.volume = AudioUtility.ConvertDbtoAmplitude(startingVolume);
        source.pitch = pitch;
        faderVolume = startingVolume;

        // Chooses/plays clips as set.
        if (playlist.Count == 0)
        {
            if (source.clip != null)
            {
                playlist.Add(source.clip);
            }
            else
            {
                Debug.LogError(this + "on " + gameObject.name + ": You must attach at least one AudioClip to the AudioSource or the AudioSourceController");
            }
        }

        if (newClipPerPlay)
        {
            source.clip = GetRandomClipFromList(playlist);  
        }
        else
        {
            source.clip = playlist[0];
        }
    }

    private void Start()
    {
        StartPlayback();
        hasInit = true;
    }

    private void StartPlayback()
    {
        if (playOnAwake)
        {
            if (loopClips)
            {
                PlayLoop();
            }
            else
            {
                source.pitch = pitch + Random.Range(-pitchRand, pitchRand);
                source.Play();
            }
        }

        if (fadeInOnAwakeTime > 0.0f)
        {
            FadeTo(0f, fadeInOnAwakeTime, 1.0f, false);
        }
    }
    #endregion

    #region Player/Looper
    /// <summary>
    /// Sets and plays a new clip immediately.
    /// </summary>
    /// <param name="clip">The Audio Clip to be played.</param>
    public void PlayNew(AudioClip clip)
    {
        StopLooping(0);
        source.clip = clip;
        source.Play();
    }

    /// <summary>
    /// Plays one of the clips in the Playlist list at random.
    /// </summary>
    public void PlayRandom()
    {
        StopLooping(0);
        AudioClip randomClip = GetRandomClipFromList(playlist);
        source.clip = randomClip;
        source.pitch = ModulatePitch();
        source.Play();
    }

    /// <summary>
    /// Plays one of the clips in the Playlist list at random. Modulates volume and pitch by a random range.
    /// </summary>
    /// <param name="volMin">Volume modulation minimum (in db). Offset by input gain.</param>
    /// <param name="volMax">Volume modulation minimum (in db). Offset by input gain.</param>
    /// <param name="pitchMin">Pitch modulation minimum.</param>
    /// <param name="pitchMax">Pitch modulation maximum.</param>
    public void PlayRandom(float volMin, float volMax, float pitchMin, float pitchMax)
    {
        float vol = Random.Range(volMin, volMax);
        FadeTo(vol, 0, 0.5f, false);

        pitch = Random.Range(pitchMin, pitchMax);
        source.pitch = pitch;

        StopLooping(0);

        AudioClip randomClip = GetRandomClipFromList(playlist);
        source.clip = randomClip;
        
        source.Play();
    }
    
    /// <summary>
    /// Plays the Audio Clip or Clips on a loop with the current settings.
    /// </summary>
    public void PlayLoop()
    {
        loopClips = true;
        loopCoroutine = ClipLooper(intervalBetweenPlays);
        StartCoroutine(loopCoroutine);
    }

    /// <summary>
    /// Plays the Audio Clip or Clips on a loop with a gap between plays.
    /// </summary>
    /// <param name="interval">The gap in seconds between plays of the Clip Player.</param>
    public void PlayLoop(int interval)
    {
        loopClips = true;
        loopCoroutine = ClipLooper(interval);
        StartCoroutine(loopCoroutine);
    }

    /// <summary>
    /// Stops all activity from the Clip Looper.
    /// </summary>
    /// <param name="delay">A time in seconds to wait before stopping the Audio Source, to allow time for a fade.</param>
    public void StopLooping(float delay)
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
        }

        if (delay > 0.0f)
        {
            StartCoroutine(StopSourceAfter(delay));
        }
        else
        {
            source.Stop();
        }

        looperIsLooping = false;
    }

    private IEnumerator StopSourceAfter(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        source.Stop();
        yield break;
    }

    // Loops the clips in Playlist with randomisation, modulation and interval settings.
    private IEnumerator ClipLooper(float interval)
    {
        while (true)
        {
            if (!looperIsLooping)
            {
                AudioClip newClip;

                if (!newClipPerPlay)
                {
                    // ie if we're looping the same one
                    if (interval == 0.0f)
                    {
                        // if no interval (straight loop on one clip): use audiosource native looper, which is more precise than coroutine, and stop the clip looper.
                        source.loop = true;
                        source.pitch = pitch + Random.Range(-pitchRand, pitchRand);

                        if (!source.isPlaying)
                        {
                            source.Play();
                        }

                        looperIsLooping = true;
                        yield break;
                    }
                    else
                    {
                        // if there is an interval, make sure the same clip is chosen and override looper.
                        newClip = source.clip;
                        source.loop = false;
                    }
                }
                else
                {
                    newClip = GetRandomClipFromList(playlist);
                }

                float newClipPitch = pitch + Random.Range(-pitchRand, pitchRand);
                source.pitch = newClipPitch;

                source.clip = newClip;
                source.Play();
                looperIsLooping = true;

                // Applies randomness to the interval with Interval Rand.
                float newInterval = Mathf.Clamp(interval + Random.Range(-intervalRand, intervalRand), 0, interval + intervalRand);
                newInterval += (source.clip.length / newClipPitch);
                yield return new WaitForSeconds(newInterval);

                looperIsLooping = false;
                yield return null;
            }
            else
            {
                yield break;
            }
        }
    }

    private AudioClip GetRandomAudioClipFromArray(AudioClip[] cliplist)
    {
        return cliplist[Mathf.Clamp(0, Random.Range(0, cliplist.Length), cliplist.Length)];
    }

    private AudioClip GetRandomClipFromList(List<AudioClip> cliplist)
    {
        return cliplist[Mathf.Clamp(0, Random.Range(0, cliplist.Count), cliplist.Count)];
    }
    #endregion

    #region Fader
    /// <summary>
    /// Begins a fade on the attached Audio Source with a smooth curve good for most sounds.
    /// </summary>
    /// <param name="targetVol">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
    /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
    /// <param name="stopAfterFade">If true, stops the Audio Source once the fade is finished.</param>
    public void FadeTo(float targetVol, float fadeTime, bool stopAfterFade)
    {
        // Create a two-point s-curve.
        AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        if (isFading)
        {
            StopCoroutine(fadeCoroutine);
            isFading = false;
        }

        fadeCoroutine = StartFadeInDb(fadeTime, targetVol, fadeCurve, stopAfterFade);
        StartCoroutine(fadeCoroutine);
        isFading = true;
    }
    
    /// <summary>
    /// Begins a fade on the attached Audio Source defined by a float curve shape argument.
    /// </summary>
    /// <param name="targetVol">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
    /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
    /// <param name="curveShape">Defines the bend of the fade curve, ie. the rate of change of the volume over time, from exponential (0) to S-Curve (0.5) to logarithmic (1).</param>
    /// <param name="stopAfterFade">If true, stops the Audio Source once the fade is finished.</param>
    public void FadeTo(float targetVol, float fadeTime, float curveShape, bool stopAfterFade)
    {
        curveShape = Mathf.Clamp(curveShape, 0.0f, 1.0f);

        // Creates an Animation Curve with Curve Shape to evaluate the fade over time.
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, Mathf.Sin(curveShape), 0, 1.0f - curveShape);
        keys[1] = new Keyframe(1, 1, 1 - curveShape, 0, curveShape, 0);
        AnimationCurve fadeCurve = new AnimationCurve(keys);

        if (isFading)
        {
            StopCoroutine(fadeCoroutine);
            isFading = false;
        }

        fadeCoroutine = StartFadeInDb(fadeTime, targetVol, fadeCurve, stopAfterFade);
        StartCoroutine(fadeCoroutine);
        isFading = true;
    }
    
    /// <summary>
    /// Begins a fade on the attached Audio Source defined by a custom Animation Curve.
    /// </summary>
    /// <param name="targetVol">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
    /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.></param>
    /// <param name="fadeCurve">A custom Animation Curve to be evaluated by StartFadeInDb. Values on keyframes of both axes should be constrained from 0-1.</param>
    /// <param name="stopAfterFade">If true, stops the Audio Source once the fade is finished.</param>
    public void FadeTo(float targetVol, float fadeTime, AnimationCurve fadeCurve, bool stopAfterFade)
    {
        if (isFading)
        {
            StopCoroutine(fadeCoroutine);
            isFading = false;
        }

        fadeCoroutine = StartFadeInDb(fadeTime, targetVol, fadeCurve, stopAfterFade);
        StartCoroutine(fadeCoroutine);
        isFading = true;
    }

    private IEnumerator StartFadeInDb(float fadeTime, float targetVol, AnimationCurve fadeCurve, bool stopAfterFade)
    {
        // Snaps to new volume if fade time is zero or less. 
        if (fadeTime <= 0.0f)
        {
            faderVolume = targetVol;
            UpdateAudiosourceVolume();
            isFading = false;

            if (stopAfterFade)
            {
                source.Stop();
            }
            yield break;
        }

        // Fade starts
        float startVol = faderVolume;
        float currentTime = 0f;
        while (currentTime < fadeTime)
        {
            currentTime += Time.deltaTime;
            faderVolume = Mathf.Lerp(startVol, targetVol, fadeCurve.Evaluate(currentTime / fadeTime));
            UpdateAudiosourceVolume();
            yield return null;
        }

        // Fade ends
        isFading = false;
        if (stopAfterFade)
        {
            if (looperIsLooping)
            {
                StopLooping(0);
            }
            else
            {
                source.Stop();
            }
        }
        yield break;
    }

    private void CheckAudiosource()
    {
        if (!source)
        {
            source = GetComponent<AudioSource>();
        }
    }

    private void UpdateFaderVolume()
    {
        CheckAudiosource();
        faderVolume = AudioUtility.ConvertAmplitudetoDb(source.volume);
    }

    private void UpdateAudiosourceVolume()
    {
        float currentVol = inputGain + faderVolume + outputGain;
        source.volume = AudioUtility.ConvertDbtoAmplitude(currentVol);
    }
    #endregion

    public void SetInputGain(float value)
    {
        inputGain = value;
        UpdateAudiosourceVolume();
    }

    public void SetOutputGain(float value)
    {
        outputGain = value;
        UpdateAudiosourceVolume();
    }   
    
    private void OnValidate()
    {
        if (Application.isPlaying && hasInit)
        {
            UpdateAudiosourceVolume();
        }
    } 

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
