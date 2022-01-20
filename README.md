# Unity Audio Utility

Constantly growing collection of c# scripts for Unity audio implementation.

## Audio Function Scripts

**[AudioSourceController](Audio%20Functions/AudioSourceController.cs)** An all-in-one controller for audio sources that does: precise curve-based fading, decibel range volume editing and multi-clip playback with randomisation and pitch/volume modulation.

**[AudioUtility](Audio%20Functions/AudioUtility.cs):** a stanley knife of useful tools, most of my other scripts call it.

**[DialoguePlayback](Audio%20Functions/DialoguePlayback.cs):** Provides an interface for playback of dialogue lines with lists of Speaking Characters, Lines and Clips. Clips grouped into lines can be triggered randomly with or without repetition. Trigger lines by passing either the <int> index or the <string> name of the character and their line. Individual clips can be triggered directly with an additional argument.

## GameObject Scripts

**[ConstrainToRadius](GameObject%20Control/ConstrainToRadius.cs):** Constrains a gameobject to a given radius around another gameobject while following a third gameobject. Created to confine the sounds of waves to the edges of a lake.

**[DistributeSoundObjects](GameObject%20Control/DistributeAudioObjects.cs):** Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for distributing individual stems of larger ambiences eg. bird audio objects in a forest area.
  
# Get In Touch
You can contact me on discord (blubberbaleen#2086) for complaints, feedback and praise. If you'd like to work together on your next Unity project, you can find my portfolio over at my [website](https://www.xaviervelastin.com/gameaudio).  
  
  
# Screenshots
  Dialogue Playback
  
![Screenshot from the Unity Inspector for DialoguePlayback](/img/DialoguePlayback_Inspector.jpg)
  
  XQ-Linear Show Control
  
  ![Screenshot from the Unity Inspector for XQ-LSC](/img/xqprototype_qlistscreenshot.jpg)
  
  
  DistributeAudioObjects
  ![Screenshot from Unity for DistributeAudioObjects](/img/distributeaudioobjects.png)
