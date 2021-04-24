using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>  
/// The AudioSourceFader fades the attached audio source component. It uses decibels (db) to scale the amplitude logarithmically and lets you customise the fade up and fade down curve separately, resulting in easy-to-use fades that are easier on the ear than directly fading the volume on the audio source. FadeUp, FadeDown and FadeDownAndStop are controllable via Unity Events with a float argument denoting fade time. FadeTo is controllable by script and lets the user specify target volume as well as duration.
/// </summary>
/// 
// # Inspector Options #
/// Output
/// - Audio Source: attach via Inspector, or leave blank to use this gameobject's audio source
/// - Output Gain (in Db): Since this script takes over the volume meter on the audio source, use this to adjust master gain.
/// - Fade In On Awake: Plays the audio and fades in when the script is loaded (ie the object is created or enabled, or the scene is loaded). Fade In Time specifies the duration in seconds.
/// Curves 
/// - Fade Up/Down Curve: click to open and edit curves, recall and store presets. Each curve has its strengths and weaknesses, use your ears.
/// Feedback: Fade
/// - Fade Progress: the most sure-fire way to check whether a fade is actually running.
/// - Fade Info: displays information on the current fade; if there's no fade, it's blank.
/// 
// # Changelog #
/// ## AudioSourceFader 1.2.1 by blubberbaleen 
/// (6 Mar '21)
/// 1.1 | 3 Mar: reduced feedback, added error compensating, created new function UpdateAudioSourceAmplitude so it's only working when it's being called rather than in Update. Fixed some jumping associated with erroneous initialisation.
/// 1.2 | 5 Mar: added a audiosource check in the fade coroutine. Added 'fade in on awake' function.
/// 1.2.1 | 6 Mar: Initialises a linear curve now since that's more normal.
/// 1.2.2 | 6 Mar: Added 'Fade To' function for script calls, with third argument specifying curveshape (0 = linear; 1 = weighted). More info in code.
/// 

// # Feedback to xavier@xaviervelastin.com.

[RequireComponent(typeof(AudioSource))]

public class AudioSourceFader : MonoBehaviour
{
    #region variables and inspector ui  

    [Header("Output")]
        public AudioSource audioSource;
        [Range(-70, 0)] public float outputGain;
        public bool FadeInOnAwake = false;
        public float FadeInTime = 5.0f;

