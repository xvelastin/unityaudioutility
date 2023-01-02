// v1.3 - 1 Jan 2023 //
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All-in-one script for advanced playback and volume control of Audio Source components without middleware, suitable for WebGL targets. A two-part gain stage allows for setting a fixed maximum and directly adjusting levels in decibels. The Fader allows for smooth, dynamic fading to suit a variety of situations. The Clip Player provides controls for looping, randomisation and modulation of multiple Audio Clips.
/// </summary>
/// 

namespace XV
{
    [RequireComponent(typeof(AudioSource)), DisallowMultipleComponent]
    public class AudioSourceController : MonoBehaviour
    {
        /// <summary>
        /// Curve shapes for AudioSourceController's Fader. 
        /// </summary>
        public enum FadeCurveType
        {
            /// <summary>Fade starts quickly and then eases down. Example uses: bringing in a music track transition, dipping background sound for a pause menu.</summary>
            FastInSlowOut,
            /// <summary>An s-curve. Suitable for most audio as it's quick to start and end but smooth in the middle.</summary>
            EaseInOut,
            /// <summary>An exponential curve. Starts slowly and then quickly ramps up. Example uses: to simulate something coming towards the listener realistically, fading out a music track transition.</summary>
            SlowInFastOut
        }

        public enum PlaybackBehaviour
        {
            /// <summary>Plays or loops the selected clip only.</summary>
            Single,
            /// <summary>Plays the next clip or loops all clips in the playlist in order.</summary>
            Sequential,
            /// <summary>Plays or loops all clips in the playlist at random with equal weighting.</summary>
            Random,
        }

        /// <summary> The Audio Source to control. This should ideally be not changed at runtime. </summary>
        public AudioSource source;

        [Header("Gain")]
        [Range(-70, 12), Tooltip("The overall volume of this Audio Source. To set at runtime, use the SetOutputVolume method.")]
        public float outputVolume = 0.0f;
        [Range(-70, 12), Tooltip("The volume in decibels this Audio Source starts at.")]
        public float startingVolume = 0.0f;

        [Header("Clip Player")]
        [Tooltip("Load clips here to be played back by the Audio Source Controller.")]
        public List<AudioClip> playlist = new List<AudioClip>();

        private int currentClipIndex = -1;

        [Tooltip("Starts playback as soon as the object is loaded. Note: the Audio Source Controller disables the native Audio Source Play On Awake option in order for it to work, so you have to use this.")]
        public bool playOnAwake = false;

        [Tooltip("Defines how the clips are selected from the Playlist.\n Single: plays back the same clip each time.\n Sequential: plays back the next clip in the playlist.\n Random: chooses a new clip at random from the playlist.")]
        public PlaybackBehaviour playbackBehaviour;

        [Tooltip("If true, the clips in the Playlist will be looped")]
        public bool loopClips = false;

        private bool looperIsLooping = false;

        [Tooltip("When the looper is looping, sets a delay between selecting and playing the next clip."), Min(0)]
        public float intervalBetweenPlays = 0.0f;
        [Tooltip("Depth of interval modulation. The time calculation is Interval +/- (Interval Rand / 2), with the randomisation retriggering when the previous clip gets played."), Min(0)]
        public float intervalRand = 0.0f;

        [Header("Modulation")]
        [Tooltip("Input gain stage in db, modulated by Gain Rand."), Range(-70, 12)]
        public float gain = 0f;
        [Tooltip("Depth of volume modulation in db. The gain calculation is Gain +/- (Gain Rand / 2), with the randomisation retriggering on each call of Play or PlayOnce. Ie. a Gain level of -6 with a Gain Rand of 3 can output from -9 to -3."), Range(0, 12)]
        public float gainRand = 0f;

        [Tooltip("Pitch of the Audio Source. Negative values will play backwards. Note: for a WebGL build this will affect speed but not pitch."), Range(-3f, 3f)]
        public float pitch = 1f;
        [Tooltip("Depth of pitch modulation. The final pitch calculation is Pitch +/- (Pitch / 2), with the randomisation retriggering on each call of Play or PlayOnce. Ie. a Pitch level of 1 with a Pitch Rand of 0.2 can output from 0.8 to 1.2."), Range(0, 3f)]
        public float pitchRand = 0f;

        [Header("Fader")]
        [Tooltip("Triggers a fade in as soon as the object is loaded. Note this is independent of Play On Awake.")]
        public bool fadeInOnAwake = false;
        [Tooltip("The fade time for the above fade in."), Min(0)]
        public float fadeInOnAwakeTime = 0.0f;
        [Tooltip("If no fade curve is specified, choose the curve type that this ASC uses to define the rate of change of volume during the fade. See code for details.")]
        public FadeCurveType defaultFadeCurveType = FadeCurveType.EaseInOut;
        private bool isFading = false;
        private float faderVolume = 0.0f;


