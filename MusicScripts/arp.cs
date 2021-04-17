using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arp : MonoBehaviour
{
    //List<GameObject> voices_GO = new List<GameObject>();
    List<AudioSource> voices = new List<AudioSource>();

    public AudioClip[] notes;
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

        if (notes.Length == 0)
            Debug.LogError("add some samples to play");
       
    }

    void voiceInit()
    {
        for (int i = 0; i < numVoices; ++i)
        {
            GameObject newVoiceGO = new GameObject("voice_" + i);
            newVoiceGO.transform.parent = this.gameObject.transform;           

            var addedAudioSourceComponent = newVoiceGO.AddComponent<AudioSource>();
            addedAudioSourceComponent.volume = 1f;

            voices.Add(addedAudioSourceComponent);
        }
    }

    public void Bang(int beat)
    {

        if (Random.Range(0f, 1f) < triggerChance)
        {
            StartCoroutine(AddRhythmVariation(beat));
        }


    }


    IEnumerator AddRhythmVariation(int beat)
    {
        float swing = Random.Range(0, distort);
        yield return new WaitForSeconds(swing);

        if (arpeggiator)
        {
            if (beat == 0)
            {
                seq = Random.Range(0, notes.Length);    // sequence order resets each bar
            }
            PlaySequence(beat);
        }
        else
        {
            PlayChord();
        }

       


    }

    void PlayChord()
    {
        for (int i = 0; i < numVoices; ++i)
        {
            int noteIndex = Random.Range(1, notes.Length);
            voices[i].pitch = notePitch * (Random.Range(1 - distort, 1 + distort));
            voices[i].PlayOneShot(notes[noteIndex], noteVolume / numVoices);
        }

    }

    void PlaySequence(int beat)
    {
        // play notes in sequence (mono)

        
        int nl = notes.Length;
        int voiceIndex = beat % numVoices;
        int noteIndex = Random.Range(0, nl);
            

        voices[voiceIndex].pitch = notePitch * (Random.Range(1 - distort, 1 + distort));
        voices[voiceIndex].PlayOneShot(notes[noteIndex], noteVolume);

        seq = (seq + 1) % nl;

    }





}
