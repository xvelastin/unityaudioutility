using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandPitch : MonoBehaviour
{
    [SerializeField] [Range(-3, 3)] float pitchMin = 1f;
    [SerializeField] [Range(-3, 3)] float pitchMax = 1f;

    private void Start()
    {
        GetComponent<AudioSource>().pitch = Random.Range(pitchMin, pitchMax);
    }
}
