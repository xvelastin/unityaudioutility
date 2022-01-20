using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Straightforward amplitude to decibel converter to set attached audiosource volume in db.
/// </summary>

[ExecuteInEditMode]

public class OutputGain : MonoBehaviour
{
    [Range(-100, 0)] public float gain = 0f;
    [SerializeField] AudioSource audioSource;
    
    // Start is called before the first frame update
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnValidate()
    {
        UpdateVolume();
    }

    public void UpdateVolume()
    {
        audioSource.volume = AudioUtility.ConvertDbtoAmplitude(gain);
    }

}
