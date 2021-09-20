// By blubberbaleen. Find more at https://github.com/xvelastin/unityaudioutility //
// v1.21 - 20 Sept 2021 //
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All-in-one audiosource controller for advanced control even with WebGL targets. Intended to be called by an Audio Manager or Events script. /// 
/// </summary>
/// Public methods:
/// - SetInputGain: sets volume going into the component.
/// - SetOutputGain: sets volume coming out of the component (best practice is to just use input gain and to leave output gain to set the overall volume of the source - especially useful if you aren't using AudioMixerGroups).
/// - FadeTo: fades to a given volume (in decibels) over time. A fade curve argument applies a bend to the curve from exponential (0) to logarithmic (1). Exponential curves are good for realism, S-Curves (0.5) are good for music and larger textures, logarithmic curves are good for crossfades.
/// - PlayNew: replaces current clip with a new clip and plays.
/// - PlayRandom: plays one of the clips in the Playlist list at random, with arguments for volume and pitch modulation.
/// - PlayLoop: loops clips, selecting a new clip at random after clip end, after an interval in seconds, either using the one set in the script or as an argument.
/// - StopLooping: stops the audio after a given wait time (a fade out would need to be called separately).
/// 
/// 0db = 1.0 Amplitude, negative floor is around -80db (see AudioUtility.cs). 
/// 
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class AudioSourceController : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Gain")] // Processing stage for audio.
    [Range(-81, 24)] public float outputGain = 0f;
    [Range(-81, 24)] public float inputGain;
    [Range(-81, 24)] public float startingVolume;

    [Header("Fader")]// Controls fades.
    [Min(0)] [SerializeField] float FadeInOnAwakeTime = 0f;
    [Range(-81, 24), SerializeField]  float faderVolume;
    public bool isFading;
    private IEnumerator fadeCoroutine;
    [HideInInspector] public float currentFadeTarget;
    [HideInInspector] public float currentFadeTime;


    [Header("Clip Player")] // Choosing and playing back clips with variations.
    [SerializeField] List<AudioClip> playlist = new List<AudioClip>();
    public bool playOnAwake = false;
    public bool loopClips = false;
    public bool newClipPerPlay = false;
    [Min(0)] public float intervalBetweenPlays = 0f;
    public float intervalRand = 0f;
    private IEnumerator loopCoroutine;

    [Range(-4f, 4f)] public float pitch = 1f;
    [Range(-1f, 1f)] public float pitchRand = 0f;
    public bool looperIsLooping;

    [Header("Audibility Check")]
    public bool enableAudibilityCheck = false;


    bool hasInit = false;

    /*    DEBUGGING   */
    private void OnValidate()
    {
        if (Application.isPlaying && hasInit)
        {
            UpdateParams();
        }
    }



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
    private void Awake()
    {
        // disables inspector fiddling
        inputGain = 0;
        audioSource = GetComponent<AudioSource>();

        // Takes over audiosource functions.
        if (audioSource.isPlaying)
            audioSource.Stop();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = AudioUtility.ConvertDbtoA(startingVolume);
        audioSource.pitch = pitch;
        faderVolume = startingVolume;

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


    }

    private void Start()
    {
        Initialise();
        
    }

    private void Initialise()
    {
        
        if (enableAudibilityCheck)
            StartCoroutine(CheckAudibility(1));
        else
            StartPlayback();

        hasInit = true;

    }

    IEnumerator CheckAudibility(float checkInterval)
    {
        do
        {
            yield return new WaitForSeconds(checkInterval);
            if (CheckIfAudible())
            {
                StartPlayback();
                yield break;
            }
        }
        while (true);
    }

    private void StartPlayback()
    {

        if (playOnAwake)
        {
            audioSource.pitch = pitch + Random.Range(-pitchRand, pitchRand);
            audioSource.Play();
        }

        if (loopClips)
        {
            PlayLoop();
        }


        if (FadeInOnAwakeTime > 0.0f)
        {
            FadeTo(0f, FadeInOnAwakeTime, 1.0f, false);
        }
            
        
        

    }



    #endregion

    public bool CheckIfAudible()
    {
        var listener = AudioManager.Instance.listener;
        float distance = Vector3.Distance(transform.position, listener.transform.position);
        if (distance > audioSource.maxDistance) return false;
        else return true;

    }



    #region Player/Looper

    /// <summary>
    /// Plays a new clip.
    /// </summary>
    public void PlayNew(AudioClip clip)
    {
        StopLooping(0);
        audioSource.clip = clip;
        audioSource.Play();      
        
    }

    /// <summary>
    /// Plays one of the clips in the Playlist list at random.
    /// </summary>
    /// <param name="volMin">Volume modulation minimum (in db). Offset by input gain.</param>
    /// <param name="volMax">Volume modulation minimum (in db). Offset by input gain.</param>
    /// <param name="pitchMin">Pitch modulation minimum.</param>
    /// <param name="pitchMax"></param>
    public void PlayRandom(float volMin, float volMax, float pitchMin, float pitchMax)
    {
        float vol = Random.Range(volMin, volMax);
        FadeTo(vol, 0, 0.5f, false);

        pitch = Random.Range(pitchMin, pitchMax);
        audioSource.pitch = pitch;

        StopLooping(0);

        var randomClip = AudioUtility.RandomClipFromList(playlist);
        audioSource.clip = randomClip;
        
        audioSource.Play();
    }



    ///
    public void PlayLoop()
    {
        loopClips = true;
        loopCoroutine = ClipLooper(intervalBetweenPlays);
        StartCoroutine(loopCoroutine);
    }
    public void PlayLoop(int interval)
    {
        loopClips = true;
        loopCoroutine = ClipLooper(interval);
        StartCoroutine(loopCoroutine);
    }
    public void StopLooping(float fadeOutTime)
    {
        if (loopCoroutine != null) StopCoroutine(loopCoroutine);

        if (fadeOutTime > 0.0f) StartCoroutine(StopSourceAfter(fadeOutTime));           
        else audioSource.Stop();

        looperIsLooping = false;

    }

    private IEnumerator StopSourceAfter(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        audioSource.Stop();
        yield break;
    }
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
                        // if no interval (straight loop on one clip): use audiosource native looper, which is more precise than coroutine
                        audioSource.loop = true;
                        audioSource.pitch = pitch + Random.Range(-pitchRand, pitchRand);

                        if (!audioSource.isPlaying) audioSource.Play();

                        looperIsLooping = true;
                        yield break;

                    }
                    else
                    {
                        // if there is an interval
                        newClip = audioSource.clip;
                        audioSource.loop = false;
                    }
                }
                else
                {
                    // ie if we're choosing a new clip each loop iteration
                    newClip = AudioUtility.RandomClipFromList(playlist);
                }

                float newClipPitch = pitch + Random.Range(-pitchRand, pitchRand);
                audioSource.pitch = newClipPitch;

                audioSource.clip = newClip;
                audioSource.Play();
                looperIsLooping = true;

                float newInterval = Mathf.Clamp(interval + Random.Range(-intervalRand, intervalRand), 0, interval + intervalRand);
                newInterval += (audioSource.clip.length / newClipPitch);
                yield return new WaitForSeconds(newInterval);

                looperIsLooping = false;

                yield return null;
            }
            else yield break;
        }
    }

    #endregion

    #region Fader

    public void FadeTo(float targetVol, float fadetime, float curveShape, bool stopAfterFade)
    {
        if (fadetime <= 0.0f)
        {
            faderVolume =  targetVol;
            UpdateParams();

            if (stopAfterFade)
            {
                audioSource.Stop();
            }               

            return;
        }
        
        curveShape = Mathf.Clamp(curveShape, 0.0f, 1.0f);

        currentFadeTarget = targetVol;
        currentFadeTime = fadetime;

        // Uses an AnimationCurve
        // curveShape 0.0 = ramps up (exponential); curveShape 0.5 = s-curve; curveshape 1.0 ramps down (logarithmic).
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, Mathf.Sin(curveShape), 0, 1.0f - curveShape);
        keys[1] = new Keyframe(1, 1, 1 - curveShape, 0, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);

        if (isFading)
        {
            StopCoroutine(fadeCoroutine);
            isFading = false;
        }
        fadeCoroutine = StartFadeInDb(fadetime, targetVol, animcur, stopAfterFade);
        StartCoroutine(fadeCoroutine);
        isFading = true;
    }


    private IEnumerator StartFadeInDb(float fadetime, float targetVol, AnimationCurve animcur, bool stopAfterFade)
    {
        float startVol = faderVolume;

        //Debug.Log(this + "on " + gameObject.name + " : Fading to " + targetVol + " from " + startVol + " over " + fadetime);
        float currentTime = 0f;
        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            faderVolume = Mathf.Lerp(startVol, targetVol, animcur.Evaluate(currentTime / fadetime));

            UpdateParams();
            yield return null;
        }

        isFading = false;

        if (stopAfterFade)
        {
            if (looperIsLooping) StopLooping(0);
            else audioSource.Stop();
        }

        yield break;
    }

    private void UpdateParams()
    {
        float currentVol = inputGain + faderVolume + outputGain;
        audioSource.volume = AudioUtility.ConvertDbtoA(currentVol);
    }


    #endregion
    private void OnDisable()
    {
        StopAllCoroutines();
    }

}
