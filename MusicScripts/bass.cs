using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bass : MonoBehaviour
{
    List<AudioSource> voices = new List<AudioSource>();
    public AudioClip[] notes;

    public int numVoices = 1;
    public float noteVolume = 1f;
    public float notePitch = 0.5f;

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
            GameObject newVoice = new GameObject("voice_" + i);
            newVoice.transform.parent = this.gameObject.transform;

            var addedAudioSourceComponent = newVoice.AddComponent<AudioSource>();
            voices.Add(addedAudioSourceComponent);
        }
    }


    public void Bang(int beat)
    {
        if (beat == 0)
        {
            if (Random.Range(0f, 1f) < triggerChance)
            {
                PlayNote();
            }


           
        }

    }

    void PlayNote()
    {
        int index = Random.Range(0, notes.Length);       

        for (int i = 0; i < numVoices; ++i)
        {
            voices[i].pitch = notePitch;
            voices[i].PlayOneShot(notes[index], noteVolume / numVoices);
        }

    }

}
