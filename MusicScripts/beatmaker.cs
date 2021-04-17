using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[System.Serializable]
public class BeatEvent : UnityEvent<int> { } //arg1 = beat#


public class beatmaker : MonoBehaviour
{
    [Range(30, 360)] public float globalTempo = 60;     // frequency of global beat / beat0 / start of bar
    [Min(1)] public int subdivisions = 4;               // number of off-beats per bar


    [Header("Count")]
    [SerializeField] int beat;
    [SerializeField] public int beatCount;
    [SerializeField] public int barCount;

    public BeatEvent SendBeat;

    private void Start()
    {
        StartCoroutine(Metronome());
        beat = -1;

        

        
    }


    IEnumerator Metronome()
    {
        while (true)
        {
            float waitTime = 60 / globalTempo / subdivisions;            
            yield return new WaitForSeconds(waitTime);

            beat = (beat + 1) % subdivisions;

            SendBeat.Invoke(beat);           

            if (beat == 0)            
                barCount++;

            beatCount++;

            

        }
    }




    private void OnDestroy()
    {
        StopAllCoroutines();
    }

}


