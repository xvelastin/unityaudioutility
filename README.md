# Unity Audio Utility

Constantly growing collection of c# scripts for Unity audio implementation.

## Audio Function Scripts

**[AudioSourceController](AudioSourceController/AudioSourceController.cs):** An all-in-one script that extends Unity's built-in Audio Source component designed with solo devs and game jammers in mind. Handles various advanced playback techniques including looping multiple clips, lots of randomisation, highly customisable fades and callbacks.

**[AudioUtility / DevUtils](Utility/AudioUtility.cs):** a stanley knife of useful tools for audio, like decibel conversion and creating fade curves.

**[DevUtils](Utility/DevUtils.cs)** Some common functions I find I use a lot in implementation like range mapping and parameter modulation, and an in-progress audio logger.


**[DialoguePlayback](DialoguePlayback/DialoguePlayback.cs):** Provides an interface for playback of dialogue lines with lists of Speaking Characters, Lines and Clips. Clips grouped into lines can be triggered randomly with or without repetition. Trigger lines by passing either the <int> index or the <string> name of the character and their line. Individual clips can be triggered directly with an additional argument.
  
**[XQ-Linear Show Control (external repo)](https://github.com/xvelastin/XQ-Linear-Sound-Control-for-Unity):** Controls and re-orders simple audio playback actions with intuitive re-orderable lists and custom inspectors.

## GameObject Scripts

**[ConstrainToRadius](ConstrainToRadius/ConstrainToRadius.cs):** Constrains a gameobject to a given radius around another gameobject while following a third gameobject. Created to confine the sounds of waves to the edges of a lake.

**[DistributeSoundObjects](DistributeAudioObjects/DistributeAudioObjects.cs):** Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for distributing individual stems of larger ambiences eg. bird audio objects in a forest area.
  
# Get In Touch
You can contact me on discord (blubberbaleen) for complaints, feedback and praise. If you'd like to work together on your next Unity project, you can find my portfolio over at my [website](https://www.xaviervelastin.com/gameaudio).  
  
  
# Screenshots
  Dialogue Playback
  
![Screenshot from the Unity Inspector for DialoguePlayback](/img/DialoguePlayback_Inspector.jpg)
  
  XQ-Linear Show Control
  
  ![Screenshot from the Unity Inspector for XQ-LSC](/img/xqprototype_qlistscreenshot.jpg)
  
  
  DistributeAudioObjects
  ![Screenshot from Unity for DistributeAudioObjects](/img/distributeaudioobjects.png)
