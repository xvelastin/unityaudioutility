using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for looping stems of larger ambiences.
/// </summary>
public class DistributeAudioObjects : MonoBehaviour
{
    public AudioClip clipToDistribute;
    public GameObject audioObjectPrefab;
    public GameObject dropArea;
    public int numberOfSoundsToDistribute;
    public bool playOnCreate;
    public List<GameObject> createdAudioObjects = new List<GameObject>();

    private Vector3 dropAreaSize;
    private string originalName;

    /// <summary>
    /// Distributes the audio object prefabs in the area defined by the Drop Area object's size.
    /// </summary>
    public void DistributeSounds()
    {
        // Size is defined by the scale values - intended so the Drop Area can use a Box Collider to visualise the area.
        dropAreaSize = dropArea.transform.localScale;

        for (int i = 0; i < numberOfSoundsToDistribute; ++i)
        {
            // Choose a random point inside the drop area.
            Vector3 randomPoint = dropArea.transform.position +
                                    new Vector3(Random.Range(-dropAreaSize.x / 2, dropAreaSize.x / 2),
                                                Random.Range(-dropAreaSize.y / 2, dropAreaSize.y / 2),
                                                Random.Range(-dropAreaSize.z / 2, dropAreaSize.z / 2));

            GameObject newObj = Instantiate<GameObject>(audioObjectPrefab, this.transform, true);
            newObj.transform.position = randomPoint;
            newObj.name = this.gameObject.name + "-" + i;

            // Hold a reference to the created object in a List.
            createdAudioObjects.Add(newObj);

            // Assign the clip to the audiosource and randomise its starting position, to give variety.
            AudioSource source = newObj.GetComponent<AudioSource>();
            source.clip = clipToDistribute;
            source.time = (source.clip.length / source.pitch) * Random.Range(0, 1);

            if (playOnCreate)
            {
                source.Play();
            }
        }
        // Renames this object to mark how many children it has, stores old name in case of ClearList.
        originalName = gameObject.name;
        gameObject.name = gameObject.name + "(x" + numberOfSoundsToDistribute + ")";
    }

    /// <summary>
    /// Destroys the created objects and resets name to what it was before DistributeSounds was called.
    /// </summary>
    public void ClearList()
    {
        foreach (GameObject obj in createdAudioObjects)
        {
            DestroyImmediate(obj);
        }
        createdAudioObjects = new List<GameObject>();
        gameObject.name = originalName;
    }
}
