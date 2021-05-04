using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>  
/// Fades the attached audiosource component.
/// </summary>
// 
// Adapted from original at https://github.com/xvelastin/unityaudioutility//

[RequireComponent(typeof(AudioSource))]

public class AudioSourceFader : MonoBehaviour
{
    #region variables and inspector ui  

    [Header("Output")]
        [SerializeField] AudioSource audioSource;
        [Range(-70, 0)] public float outputGain;
        public bool FadeInOnAwake = false;
        public float FadeInOnAwakeTime = 5.0f;

    [Header("Curves")]
        [SerializeField] AnimationCurve FadeUpCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0, 0, 0), new Keyframe(1, 1, 0, 0, 0, 0));
        [SerializeField] AnimationCurve FadeDownCurve = new AnimationCurve(new Keyframe(0, 0, 0, 0, 0, 0), new Keyframe(1, 1, 0, 0, 0, 0));

    [Header("Feedback: Fade")]
        [SerializeField] [Range(0, 1)] float fadeProgress = 0;
        [SerializeField] string fadeInfo;                              

    [Header("Feedback: Volume")]
        [SerializeField] [Range(0, 1)] float amplitude;
        [SerializeField] [Range(-100, 0)] float db;

    float adjustedAmplitude;
    bool isFading;
    #endregion

    

    void Awake()
    {
        CheckAudioSource();

     #region Intialise volume levels
        UpdateParams();
    #endregion

       
        if (FadeInOnAwake)                      // If enabled, triggers a fade in once everything's initialised.
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                //Debug.Log("AudioSourceFader on " + this.gameObject.name + ": Audio clip not set to 'Play on Awake', but play triggered anyway because 'fade in on awake' is enabled");
            }
            FadeUp(FadeInOnAwakeTime);
        }
    }

    public void UpdateParams()
    {
        // Audiosource volume should be set to either 0 or 1, use OutputGain in Inspector to set level.
        adjustedAmplitude = audioSource.volume;
        amplitude = audioSource.volume;
        db = AudioUtility.ConvertAtoDb(amplitude);
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
        //Debug.Log("AudioSourceFader on " + this.gameObject.name + ": " + fadeInfo);

        CheckAudioSource();
        float currentTime = 0f;
        float startDb = db - outputGain;

        while (currentTime < fadetime)
        {
            currentTime += Time.deltaTime;
            adjustedAmplitude = AudioUtility.ConvertDbtoA(Mathf.Lerp(startDb, targetDb, animcur.Evaluate(currentTime / fadetime)));
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


    public void UpdateAudioSourceAmplitude() // End of process chain, adjusted values sent to and received by Audio Source component.
    {
        CheckAudioSource();
        // Updates volume on audiosource with adjusted fade volume, minus output gain reduction.
        audioSource.volume = adjustedAmplitude * AudioUtility.ConvertDbtoA(outputGain);

        // Update values in inspector and for StartFade coroutine
        amplitude = audioSource.volume;
        db = AudioUtility.ConvertAtoDb(amplitude);

    }


    private void CheckAudioSource() // At startup and before triggering a fade, checks if there's an audiosource component, and if not, associates it to the one on this gameobject.
    {
        if (audioSource == null)
        {
            //Debug.Log("AudioSourceFader on " + this.gameObject.name + ": no attached Audiosource component found; associating the first one on this GameObject for all future fades received.");
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

}
