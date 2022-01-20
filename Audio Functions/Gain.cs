using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple gain stage for audio source processing. Set input gain from scripts with SetGain(), use output gain to set the maximum volume.
/// </summary>



public class Gain : MonoBehaviour
{
    [SerializeField] [Range(-100, 24)] float outputGain = 0f;
    [SerializeField] [Range(-100, 24)] float inputGain = 0f;    
    [SerializeField] AudioSource audioSource;
    
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        
    }

    public void SetGain(float value)
    {
        inputGain = value;
        audioSource.volume = AudioUtility.ConvertDbtoAmplitude(outputGain + inputGain);
    }


}
