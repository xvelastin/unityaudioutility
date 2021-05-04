# unityaudioutility
Constantly growing collection of c# scripts for Unity audio implementation.
Unfinished and undocumented.


## Audio Function Scripts

**AudioSourceController:** An all-in-one controller for audio sources that amalgamates the audiosourcefader (for fades), gain (for db conversion) and audiosourceplayer (for multi-clip looping).

**AudioSourceFader:** Creates natural sounding fades that can be accessed without any scripting (Unity Events). Attach to a gameobject in a scene with an audiosource component. Fade curve editable in inspector or with an argument (c# calls only). Created for Limbik Theatre's audio play Pangea.

**AudioUtility:** a stanley knife of useful tools.

**fadeInOnAwake:** very simple script, does what it says.

**outputGain & jitterGain:** outputGain takes over an audiosource's volume control, letting you control it in decibels (loudness) for more precise control. The jitterGain script applies a smoothed randomness to outputGain's output, for some reason.


## Music Scripts

**arp**: Sample-based sequencer driven by the beatmaker.cs script

**beatmaker:** Creates bangs based on tempo + subdivisions.


## GameObject Scripts

**ConstrainToRadius:** Constrains a gameobject to a given radius around another gameobject while following a third gameobject. Created to confine the sounds of waves to the edges of a lake.

**DistributeSoundObjects:** Randomly distributes prefabs in a given dropArea gameobject (takes scale values of a reference gameobject, eg. with a collider). Intended for distributing individual stems of larger ambiences eg. bird audio objects in a forest area.