    [Header("Curves")]
    // NB: This initialises a linear curve but will be overriden with changes in inspector.   
        public AnimationCurve FadeUpCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0, 0, 0), new Keyframe(1, 1, 0, 0, 0, 0));
        public AnimationCurve FadeDownCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0, 0, 0), new Keyframe(1, 1, 0, 0, 0, 0));

    // NB: Optional feedback in Inspector at runtime. To hide, uncomment "[HideInInspector]".
    [Header("Feedback: Fade")]
    //[HideInInspector]
        [SerializeField] [Range(0, 1)] float fadeProgress = 0;
    //[HideInInspector]
        [SerializeField] string fadeInfo;                              

    [Header("Feedback: Volume")]
    //[HideInInspector] 
        [SerializeField] [Range(0, 1)] float amplitude;
    //[HideInInspector] 
        [SerializeField] [Range(-100, 0)] float db;

    float adjustedAmplitude;
    bool isFading;
    #endregion

    

    void Start()
    {
        // If there's no audio source attached in the Inspector, adds the first one it finds on this object.
        CheckAudioSource();

        // Warning in case one or both of the fade curves are empty.
        if (FadeUpCurve.keys.Length == 0 | FadeDownCurve.keys.Length == 0)
        {
            Debug.LogError("No keyframes found for a fade curve on " + this.gameObject.name + "! Put something in or the fades won't work.");
        }
       

        
    #region Intialise volume levels
        // Audiosource volume should be set to either 0 or 1, use OutputGain in Inspector to set level.
        adjustedAmplitude = audioSource.volume;
        amplitude = audioSource.volume;
        db = ConvertAtoDb(amplitude);
    #endregion

       
        if (FadeInOnAwake)                      // If enabled, triggers a fade in once everything's initialised.
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log("AudioSourceFader on " + this.gameObject.name + ": Audio clip not set to 'Play on Awake', but play triggered anyway because 'fade in on awake' is enabled");
            }
            FadeUp(FadeInTime);
        }
        

    }

    private void OnValidate() // Checks for changes in inspector to trigger UpdateAudioSourceAmplitude().
    {        
        if (Application.isPlaying)
        {
            if (audioSource == null) return;  // If an audiosource compoennt has not been found, it won't update values, to protect against NaN errors.
            UpdateAudioSourceAmplitude();
        }
    }

    #region Unity Events-friendly functions (float argument is duration in seconds).

    public void FadeUp(float fadetime)                      // Made for Unity Events: fades up to output gain level over (fadetime) seconds.
    {
        if (FadeUpCurve.keys.Length > 0)
        {
            fadeInfo = "Fading up from " + (int)db + "db over " + fadetime + " seconds.";

            if (isFading)
            {
                StopAllCoroutines();
                isFading = false;
            }
            StartCoroutine(StartFadeInDb(fadetime, 0.0f, FadeUpCurve));
            isFading = true;           
        }
        else
        {
            Debug.LogError("No keyframes found for a fade curve on " + this.gameObject.name + "! Fade aborted.");
            return;
        }
    }

    public void FadeDown(float fadetime)                    // Made for Unity Events: fades down to -70db over (fadetime) seconds.
    {
        if (FadeDownCurve.keys.Length > 0)
        {
            fadeInfo = "Fading down from " + (int)db + "db over " + fadetime + " seconds.";

            if (isFading)
            {
                StopAllCoroutines();
                isFading = false;
            }
            StartCoroutine(StartFadeInDb(fadetime, -70.0f, FadeDownCurve));
            isFading = true;
        }
        else
        {
            Debug.LogError("No keyframes found for a fade curve on " + this.gameObject.name + "! Fade not started.");
            return;
        }
    }

    public void FadeDownAndStop(float fadetime)             // Made for Unity Events: fades down to -70db over (fadetime) seconds, then stops the clip.
    {
        if (FadeDownCurve.keys.Length > 0)
        {
            fadeInfo = "Fading down and stopping from " + (int)db + "db over " + fadetime + " seconds.";

            if (isFading)
            {
                StopAllCoroutines();
                isFading = false;
            }
            StartCoroutine(StartFadeInDb(fadetime, -70.0f, FadeDownCurve));
            StartCoroutine(StopClipAfterFade(audioSource, fadetime));
            isFading = true;
        }
        else
        {
            Debug.LogError("No keyframes found for a fade curve on " + this.gameObject.name + "! Fade not started.");
            return;
        }
    }
    
    #endregion

    public void FadeTo(float targetDb, float fadetime, float curveShape)      // Dynamic fade function. Curveshape argument modifies curviness of curve (more info in code).
    {
        // Experimental addition, potentially buggy: infers values over 0.0 as amplitude, and converts to dB. note: 0.0 amplitude will be read as 0db (ie. 1.0 amplitude).         
        if (targetDb > 0.0f)
        {
            targetDb = ConvertAtoDb(targetDb);
            Debug.Log("!!WARNING!!: AudioSourceFader on " + this.gameObject.name + ": " +
                "targetDb value was over 0 - FadeTo assumes a loudness (-inf to 0) value in decibels, but this looks like an amplitude (0 to 1) value; converting to " + targetDb + "db for now, but it's advised to use a decibel value in future.");
        }


        // curveShape handler: creates a new Animation Curve with values from curveShape. At curveShape = 0.0, it's flat (linear: good for most audio); at 0.5, it's an S-Curve (starts and ends slowly, good for fade-outs); at 1.0, it's exponential (starts quickly, then slows down, good for crossfades).       
        Keyframe[] keys = new Keyframe[2];
        keys[0] = new Keyframe(0, 0, 0, 1f - curveShape, 0, 1f - curveShape);
        keys[1] = new Keyframe(1, 1, 1f - curveShape, 0f, curveShape, 0);
        AnimationCurve animcur = new AnimationCurve(keys);


        if (FadeUpCurve.keys.Length > 0)
        {
            fadeInfo = "Fading from " + (int)db + "db to " + (int)targetDb + "db over " + fadetime + " seconds with a curve shape of " + curveShape + ".";

            if (isFading)
            {
                StopAllCoroutines();
                isFading = false;
            }
            StartCoroutine(StartFadeInDb(fadetime, targetDb, animcur));
            isFading = true;
        }
        else
        {
            Debug.LogError("No keyframes found for a fade curve on " + this.gameObject.name + "! Fade aborted.");
            return;
        }
    }


    private IEnumerator StartFadeInDb (float fadetime, float targetDb, AnimationCurve animcur) // Fade coroutine, based on a given curve.
    {
        Debug.Log("AudioSourceFader on " + this.gameObject.name + ": " + fadeInfo);

        CheckAudioSource();
        float currentTime = 0f;
        float startDb = db - outputGain;

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            adjustedAmplitude = ConvertDbtoA(Mathf.Lerp(startDb, targetDb, animcur.Evaluate(currentTime / fadetime)));
            fadeProgress = Mathf.Lerp(0, 1, currentTime / fadetime);

            UpdateAudioSourceAmplitude();

            yield return null;
        }

        isFading = false;
        fadeInfo = "";
        yield break;
    }

    private IEnumerator StopClipAfterFade(AudioSource AS, float fadetime)
    {
        yield return new WaitForSecondsRealtime(fadetime);
        AS.Stop();
        fadeInfo = "Audioclip stopped.";
        yield break;
    }


    private void UpdateAudioSourceAmplitude() // End of process chain, adjusted values sent to and received by Audio Source component.
    {
        // Updates volume on audiosource with adjusted fade volume, minus output gain reduction.
        audioSource.volume = adjustedAmplitude * ConvertDbtoA(outputGain);

        // Update values in inspector and for StartFade coroutine
        amplitude = audioSource.volume;
        db = ConvertAtoDb(amplitude);

    }


    private void CheckAudioSource() // At startup and before triggering a fade, checks if there's an audiosource component, and if not, associates it to the one on this gameobject.
    {
        if (audioSource == null)
        {
            Debug.Log("AudioSourceFader on " + this.gameObject.name + ": no attached Audiosource component found; associating the first one on this GameObject for all future fades received.");
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private float ConvertAtoDb(float amp)
    {
        amp = Mathf.Clamp(amp, ConvertDbtoA(-70f), 1f);
        return 20 * Mathf.Log(amp) / Mathf.Log(10);        
    }

    private float ConvertDbtoA(float db)
    {
        return Mathf.Pow(10, db / 20);        
    }
}
