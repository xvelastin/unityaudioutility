// by blubberbaleen //
// v1.0 - 20 Jan 2022 //

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XV
{
    /// <summary>
    /// Provides an interface for playback of dialogue lines. Groups of similar lines can be triggered randomly with or without repetition. Trigger lines by passing either the <int> index or the <string> name of the character and their line. Individual clips can be triggered directly with an additional argument.
    /// </summary>
    public class DialoguePlayback : MonoBehaviour
    {
        public List<SpeakingCharacter> speakingCharacters = new List<SpeakingCharacter>() { null };

        // PlayLine methods will randomly trigger any clip within a given character's line. Useful to trigger variations of a clip for situations where you might hear a character say the same sort of thing many times.
        public void PlayLine(int characterIndex, int lineIndex)
        {
            speakingCharacters[characterIndex].lines[lineIndex].PlayRandom(speakingCharacters[characterIndex].audioSource);
        }
        public void PlayLine(int characterIndex, string lineName)
        {
            for (int i = 0; i < speakingCharacters[characterIndex].lines.Count; i++)
            {
                if (speakingCharacters[characterIndex].lines[i].name == lineName)
                {
                    speakingCharacters[characterIndex].lines[i].PlayRandom(speakingCharacters[characterIndex].audioSource);
                }
            }
        }
        public void PlayLine(string characterName, int lineIndex)
        {
            for (int i = 0; i < speakingCharacters.Count; i++)
            {
                if (speakingCharacters[i].name == characterName)
                {
                    speakingCharacters[i].lines[lineIndex].PlayRandom(speakingCharacters[i].audioSource);
                }
            }
        }
        public void PlayLine(string characterName, string lineName)
        {
            for (int a = 0; a < speakingCharacters.Count; a++)
            {
                if (speakingCharacters[a].name == characterName)
                {
                    for (int b = 0; b < speakingCharacters[a].lines.Count; b++)
                    {
                        if (speakingCharacters[a].lines[b].name == lineName)
                        {
                            speakingCharacters[a].lines[b].PlayRandom(speakingCharacters[a].audioSource);
                        }
                    }
                }
            }
        }

        // PlayClip methods will trigger a specific clip. Useful for linear and/or static playback, where you know the clip can always be the same.
        public void PlayClip(int characterIndex, int lineIndex, int clipIndex)
        {
            speakingCharacters[characterIndex].lines[lineIndex].PlayNew(speakingCharacters[characterIndex].audioSource, clipIndex);
        }
        public void PlayClip(int characterIndex, string lineName, int clipIndex)
        {
            for (int i = 0; i < speakingCharacters[characterIndex].lines.Count; i++)
            {
                if (speakingCharacters[characterIndex].lines[i].name == lineName)
                {
                    speakingCharacters[characterIndex].lines[i].PlayNew(speakingCharacters[characterIndex].audioSource, clipIndex);
                }
            }
        }
        public void PlayClip(string characterName, int lineIndex, int clipIndex)
        {
            for (int i = 0; i < speakingCharacters.Count; i++)
            {
                if (speakingCharacters[i].name == characterName)
                {
                    speakingCharacters[i].lines[lineIndex].PlayNew(speakingCharacters[i].audioSource, clipIndex);
                }
            }
        }
        public void PlayClip(string characterName, string lineName, int clipIndex)
        {
            for (int a = 0; a < speakingCharacters.Count; a++)
            {
                if (speakingCharacters[a].name == characterName)
                {
                    for (int b = 0; b < speakingCharacters[a].lines.Count; b++)
                    {
                        if (speakingCharacters[a].lines[b].name == lineName)
                        {
                            speakingCharacters[a].lines[b].PlayNew(speakingCharacters[a].audioSource, clipIndex);
                        }
                    }
                }
            }
        }

        [System.Serializable]
        public class SpeakingCharacter
        {
            [Header("Character")]
            [Tooltip("Characters can be addressed by name as well as by index.")]
            public string name;
            [Tooltip("The target AudioSource, probably on the character GameObject")]
            public AudioSource audioSource;
            public List<DialogueLine> lines = new List<DialogueLine>() { null };
        }

        [System.Serializable]
        public class DialogueLine
        {
            [Header("Line")]
            [Tooltip("Lines can be addressed by name as well as by index.")]
            public string name;
            [Tooltip("If true, clips will be removed from the list after playing.")]
            public bool doNotRepeatClips = false;
            [Tooltip("If true, new calls to Play functions will stop any audio currently playing. If false, the new will not trigger.")]
            public bool allowInterruptions = true;
            public List<AudioClip> clips = new List<AudioClip>() { null };

            public void PlayNew(AudioSource voice, int index)
            {
                if (voice.isPlaying)
                {
                    if (!allowInterruptions)
                    {
                        return;
                    }
                    else Stop(voice);
                }

                voice.clip = clips[index];
                voice.Play();
            }

            public void PlayRandom(AudioSource voice)
            {
                if (voice.isPlaying)
                {
                    if (!allowInterruptions)
                    {
                        return;
                    }
                    else Stop(voice);
                }
                if (clips.Count == 0)
                {
                    Debug.Log("Attempted to play on " + voice + ", but no clips found");
                    return;
                }

                int index = Random.Range(0, clips.Count);
                voice.clip = clips[index];
                voice.Play();

                if (doNotRepeatClips)
                {
                    clips.RemoveAt(index);
                }
            }

            public void Stop(AudioSource voice)
            {
                voice.Stop();
            }
        }
    }


}
