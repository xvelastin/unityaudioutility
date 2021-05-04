using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourcePlayer : MonoBehaviour
{
    //[SerializeField] AudioClip[] clips;
    [SerializeField] List<AudioClip> clips = new List<AudioClip>();

    [SerializeField] bool loop;
    [SerializeField] bool clipPlaying;
    [SerializeField] float intervalBetweenPlays;
    [SerializeField] [Range(-4f, 4f)] float pitch = 1f;


    private AudioSource source;


    private void Start()
    {
        source = GetComponent<AudioSource>();

        if (clips.Count == 0)
            clips.Add(source.clip);
        else source.clip = AudioUtility.RandomClipFromList(clips);


        if (loop)
            PlayLoopWithInterval();
        

    }


    public void PlayLoopWithInterval()
    {
        loop = true;
        LoopClip(intervalBetweenPlays);
    }
    public void PlayLoopWithInterval(float interval)
    {
        loop = true;
        LoopClip(interval);
    }


    void LoopClip(float interval)
    {
        source.pitch = pitch;
        StartCoroutine(ClipLooper(source, AudioUtility.RandomClipFromList(clips), interval));
    }


    IEnumerator ClipLooper(AudioSource src, AudioClip clip, float interval)
    {
        while (true)
        {    
            if (!clipPlaying)
            {
                StartCoroutine(WaitIntervalThenPlay(src, clip, interval));
                clipPlaying = true;
            }
            yield return null;
        }
       
    }

    public IEnumerator WaitIntervalThenPlay(AudioSource src, AudioClip clip, float interval)
    {      
        interval += src.clip.length;
        //Debug.Log("playing clip " + clip + "at object " + src.gameObject.name + ".. waiting " + interval + "seconds");

        yield return new WaitForSeconds(interval);
        clipPlaying = true;
        src.clip = clip;
        src.Play();

        yield return new WaitForSeconds(src.clip.length);
        clipPlaying = false;
        yield return null;
        
        
    }




}
