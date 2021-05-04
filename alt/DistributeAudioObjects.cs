using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for looping stems of larger ambiences.
/// </summary>

public class DistributeAudioObjects : MonoBehaviour
{
    [SerializeField] AudioClip clipToDistribute;
    [SerializeField] int numberOfSoundsToDistribute;
    [SerializeField] GameObject audioObject;
    [SerializeField] GameObject dropArea;
    Vector3 dropAreaSize;

    [SerializeField] [Range(-100, 0)] float createdObjectGain;
    [SerializeField] [Range(-4f, 4f)] float pitchOffset;
    [SerializeField] float randomPitchRange;

    [SerializeField] bool PlayOnAwake = false;

    public List<GameObject> createdAudioObjects = new List<GameObject>();


    private void Start()
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
            audiosource.pitch += pitchOffset + Random.Range(-randomPitchRange, randomPitchRange);                       
            audiosource.time = audiosource.clip.length * Random.Range(0, 1);

            //vol
            if (audiosource.GetComponent<AudioSourceFader>())            
                audiosource.GetComponent<AudioSourceFader>().outputGain = createdObjectGain;            
            else
                audiosource.volume = AudioUtility.ConvertDbtoA(createdObjectGain);

           

            if (PlayOnAwake)
                audiosource.Play();
        }

        this.gameObject.name = this.gameObject.name + "(x" + numberOfSoundsToDistribute + ")";

    }

}
