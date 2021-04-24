using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fadeInOnAwake : MonoBehaviour
{

    AudioSource audiosource;
    [SerializeField] float duration = 3f;


    private void Start()
    {
        audiosource = GetComponent<AudioSource>();
        float targetVol = audiosource.volume;
        audiosource.volume = 0f;
        StartCoroutine(StartFade(audiosource, 0f, targetVol, duration));


    }


    IEnumerator StartFade(AudioSource source, float startingVol, float targetVol, float duration)
    {
        yield return new WaitForSeconds(5);

        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startingVol, targetVol, currentTime / duration);

            yield return null;
        }

    }
}