        [Header("Debug")]
        public bool printMessagesToConsole;

        private IEnumerator fadeCoroutine;
        private IEnumerator loopCoroutine;
        private bool hasInit = false;


        #region Initialisation
        private void Awake()
        {
            if (!source) source = GetComponent<AudioSource>();

            // Takes over audiosource functions and get ready for own initialisation.
            if (source.isPlaying) source.Stop();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = AudioUtility.ConvertDbtoAmplitude(startingVolume);
            source.pitch = pitch = ModulateParam(pitch, pitchRand);
            faderVolume = startingVolume;
            SetOutputVolume(outputVolume);

            // If there's nothing in the playlist, check audio source and load this. If nothing there, throw error.
            if (playlist.Count == 0)
            {
                if (source.clip != null)
                {
                    playlist.Add(source.clip);
                }
                else
                {
                    Debug.LogError(this + ": You must attach at least one AudioClip to the AudioSource or the AudioSourceController");
                }
            }

            // Chooses the first clip to playback based on playback behaviour.
            switch (playbackBehaviour)
            {
                case PlaybackBehaviour.Single:
                case PlaybackBehaviour.Sequential:
                    source.clip = playlist[0];
                    break;

                case PlaybackBehaviour.Random:
                    source.clip = GetNewClip();
                    break;
            }

        }

        private void Start()
        {
            if (playOnAwake)
            {
                if (printMessagesToConsole) Debug.Log(this + ": Playing on awake.");
                Play();
            }

            if (fadeInOnAwake)
            {
                if (printMessagesToConsole) Debug.Log(this + ": Fading in on awake.");
                FadeTo(0f, fadeInOnAwakeTime, 1.0f, false);
            }

            hasInit = true;
        }


        #endregion

        #region Clip Player/Looper

        /// <summary>
        /// Triggers playback with the current settings.
        /// </summary>
        public void Play()
        {
            if (loopClips)
            {
                // Play loop modulated with current settings.
                PlayLoop();
            }
            else
            {
                // Play oneshot modulated with current settings.
                StopLooping(0);
                source.clip = GetNewClip();

                float oldgain = gain;
                gain = ModulateParam(gain, gainRand);
                UpdateAudiosourceVolume();

                source.pitch = ModulateParam(pitch, pitchRand);
                source.Play();

                if (printMessagesToConsole)
                    Debug.Log(this + ": Playing modulated oneshot. Clip: " + source.clip.name + ". Modulated volume: " + gain + ". Modulated pitch: " + source.pitch);

                gain = oldgain; // Resets to avoid drift.
            }
        }

        /// <summary>
        /// Plays the clip that is currently loaded in the Audio Source once, regardless of the current playback settings.
        /// </summary>
        public void PlayOnce()
        {
            StopLooping(0);
            source.Play();
        }

        /// <summary>
        /// Sets and plays a new Audio Clip once, regardless of loop settings. In order to take advantage of the Clip Looper's functionality, it is preferable to add the clip to the playlist and then play with the Play method.
        /// </summary>
        /// <param name="clip">The Audio Clip to be played.</param>
        public void PlayOnce(AudioClip clip)
        {
            StopLooping(0);
            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// Plays a clip from the Playlist at the given index once, regardless of loop settings. 
        /// </summary>
        /// <param name="index">The index of the clip in the Playlist.</param>
        public void PlayOnce(int index)
        {
            StopLooping(0);
            source.clip = playlist[index];
            source.Play();
        }

        /// <summary>
        /// Plays a clip in the Playlist according to playback behaviour. Modulates volume and pitch by a given random range.
        /// </summary>
        /// <param name="gain">Volume modulation minimum (in db). Offset by input gain.</param>
        /// <param name="gainRand">Volume modulation maximum (in db). Offset by input gain.</param>
        /// <param name="pitch">Pitch modulation minimum.</param>
        /// <param name="pitchRand">Pitch modulation maximum.</param>
        public void PlayOnceModulated(float gain, float gainRand, float pitch, float pitchRand)
        {
            StopLooping(0);
            source.clip = GetNewClip();

            // modulate volume
            this.gain = gain;
            this.gainRand = gainRand;
            this.gain = ModulateParam(gain, gainRand);
            UpdateAudiosourceVolume();

            this.pitch = pitch;
            this.pitchRand = pitchRand;
            source.pitch = ModulateParam(pitch, pitchRand);

            source.Play();

            if (printMessagesToConsole) Debug.Log(this + ": Playing modulated oneshot. Clip: " + source.clip.name + ". Modulated volume: " + this.gain + ". Modulated pitch: " + source.pitch);

            this.gain = gain; // Reset original volume so it doesn't drift
        }

        /// <summary>
        /// Plays the Audio Clip or Clips on a loop with the current settings.
        /// </summary>
        public void PlayLoop()
        {
            if (printMessagesToConsole) Debug.Log(this + ": Looping playlist with the playback mode: " + playbackBehaviour.ToString());
            loopClips = true;
            loopCoroutine = ClipLooper();
            StartCoroutine(loopCoroutine);
        }

        /// <summary>
        /// Stops all activity from the Clip Looper. If there is a clip playing, it waits until the current clip has finished before stopping.
        /// </summary>
        public void StopLooping()
        {
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
            }
            else return;

            if (looperIsLooping)
            {
                float timeRemaining = source.clip.length - source.time;
                StartCoroutine(StopSourceAfter(timeRemaining));
            }

            looperIsLooping = false;
        }

