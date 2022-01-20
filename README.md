# Unity Audio Utility

Constantly growing collection of c# scripts for Unity audio implementation.

## Audio Function Scripts

**AudioSourceController:** An all-in-one controller for audio sources that does: precise curve-based fading, decibel range volume editing and multi-clip playback with randomisation and pitch/volume modulation.

**AudioUtility:** a stanley knife of useful tools, most of my other scripts call it.

**DialoguePlayback:** Provides an interface for playback of dialogue lines with lists of Speaking Characters, Lines and Clips. Clips grouped into lines can be triggered randomly with or without repetition. Trigger lines by passing either the <int> index or the <string> name of the character and their line. Individual clips can be triggered directly with an additional argument.

**fadeInOnAwake:** very simple script, does what it says.

**outputGain & jitterGain:** outputGain takes over an audiosource's volume control, letting you control it in decibels (loudness) for more precise control. The jitterGain script applies a smoothed randomness to outputGain's output, for some reason.

## GameObject Scripts

**ConstrainToRadius:** Constrains a gameobject to a given radius around another gameobject while following a third gameobject. Created to confine the sounds of waves to the edges of a lake.

**DistributeSoundObjects:** Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for distributing individual stems of larger ambiences eg. bird audio objects in a forest area.
  
  
# Screenshots
  Dialogue Playback
  
![Screenshot from the Unity Inspector for DialoguePlayback](/img/DialoguePlayback_Inspector.jpg)
  
  XQ-Linear Show Control
  
  ![Screenshot from the Unity Inspector for XQ-LSC](/img/xqprototype_qlistscreenshot.jpg)
  
  
  DistributeAudioObjects
  ![Screenshot from Unity for DistributeAudioObjects](/img/distributeaudioobjects.png)
