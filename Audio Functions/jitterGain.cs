using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies some noise to an attached outputGain script.
/// </summary>

public class JitterGain : MonoBehaviour
{
    private OutputGain gainScript;
    private float startGain;
    [SerializeField] float jitterAmount;
    [SerializeField] float rampTime = 0.5f;

    private float vel;
    
    void Start()
    {
        gainScript = GetComponent<OutputGain>();
        startGain = gainScript.gain;
    }

    void Update()
    {        
        float newGain = startGain + Random.Range(-jitterAmount, jitterAmount);
        newGain = Mathf.SmoothDamp(startGain, newGain, ref vel, rampTime);

        gainScript.gain = newGain;
        gainScript.UpdateVolume();
    }
}