        /// <summary>
        /// Stops all activity from the Clip Looper after a delay in seconds.
        /// </summary>
        /// <param name="delay">A time in seconds to wait before stopping the Audio Source, eg. to allow time for a fade. A value of 0.0 stops the clip immediately regardless of how long it has left.</param>
        public void StopLooping(float delay)
        {
            if (looperIsLooping)
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

                source.loop = false;
                looperIsLooping = false;
            }

        }

        private IEnumerator StopSourceAfter(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            source.Stop();
            if (printMessagesToConsole) Debug.Log(this + ": Stopping source.");
            yield break;
        }

        // Loops the clips in Playlist with randomisation, modulation and interval settings.
        private IEnumerator ClipLooper()
        {
            while (true)
            {
                if (!looperIsLooping)
                {
                    AudioClip newClip;

                    // play back the same clip or choose a new one?
                    if (playbackBehaviour == PlaybackBehaviour.Single)
                    {
                        if (intervalBetweenPlays == 0.0f)
                        {
                            // if no interval (straight loop on one clip): use audiosource native looper, which is more precise than coroutine, and stop the clip looper.
                            source.loop = true;

                            // Modulation only happens once the loop starts, but doesn't re-trigger on each loop.
                            source.pitch = ModulateParam(pitch, pitchRand);


                            if (!source.isPlaying)
                            {
                                source.Play();
                                if (printMessagesToConsole) Debug.Log(this + ": Looping single clip: " + source.clip);
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
                        newClip = GetNewClip();
                    }

                    ModulateGain();
                    source.pitch = ModulateParam(pitch, pitchRand);

                    source.clip = newClip;
                    source.Play();
                    looperIsLooping = true;
                    if (printMessagesToConsole)
                    {
                        Debug.Log(this + ": Playing clip " + currentClipIndex + " of " + playlist.Count + ": " + source.clip);
                    }

                    // Applies randomness to the interval with Interval Rand.
                    float newInterval = Mathf.Clamp(ModulateParam(intervalBetweenPlays, intervalRand), 0, intervalBetweenPlays + intervalRand);
                    float clipLength = source.clip.length / source.pitch;
                    newInterval += clipLength;
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



        private AudioClip GetNewClip()
        {
            AudioClip newAudioClip;
            int newClipIndex;

            // Get new clip based on playback behaviour
            switch (playbackBehaviour)
            {
                default:
                    currentClipIndex = Mathf.Clamp(currentClipIndex, 0, 1);
                    newClipIndex = currentClipIndex;
                    break;

                case PlaybackBehaviour.Single:
                    // selects current audio clip
                    currentClipIndex = Mathf.Clamp(currentClipIndex, 0, 1);
                    newClipIndex = currentClipIndex;
                    break;

                case PlaybackBehaviour.Sequential:
                    // selects next one in sequence
                    newClipIndex = (currentClipIndex + 1) % (playlist.Count);
                    break;

                case PlaybackBehaviour.Random:
                    // selects random from list
                    currentClipIndex = Mathf.Clamp(currentClipIndex, 0, 1);
                    newClipIndex = Mathf.Clamp(0, Random.Range(0, playlist.Count), playlist.Count);
                    break;
            }

            currentClipIndex = newClipIndex;
            newAudioClip = playlist[currentClipIndex];
            return newAudioClip;

        }
        #endregion

        #region Fader

        /// <summary>
        /// Shortcut of FadeTo(volume:0db, fadeTime, stopAfterFade:false). Use with Play() to bring a sound in using the default curve on the Inspector.
        /// </summary>
        /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
        public void FadeIn(float fadeTime)
        {
            FadeTo(0, fadeTime, false);
        }

        /// <summary>
        /// Shorthand of FadeTo(volume:minimum, fadeTime, stopAfterFade:true). Use to fade a sound out to silence and then stop it.
        /// </summary>
        /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
        public void FadeOut(float fadeTime)
        {
            FadeTo(AudioUtility.minimum, fadeTime, true);
        }

        /// <summary>
        /// Begins a fade on the attached Audio Source with a smooth curve good for most sounds.
        /// </summary>
        /// <param name="targetVol">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
        /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
        /// <param name="stopAfterFade">If true, stops the Audio Source once the fade is finished.</param>
        public void FadeTo(float targetVol, float fadeTime, bool stopAfterFade)
        {
            AnimationCurve fadeCurve = CurveFromType(defaultFadeCurveType);

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

            if (isFading)
            {
                StopCoroutine(fadeCoroutine);
                isFading = false;
            }

            AnimationCurve fadeCurve = DrawFadeCurve(curveShape);
            fadeCoroutine = StartFadeInDb(fadeTime, targetVol, fadeCurve, stopAfterFade);
            StartCoroutine(fadeCoroutine);
            isFading = true;
        }

        /// <summary>
        /// Begins a fade on the attached Audio Source defined by a Fade Curve Type.
        /// </summary>
        /// <param name="targetVol">The volume in decibels that the Audio Source will be at at the end of the fade time.</param>
        /// <param name="fadeTime">The time in seconds for the Audio Source to reach the target volume.</param>
        /// <param name="curveType">The type of curve, which defines the rate of change of the volume over time.</param>
        /// <param name="stopAfterFade">If true, stops the Audio Source once the fade is finished.</param>
        public void FadeTo(float targetVol, float fadeTime, FadeCurveType curveType, bool stopAfterFade)
        {
            if (isFading)
            {
                StopCoroutine(fadeCoroutine);
                isFading = false;
            }

            AnimationCurve fadeCurve = CurveFromType(curveType);
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

        private static AnimationCurve CurveFromType(FadeCurveType curveType)
        {
            float curveShape;
            switch (curveType)
            {
                default:
                    curveShape = 0.5f;
                    break;

                case FadeCurveType.FastInSlowOut:
                    curveShape = 1.0f;
                    break;

                case FadeCurveType.EaseInOut:
                    curveShape = 0.5f;
                    break;

                case FadeCurveType.SlowInFastOut:
                    curveShape = 0.0f;
                    break;
            }
            return DrawFadeCurve(curveShape);
        }

        private static AnimationCurve DrawFadeCurve(float curveShape)
        {
            // Creates an Animation Curve to evaluate the fade over time from exponential (0.0) to s-curve (0.5) to logarithmic (1.0)
            Keyframe[] keys = new Keyframe[2];
            keys[0] = new Keyframe(0, 0, 0, -(Mathf.Cos(curveShape * Mathf.PI) - 1) / 2, 0, 1.0f - curveShape);
            keys[1] = new Keyframe(1, 1, 1 - curveShape, 0, curveShape, 0);
            return new AnimationCurve(keys);
        }

        private IEnumerator StartFadeInDb(float fadeTime, float targetVol, AnimationCurve fadeCurve, bool stopAfterFade)
        {
            if (printMessagesToConsole)
            {
                if (stopAfterFade)
                    Debug.Log(this + ": Fading from " + AudioUtility.ConvertAmplitudetoDb(source.volume) + "db to " + targetVol + "db over " + fadeTime + " seconds and stopping Audio Source when finished.");
                else
                    Debug.Log(this + ": Fading from " + AudioUtility.ConvertAmplitudetoDb(source.volume) + "db to " + targetVol + "db over " + fadeTime + " seconds.");
            }
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

        #endregion

        private void ModulateGain()
        {
            float oldgain = gain;
            gain = ModulateParam(gain, gainRand);
            UpdateAudiosourceVolume();
            gain = oldgain;
        }

        private static float ModulateParam(float centreValue, float modulationValue)
        {
            return centreValue + Random.Range(-modulationValue / 2, modulationValue / 2);
        }

        private void CheckAudiosource()
        {
            if (!source)
            {
                source = GetComponent<AudioSource>();
            }
        }

        public void SetOutputVolume(float value)
        {
            outputVolume = value;
            UpdateAudiosourceVolume();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && hasInit)
            {
                UpdateAudiosourceVolume();
            }
        }

        private void UpdateAudiosourceVolume()
        {
            float currentVolInDB = gain + faderVolume + outputVolume;
            float currentVolInAmps = AudioUtility.ConvertDbtoAmplitude(currentVolInDB);
            source.volume = currentVolInAmps;

            if (printMessagesToConsole)
            {
                if (currentVolInDB > 0.0)
                    Debug.LogWarning(this + " is trying to set volume to " + currentVolInDB + "db. However the real volume won't exceed 0db as Unity clamps it to maximum 1.0 amplitude.");
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }


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

    }
}