using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for looping stems of larger ambiences.
/// </summary>

// nb: script is under construction, copy at your own risk.

public class DistributeAudioObjects : MonoBehaviour
{
    [SerializeField] AudioClip clipToDistribute;
    [SerializeField] int numberOfSoundsToDistribute;
    [SerializeField] GameObject audioObject;
    [SerializeField] GameObject dropArea;
    Vector3 dropAreaSize;

    [SerializeField] bool DistributeOnAwake = false;

    public List<GameObject> createdAudioObjects = new List<GameObject>();


    private void Start()
    {
        if (DistributeOnAwake) DistributeSounds();
    }

    public void DistributeSounds() 
    {
        dropAreaSize = dropArea.transform.localScale;        

        for (int i = 0; i < numberOfSoundsToDistribute; ++i)
        {
            Vector3 randPos = dropArea.transform.position + 
                new Vector3(Random.Range(-dropAreaSize.x / 2, dropAreaSize.x / 2),
                            Random.Range(-dropAreaSize.y / 2, dropAreaSize.y / 2),
                            Random.Range(-dropAreaSize.z / 2, dropAreaSize.z / 2));

            var go = Instantiate<GameObject>(audioObject, this.transform, true);
            go.transform.position = randPos;
            go.name = this.gameObject.name + "-" + i;

            createdAudioObjects.Add(go);

            var audiosource = go.GetComponent<AudioSource>();
            audiosource.clip = clipToDistribute;                 
            audiosource.time = (audiosource.clip.length / audiosource.pitch) * Random.Range(0, 1);
        }

        this.gameObject.name = this.gameObject.name + "(x" + numberOfSoundsToDistribute + ")";
    }
}
