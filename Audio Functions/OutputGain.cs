using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]

public class OutputGain : MonoBehaviour
{
    [SerializeField] [Range(-100, 0)] public float gain = 0f;
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
        audioSource.volume = AudioUtility.ConvertDbtoA(gain);

    }

}
