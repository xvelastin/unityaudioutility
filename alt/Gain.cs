using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gain stage for audio source processing. Inputgain is visible to scripting, output gain changes directly.
/// </summary>


public class Gain : MonoBehaviour
{
    [SerializeField] [Range(-100, 24)] float outputGain = 0f;
    [Range(-100, 0)] public float inputGain = 0f;    
    [SerializeField] AudioSource audioSource;
    
    // Start is called before the first frame update
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        
        UpdateVolume();
    }

    public void UpdateVolume()
    {
        audioSource.volume = AudioUtility.ConvertDbtoA(outputGain + inputGain);

    }

}
