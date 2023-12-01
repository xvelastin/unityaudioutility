// v2.0 - 1 Dec 2023 - created by blubberbaleen - for more, see https://github.com/xvelastin/unityaudioutility //
// Public methods: Start, Stop, StopLooping, Pause, UnPause, TogglePaused, FadeIn, FadeOut, FadeTo.
// Subscribable Unity Events: OnPlayTriggered, OnStop, OnFinishedPlaying.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace XV
{
    /// <summary>
    /// A self-contained script that extends the functionality of Unity's Audio Source component. It can handle playback of multiple clips, looping that avoids repetitions, highly adjustable fades, and more. It uses decibels for volume, and semitones for pitch.
    /// </summary>
    public class AudioSourceController : MonoBehaviour
    {
        #region Global Settings
        public const float MINIMUM_VOLUME = -60.0f; 
        public const float MAXIMUM_VOLUME = 0.0f;   // you can only increase past 0db on the audio mixer or using an audio effect. 

        private const float PauseFadeTime = 0.2f;   // time to fade out and back in (in seconds) when Pause/Unpause is called, to avoid audible pops.
        
        #endregion
        
        #region Inspector Serialised Fields

        [Header("Playback")] 
        
        public List<AudioClip> Playlist = new();
        public EPlaybackBehaviour PlaybackBehaviour = EPlaybackBehaviour.Random;
        public enum EPlaybackBehaviour
        {
            Single,     // Repeats the last clip (or first in the Playlist if playing for the first time). 
            Sequential, // Plays the next clip in order. 
            Random      // Chooses a new clip at random.
        }

        [Tooltip("Avoids playing the last (n) clips played, to reduce audible repetitions. Only relevant if Playback Behaviour is 'Random'.")]
        [Min(0)]
        public int AvoidRepeatingClips = 1;

        [Tooltip("Use PlayOneShot for repeated overlapping sounds that don't need to either loop or move with the game object.")]
        public bool PlayOneShot;
        
        [Tooltip("Start playback as soon as the game object is enabled.")]
        public bool PlayOnEnable;

        [Tooltip("Loops the clips in the Playlist according to the Playback Behaviour.")] 
        [SerializeField] private bool loop;

        [Header("Parameters")]
        
        [Tooltip("The volume in decibels. Each increment of 3db is roughly twice the loudness, with 0 being the maximum a single Audio Source can send to an Audio Mixer.")]
        [SerializeField] private Parameter volume = new()
        {
            Value = 0f,
            RandomRange = 0f,
            MinValue = MINIMUM_VOLUME,
            MaxValue = MAXIMUM_VOLUME
        };

        [Tooltip("The pitch (playback speed) of the audio source in semitones. +12 semitones is equivalent to twice as fast, or an octave higher.")]
        [SerializeField] private Parameter pitch = new()
        {
            Value = 0f,
            RandomRange = 0f,
            MinValue = -12,
            MaxValue = 12
        };

        [Tooltip("Adds a delay in seconds before playing. For looping clips, this is applied between plays.")]
        [SerializeField] private Parameter delay = new()
        {
            Value = 0f,
            RandomRange = 0,
            MinValue = 0,
            MaxValue = -1
        };

        [Header("Fader")] 
        
        [SerializeField] private FadeSettings fadeInSettings = new(FadeSettings.ECurveDirection.In);

        [Tooltip("If true, the fade in is re-triggered along with the clip. If false, the fade in occurs independently.")]
        [SerializeField] private bool fadeInOnEachPlay;

        [SerializeField] private FadeSettings fadeOutSettings = new(FadeSettings.ECurveDirection.Out);

        [Tooltip("If true, Stop is delayed until the current clip finishes. Fade out instead affects the end of the clip.")]
        [SerializeField] private bool letClipFinish;

        [Header("Events")]
        [Space(10)]
        
        [Tooltip("Invoked whenever a sound starts.")]
        public UnityEvent OnPlay;

        [Tooltip("Invoked whenever Stop is called. Only triggers if it was already playing.")]
        public UnityEvent OnStop;

        [Tooltip("Invoked when no longer playing or looping. Does not apply to sounds started by PlayOneShot.")]
        public UnityEvent OnFinishedPlaying;  // this may not trigger exactly when the sound audibly ends. it's better used to find out for sure that the source can be disabled, destroyed or moved eg. for pooling. 
        
        [Tooltip("If true, when a non-looping sound plays, will start a coroutine to invoke OnFinishedPlaying once the audio source is no longer playing. Note that looping sounds will still invoke OnFinishedPlaying once they've finished fading out.")]
        [SerializeField] private bool checkForStopped = true;
        
        [Tooltip("The frequency in seconds the coroutine will run. Decrease for precision, increase for better performance.")]
        [SerializeField, Min(0.1f)] private float checkForStoppedFrequency = 1.0f; 

        #endregion

        #region Inspector Serialisable Classes

        [System.Serializable]
        public class Parameter
        {
            [SerializeField] private float _value;
            public float RandomRange;

            public float Value
            {
                get => _value;
                set => _value = ConstrainToMinMax(value);
            }

            [HideInInspector] public float MinValue;
            [HideInInspector] public float MaxValue;

            public float GetModulated()
            {
                return ConstrainToMinMax(ApplyRandom(Value, RandomRange));
            }

            private float ConstrainToMinMax(float value)
            {
                // set MaxValue to less than MinValue to only set a minimum value.
                return MaxValue < MinValue ? Mathf.Max(value, MinValue) : Mathf.Clamp(value, MinValue, MaxValue);
            }
        }

        [System.Serializable]
        public class FadeSettings
        {
            [Tooltip("The time it takes for the fade to complete.")] [Range(0, 30)]
            public float Duration;

            [Tooltip("Affects the speed of the change in volume when fading, from 0 (starts slow, gets faster: good for natural fades), to 1 (starts fast, gets slower: good for transitions). A value of 0.5 will produce an S-Curve (fast in the middle, slow at the start and end: good for most situations).")]
            [Range(0, 1)]
            public float CurveShape = 0.5f;

            [HideInInspector] public AnimationCurve Curve; // serialised with custom drawer
            [HideInInspector] public ECurveDirection Direction;

            public enum ECurveDirection
            {
                In,
                Out
            }

            public FadeSettings(ECurveDirection direction)
            {
                Direction = direction;
            }
        }
        
        #endregion

        #region Non-Serialised Properties
        
        public AudioSource Source { get; set; }

        /// <summary>
        /// Get or set the audio source volume in decibels.
        /// </summary>
        public float Volume
        {
            get => audioSourceVolume;
            set
            {
                volume.Value = value;
                UpdateVolume();
            }
        }

        /// <summary>
        /// Get or set the degree of randomness applied to the volume level.
        /// </summary>
        public float VolumeRandom
        {
            get => volume.RandomRange;
            set => volume.RandomRange = value;
        }
        private float audioSourceVolume
        {
            get => ToDecibels(Source.volume);
            set => Source.volume = ToAmplitude(value); 
        }

        /// <summary>
        /// The audio source playback speed (affecting pitch) in semitones.
        /// </summary>
        public float Pitch
        {
            get => audioSourcePitch;
            set
            {
                pitch.Value = value; 
                UpdatePitch();
            }
        }

        /// <summary>
        /// The degree of randomness applied to the pitch level.
        /// </summary>
        public float PitchRandom
        {
            get => pitch.RandomRange;
            set => pitch.RandomRange = value;
        }
        private float audioSourcePitch
        {
            get => ToSemitones(Source.pitch);
            set => Source.pitch = ToPitch(value);
        }

        /// <summary>
        /// Set the delay between consequent plays.
        /// </summary>
        public float Delay
        {
            get => audioSourceDelay;
            set
            {
                delay.Value = value;
                UpdateDelay();
            }
        }
        /// <summary>
        /// The degree of randomness applied to the delay level.
        /// </summary>
        public float DelayRandom
        {
            get => delay.RandomRange;
            set => delay.RandomRange = value;
        }
        private float audioSourceDelay { get; set; }

        /// <summary>
        /// Get or set the clip currently loaded into the Audio Source.
        /// </summary>
        public AudioClip Clip
        {
            get => Source.clip;
            set => Source.clip = value;
        }

        /// <summary>
        /// Get or set the loop state of the Audio Source Controller.
        /// </summary>
        public bool Loop
        {
            get => Source.loop || loop;
            set => loop = value;
        }
        
        /// <summary>
        /// Get or set the fade in time for this audio source controller.
        /// </summary>
        public float FadeInDuration
        {
            get => fadeInSettings.Duration;
            set => fadeInSettings.Duration = value;
        }
        
        /// <summary>
        /// Get or set the shape of the fade in curve. Values close to 0 produce an exponential, realistic curve, whereas close to 1 produce a logarithmic curve good for transitions. The default value of 0.5 produces a smooth curve suitable for most audio.
        /// </summary>
        public float FadeInCurveShape
        {
            get => fadeInSettings.CurveShape;
            set => fadeInSettings.CurveShape = value;
        }
        
        /// <summary>
        /// Get or set the fade in time for this audio source controller.
        /// </summary>
        public float FadeOutDuration
        {
            get => fadeOutSettings.Duration;
            set => fadeOutSettings.Duration = value;
        }
        
        /// <summary>
        /// Get or set the shape of the fade out curve. Values close to 0 produce an exponential, realistic curve, whereas close to 1 produce a logarithmic curve good for transitions. The default value of 0.5 produces a smooth curve suitable for most audio.
        /// </summary>
        public float FadeOutCurveShape
        {
            get => fadeOutSettings.CurveShape;
            set => fadeOutSettings.CurveShape = value;
        }

        /// <summary>
        /// Returns the play state as an enum. 
        /// </summary>
        public EPlayState PlayState { get; private set; } = EPlayState.Uninitialised;
        protected EPlayState playStateOnPaused;
        public enum EPlayState
        {
            Uninitialised,
            Stopped,
            Playing,
            Paused,
            Stopping,
        }
        
        #endregion

        #region Internal Fields
        
        // references
        private ClipLooper _looper;
        private Fader _fader;
        
        // playback
        private List<AudioClip> _clipsToAvoid = new();
        private int _currentClipIndex = -1;
        private Coroutine _stopAfterCoroutine;
        private Coroutine _delayedPlayCoroutine;
        private Coroutine _checkStoppedCoroutine;
        private bool _shouldCheckStopped;
        
        // stopping outside max distance
        private bool Is3DSound => Source.spatialBlend > 0;
        private bool _hasStoppedDueToOutsideMaximumDistance;
        private AudioListener _listener;

        #endregion

        #region Lifetime
        
        private void OnEnable()
        {
            Initialise();
            
            if (PlayOnEnable)
            {
                Play();
            }
        }

        private void Initialise()
        {
            if (!Source)
            {
                try
                {
                    Source = GetComponent<AudioSource>();
                }
                catch
                {
                    Debug.LogError(
                        $"{name} tried to initialise without an Audio Source component. Either add one to the game object, or set one in the Inspector.");
                }
            }
            
            Source.playOnAwake = false;

            _fader ??= new Fader(this);
            _fader.OnFadeEnd += HandleFadeEnd;

            _looper ??= new ClipLooper(this);

            PlayState = EPlayState.Stopped;
        }

        private void OnValidate()
        {
            if (Application.isPlaying && PlayState != EPlayState.Uninitialised)
            {
                UpdateAllParameters();
            }
        }
        private void LateUpdate()
        {
            if (PlayState == EPlayState.Uninitialised) return;
            HandleStopOutsideMaximumDistance();
        }

        private void OnDisable()
        {
            StopAudioSource();
            StopAllCoroutines();

            _fader.OnFadeEnd -= HandleFadeEnd;

            PlayState = EPlayState.Uninitialised;
        }

        #endregion

        #region Playing, Pausing, Stopping

        /// <summary>
        /// Plays a clip from the Playlist with the current volume, pitch and delay settings.
        /// </summary>
        public void Play()
        {
            if (PlayState == EPlayState.Uninitialised) return;
            
            _fader.FadeIn();
            
            ResetCoroutine(ref _delayedPlayCoroutine);
            ResetCoroutine(ref _stopAfterCoroutine);

            if (loop)
            {
                _looper.Start();
            }
            else
            {
                Delay = delay.GetModulated();

                if (Delay <= 0f)
                    StartPlayback();
                else
                    _delayedPlayCoroutine = StartCoroutine(DelayedPlay(Delay));
            }

            PlayState = EPlayState.Playing;
        }

        private IEnumerator DelayedPlay(float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            StartPlayback();
        }

        private void StartPlayback()
        {
            Clip = GetNewClip();
            if (!Clip) return;

            UpdateAllParameters(true);

            ResetCoroutine(ref _checkStoppedCoroutine);
            
            if (PlayOneShot)
            {
                Source.PlayOneShot(Clip);
                OnPlay?.Invoke();
                return;
            }
            
            if (fadeInOnEachPlay && PlayState != EPlayState.Stopping)
            {
                // clamp fade in time to clip length
                _fader.FadeIn(Mathf.Min(fadeInSettings.Duration, GetTotalClipDuration()));
            }
            
            Source.Play();
            OnPlay?.Invoke();

            
            _shouldCheckStopped = checkForStopped && !loop; // looping sounds have their own implementation to call OnFinishedPlaying
            if (_shouldCheckStopped)
            {
                _checkStoppedCoroutine = StartCoroutine(CheckStopped());
            }
        }
        
        public void Pause()
        {
            if (PlayState is EPlayState.Uninitialised) return;
            
            playStateOnPaused = PlayState;
            PlayState = EPlayState.Paused;

            _fader.Pause();
            _looper.Pause();
        }
        
        public void UnPause()
        {
            if (PlayState is EPlayState.Uninitialised) return;
            
            PlayState = EPlayState.Playing;

            Source.UnPause();
            _looper.UnPause();
            _fader.UnPause();
        }
        
        /// <summary>
        /// Pause or unpause playback, depending on the current play state.
        /// </summary>
        public void TogglePaused()
        {
            if (PlayState is EPlayState.Uninitialised or EPlayState.Stopped) return;
            
            switch (PlayState)
            {
                case EPlayState.Uninitialised or EPlayState.Stopped:
                    return;
                case EPlayState.Playing or EPlayState.Stopping:
                    Pause();
                    break;
                case EPlayState.Paused:
                    UnPause();
                    break;
            }
        }
        
        /// <summary>
        /// Stop playback with fade out if applied. Loops will continue running until the end of the fade out.
        /// </summary>
        public void Stop()
        {
            if (PlayState is EPlayState.Uninitialised or EPlayState.Stopped) return;

            PlayState = EPlayState.Stopping;

            if (letClipFinish)
            {
                _looper.Stop();

                if (GetTimeRemaining() < fadeOutSettings.Duration)
                {
                    _fader.FadeOut(GetTimeRemaining());
                    return;
                }

                _stopAfterCoroutine = StartCoroutine(StopAfter(GetTimeRemaining() - fadeOutSettings.Duration));
            }
            else
            {
                StopPlayback();
            }
        }

        /// <summary>
        /// Stops the sound from looping, but does not stop playback or trigger a fade out.
        /// </summary>
        public void StopLooping() => _looper.Stop();

        /// <summary>
        /// Returns the time in seconds until the end of the current clip.
        /// </summary>
        public float GetTimeRemaining()
        {
            return (Clip.length - Source.time) / Source.pitch;
        }

        private void StopPlayback()
        {
            OnStop?.Invoke();
            if (fadeOutSettings.Duration > 0)
            {
                _fader.FadeOut();
            }
            else
            {
                StopAudioSource();
            }
        }

        private IEnumerator StopAfter(float holdTime)
        {
            yield return new WaitForSeconds(holdTime);
            StopPlayback();
        }
        
        private void StopAudioSource()
        {
            ResetCoroutine(ref _checkStoppedCoroutine);
            Source.Stop();
            _looper.Stop();
            OnFinishedPlaying?.Invoke();
            PlayState = EPlayState.Stopped;
        }

        private void HandleFadeEnd()
        {
            switch (PlayState)
            {
                case EPlayState.Stopping:
                    StopAudioSource();
                    break;
                case EPlayState.Paused:
                    Source.Pause();
                    break;
            }
        }
        
        private IEnumerator CheckStopped()
        {
            var frequency = Mathf.Max(0.1f, checkForStoppedFrequency);
            while (Source.isPlaying && gameObject.activeInHierarchy)
            {
                yield return new WaitForSeconds(frequency);
            }
            OnFinishedPlaying?.Invoke();
        }
        
        private void ResetCoroutine(ref Coroutine coroutine)
        {
            if (coroutine == null) return;
            
            StopCoroutine(coroutine);
            coroutine = null;
        }

        #endregion
        
        #region Fading

        /// <summary>
        /// Fades in to the set Volume level over the duration set in Fade In Settings.
        /// </summary>
        public void FadeIn() => _fader?.FadeIn();

        /// <summary>
        /// Fades out to silence over the duration set in Fade Out Settings.
        /// </summary>
        public void FadeOut() => _fader?.FadeOut();

        /// <summary>
        /// Adjusts the volume level of the audio source over time.
        /// </summary>
        /// <param name="targetVolume">The level (in decibels) the audio source will be at by the end of the fade.</param>
        /// <param name="fadeTime">The time (in seconds) for the audio source to reach the target volume.</param>
        public void FadeTo(float targetVolume, float fadeTime)
        {
            _fader.FadeTo(Volume, targetVolume, fadeTime, targetVolume > Volume ? fadeInSettings : fadeOutSettings);
        }

        #endregion

        #region Setting AudioClip and Parameters
        private AudioClip GetNewClip()
        {
            if (Playlist.Count <= 0) return Clip ? Clip : null;
            
            switch (PlaybackBehaviour)
            {
                case EPlaybackBehaviour.Single:
                    // repeats the last clip, or plays the first from the playlist. 
                    if (Clip) return Clip;
                    _currentClipIndex = Mathf.Clamp(_currentClipIndex, 0, Playlist.Count);
                    break;
                case EPlaybackBehaviour.Sequential:
                    _currentClipIndex++;
                    _currentClipIndex %= Playlist.Count;
                    break;
                case EPlaybackBehaviour.Random:
                    // chooses randomly between available clips, avoiding the last clips played as set in DoNotRepeatClipsCount.
                    do _currentClipIndex = Mathf.Clamp(0, Random.Range(0, Playlist.Count), Playlist.Count);
                    while (ClipIsInAvoidList());
                    AddToAvoidList(Playlist[_currentClipIndex]);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            return Playlist[_currentClipIndex];
        }

        private bool ClipIsInAvoidList()
        {
            if (_clipsToAvoid.Count == 0 || _clipsToAvoid.Count >= Playlist.Count) return false;
            return _clipsToAvoid.Any(clip => clip == Playlist[_currentClipIndex]);
        }

        private void AddToAvoidList(AudioClip clip)
        {
            _clipsToAvoid.Add(clip);
            AvoidRepeatingClips = Mathf.Clamp(AvoidRepeatingClips, 0, Playlist.Count - 1);
            if (_clipsToAvoid.Count > AvoidRepeatingClips)
                _clipsToAvoid.RemoveRange(0, _clipsToAvoid.Count - AvoidRepeatingClips);
        }

        private float GetTotalClipDuration()
        {
            return Clip.length / Source.pitch;
        }

        private void UpdateAllParameters(bool modulate = false)
        {
            UpdateVolume(modulate);
            UpdatePitch(modulate);
            UpdateDelay(modulate);
        }

        private void UpdateVolume(bool modulate = false)
        {
            audioSourceVolume = modulate ? volume.GetModulated() : volume.Value;
            audioSourceVolume += _fader.FadeVolume;
        }

        private void UpdatePitch(bool modulate = false)
        {
            audioSourcePitch = modulate ? pitch.GetModulated() : pitch.Value;
        }

        private void UpdateDelay(bool modulate = false)
        {
            audioSourceDelay = modulate ? delay.GetModulated() : delay.Value;
        }


        private static float ApplyRandom(float centreValue, float modulationValue)
        {
            return Random.Range(centreValue - modulationValue, centreValue + modulationValue);
        }

        #endregion

        #region Amplitude-Decibel and Pitch-Semitone Converters
        public static float ToDecibels(float amplitude)
        {
            amplitude = Mathf.Clamp(amplitude, ToAmplitude(MINIMUM_VOLUME), 1f);
            return Mathf.Clamp(20 * Mathf.Log(amplitude) / Mathf.Log(10), MINIMUM_VOLUME, MAXIMUM_VOLUME);
        }
        public static float ToAmplitude(float decibels)
        {
            return decibels > MINIMUM_VOLUME ? Mathf.Pow(10, decibels / 20) : 0;
        }

        public static float ToSemitones(float pitch) => 12 * Mathf.Log(pitch, 2);
        public static float ToPitch(float semitones) => Mathf.Pow(2, semitones / 12);

        #endregion
        
        #region Fader & Looper
        public class Fader
        {
            public System.Action OnFadeEnd;

            public float FadeVolume { get; private set; }
            public EFadeState State { get; private set; }

            public enum EFadeState
            {
                Stopped,
                Fading,
                Paused
            }

            private Coroutine _fadeCoroutine;
            private AudioSourceController _asc;

            private System.Action<float, float, float, FadeSettings>
                OnPauseFade; // origin, destination, remaining time, settings

            private CachedFade _cachedFade;
            private bool _pausedDuringFade;

            private struct CachedFade
            {
                public float Origin;
                public float Destination;
                public float TimeRemaining;
                public FadeSettings Settings;
            }

            public Fader(AudioSourceController asc)
            {
                _asc = asc;
            }

            public void FadeIn()
            {
                FadeTo(MINIMUM_VOLUME, 0, _asc.fadeInSettings.Duration, _asc.fadeInSettings);
            }

            public void FadeIn(float fadeTime)
            {
                FadeTo(MINIMUM_VOLUME, 0, fadeTime, _asc.fadeInSettings);
            }

            public void FadeOut()
            {
                FadeTo(_asc.Volume, MINIMUM_VOLUME, _asc.fadeOutSettings.Duration, _asc.fadeOutSettings);
            }

            public void FadeOut(float fadeTime)
            {
                FadeTo(_asc.Volume, MINIMUM_VOLUME, fadeTime, _asc.fadeOutSettings);
            }

            public void FadeTo(float origin, float destination, float fadeTime, FadeSettings settings)
            {
                _asc.ResetCoroutine(ref _fadeCoroutine);
                _fadeCoroutine = _asc.StartCoroutine(Fade(origin, destination, fadeTime, settings));
            }

            public void Pause()
            {
                _asc.StartCoroutine(SnapshotAndPause());
            }

            private IEnumerator SnapshotAndPause()
            {
                if (State == EFadeState.Fading)
                {
                    _pausedDuringFade = true;
                    OnPauseFade += CacheFadeState;
                    yield return new WaitForEndOfFrame(); // ensure settings are saved
                }
                else
                {
                    _pausedDuringFade = false;
                    _cachedFade = new CachedFade() { Origin = _asc.Volume }; // save original volume for unpause
                }

                FadeOut(PauseFadeTime);
                yield return new WaitForSeconds(PauseFadeTime);
                State = EFadeState.Paused;
            }

            private void CacheFadeState(float origin, float destination, float timeRemaining, FadeSettings settings)
            {
                OnPauseFade -= CacheFadeState;
                _cachedFade = new CachedFade()
                {
                    Origin = origin,
                    Destination = destination,
                    TimeRemaining = timeRemaining,
                    Settings = settings
                };
            }

            public void UnPause()
            {
                if (State != EFadeState.Paused) return;
                _asc.StartCoroutine(ResumePreviousFade());
            }

            private IEnumerator ResumePreviousFade()
            {
                // quick fade in to paused level
                FadeTo(_asc.Volume, _cachedFade.Origin, PauseFadeTime, _asc.fadeInSettings);

                yield return new WaitForSeconds(PauseFadeTime);

                if (_pausedDuringFade)
                {
                    // resume fade
                    FadeTo(_asc.Volume, _cachedFade.Destination, _cachedFade.TimeRemaining, _cachedFade.Settings);
                }

                _asc.PlayState = _asc.playStateOnPaused;
            }

            private IEnumerator Fade(float origin, float destination, float fadeTime, FadeSettings settings)
            {
                var animationCurve = settings.Curve;
                var direction = settings.Direction;

                if (fadeTime <= 0.0f)
                {
                    FadeVolume = destination;
                }
                else
                {
                    // fade starts                    
                    State = EFadeState.Fading;
                    float currentTime = 0.0f;
                    while (currentTime < fadeTime)
                    {
                        currentTime += Time.deltaTime;

                        // lerp forwards for fade in, backwards for fade out
                        FadeVolume = Mathf.Lerp(origin, destination, direction == FadeSettings.ECurveDirection.In
                            ? animationCurve.Evaluate(currentTime / fadeTime)
                            : 1 - animationCurve.Evaluate(currentTime / fadeTime));

                        _asc.UpdateVolume();

                        OnPauseFade?.Invoke(FadeVolume, destination, fadeTime - currentTime, settings);

                        yield return 0;
                    }
                }

                // fade ends
                OnFadeEnd?.Invoke();
                State = EFadeState.Stopped;
            }
        }

        public class ClipLooper
        {
            public EClipLooperState State { get; private set; }

            public enum EClipLooperState
            {
                Stopped,
                Looping,
                Paused
            }

            private readonly AudioSourceController _asc;
            private Coroutine _loopCoroutine;
            private System.Action<float> SaveLoopPosition;
            private CachedLoopState _cachedLoop;
            private bool _canInvokeOnPlayTriggered;
            
            private const double playInvokeThreshold = 0.1f;
            
            private struct CachedLoopState
            {
                public float RemainingTime;
            }

            public ClipLooper(AudioSourceController asc)
            {
                _asc = asc;
            }

            public void Start()
            {
                if (State == EClipLooperState.Looping) Stop();

                _loopCoroutine = _asc.StartCoroutine(StartLoop());
            }

            public void Pause()
            {
                if (State == EClipLooperState.Looping)
                {
                    SaveLoopPosition += OnLoopPositionSaved;
                }

                State = EClipLooperState.Paused;
                _asc.Source.loop = false;
            }

            private void OnLoopPositionSaved(float remainingTime)
            {
                SaveLoopPosition -= OnLoopPositionSaved;
                _cachedLoop = new CachedLoopState() { RemainingTime = remainingTime };
            }


            public void UnPause()
            {
                if (State != EClipLooperState.Paused) return;
                
                _asc.StartCoroutine(UnPauseCoroutine());
            }

            private IEnumerator UnPauseCoroutine()
            {
                yield return new WaitForSeconds(_cachedLoop.RemainingTime);
                Start();
            }

            public void Stop()
            {
                if (ShouldUseUnityLooper())
                {
                    _asc.Source.loop = false;
                }
                else if (_loopCoroutine != null)
                {
                    _asc.StopCoroutine(_loopCoroutine);
                    _loopCoroutine = null;
                }

                State = EClipLooperState.Stopped;
            }

            private IEnumerator StartLoop()
            {
                State = EClipLooperState.Looping;
                var sampleRate = AudioSettings.outputSampleRate;

                while (State == EClipLooperState.Looping)
                {
                    if (ShouldUseUnityLooper())
                    {
                        // loop as god intended
                        _asc.Source.loop = true;
                        _asc.StartPlayback();

                        // invoke OnPlayTriggered when clip restarts when using unity looper
                        while (_asc.Source.loop && _asc.Source.isPlaying)
                        {
                            var timelinePosition = (double)_asc.Source.timeSamples / sampleRate;

                            if (timelinePosition < playInvokeThreshold && _canInvokeOnPlayTriggered)
                            {
                                _asc.OnPlay?.Invoke();
                                _asc.UpdateAllParameters();
                                _canInvokeOnPlayTriggered = false;
                            }

                            if (timelinePosition > playInvokeThreshold)
                            {
                                _canInvokeOnPlayTriggered = true;
                            }

                            yield return 0;
                            if (!_asc.Source.isPlaying || !_asc.Source.loop) break;
                        }

                        yield break;
                    }

                    _asc.Source.loop = false;

                    _asc.StartPlayback();

                    var clipLength = _asc.GetTotalClipDuration();
                    var delay = clipLength + _asc.Delay;
                    var currentTime = 0.0f;
                    while (currentTime < delay)
                    {
                        currentTime += Time.deltaTime;
                        SaveLoopPosition?.Invoke(delay - currentTime);
                        yield return 0;
                    }
                }
            }

            private bool ShouldUseUnityLooper()
            {
                return _asc.PlaybackBehaviour == EPlaybackBehaviour.Single 
                       && _asc.Delay == 0 
                       && _asc.PlayOneShot == false;
            }
        }

        #endregion

        private void HandleStopOutsideMaximumDistance()
        {
            if (!Is3DSound || PlayState is EPlayState.Stopped or EPlayState.Paused) return;
            
            _listener ??= FindObjectOfType<AudioListener>();
            var distanceToListener = Vector3.Distance(gameObject.transform.position, _listener.transform.position);  

            if (distanceToListener > Source.maxDistance)
            {
                if (_hasStoppedDueToOutsideMaximumDistance) return;
                
                if (loop) _looper.Pause();
                _hasStoppedDueToOutsideMaximumDistance = true;
            }
            else
            {
                if (!_hasStoppedDueToOutsideMaximumDistance) return;
                
                if (loop) _looper.UnPause();

                _hasStoppedDueToOutsideMaximumDistance = false;
            }
        }
    }
}
