using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arp : MonoBehaviour
{
    List<AudioSource> voices = new List<AudioSource>();

    public List<AudioClip> notes;

    //public AudioClip[] notes;
    public int numVoices = 1;
    public float noteVolume = 1f;
    public float notePitch = 1f;

    [Range(0f, 1f)] public float distort = 0f;

    /*[Range(0f, 0.5f)] public float detuneRange = 0.01f;
    [Range(0f, 0.99f)]  public float detimeRange = 0.01f;*/

    public bool arpeggiator = false;
    int seq = 0;
    [Range(0f, 1f)] public float triggerChance = 1f;



    private void Start()
    {
        voiceInit();

        if (notes.Count == 0)
            Debug.LogError("add some samples to play");
       
    }

    void voiceInit()
    {
        for (int i = 0; i < numVoices; ++i)
        {
            GameObject newVoiceGO = new GameObject("voice_" + i);
            newVoiceGO.transform.parent = this.gameObject.transform;           

            var addedAudioSourceComponent = newVoiceGO.AddComponent<AudioSource>();
            addedAudioSourceComponent.outputAudioMixerGroup = AudioUtility.GetMixerGroup("Music");
            addedAudioSourceComponent.volume = 1f;

            voices.Add(addedAudioSourceComponent);
        }

        var audioClipsInFolder = Resources.LoadAll<AudioClip>("arp_music");
        for (int i = 0; i < audioClipsInFolder.Length; ++i)
        {
            notes.Add(audioClipsInFolder[i]);
        }

    }



    public void BangOnTheBeat(int beat)
    {
        // Chance to trigger on every beat.
        if (Random.Range(0f, 1f) < triggerChance)
        {
            StartCoroutine(AddRhythmVariation(beat));
        }
    }

    public void BangOnTheBar(int beat)
    {
        // Chance to trigger on every bar (beat count x subdivisions)
        if (beat == 0)
        {
            if (Random.Range(0f, 1f) < triggerChance)
            {
                PlaySingleNote();
            }
        }
    }


    IEnumerator AddRhythmVariation(int beat)
    {
        // adds a bit of random offset
        float swing = Random.Range(0, distort);
        yield return new WaitForSeconds(swing);

        if (arpeggiator)
        {
            if (beat == 0)
            {
                seq = Random.Range(0, notes.Count);    // sequence order resets each bar
            }
            PlaySequence(beat);
        }
        else
        {
            PlayChord();
        }
    }



    void PlaySingleNote()
    {
        Debug.Log(this.gameObject.name + ": single note playing");

        // Plays a single note without offset.
        int index = Random.Range(0, notes.Count);

        for (int i = 0; i < numVoices; ++i)
        {
            voices[i].pitch = notePitch;
            voices[i].PlayOneShot(notes[index], noteVolume / numVoices);
        }

    }

    void PlaySequence(int beat)
    {
        Debug.Log(this.gameObject.name + ": sequence playing");

        // play notes in sequence (mono)
        int nl = notes.Count;
        int voiceIndex = beat % numVoices;
        int noteIndex = Random.Range(0, nl);           

        voices[voiceIndex].pitch = notePitch * (Random.Range(1 - distort, 1 + distort));
        voices[voiceIndex].PlayOneShot(notes[noteIndex], noteVolume);

        seq = (seq + 1) % nl;
    }


    void PlayChord()
    {
        Debug.Log(this.gameObject.name + ": chord playing");
        for (int i = 0; i < numVoices; ++i)
        {
            int noteIndex = Random.Range(1, notes.Count);
            voices[i].pitch = notePitch * (Random.Range(1 - distort, 1 + distort));
            voices[i].PlayOneShot(notes[noteIndex], noteVolume / numVoices);
        }

    }





}
