Beat Detector C# Version

INSTRUCTIONS:

1 - Create your new C# Project
2 - Copy over the .cs files in this folder to your solution explorer
3 - Place "fmodex.dll" into your applications .exe folder (Usually Debug or Release)
4 - Change the namespace in the BeatDetector and TimeStampl classes to your own namespace


How to use:

Create an instance of the BeatDetector by using the call
"BeatDetector detector = BeatDetector.Instance();" or just reference "BeatDetector.Instance() throughout.

Load the system with "detector.loadSystem();"

Load a song with "detector.LoadSong(sampleSize, filePath);" 
Where the sampleSize is how many samples the beat detector will pull per tick, and the filepath referencing the song to load in (preferably a .mp3 or .wav).
For most song's you'll want the sampleSize to be 1024.


Songs by default are paused, so "detector.setStarted(true);" is necessary for the song to begin playing.

Finally, call "detector.update()" for the detection to update per tick.

Each time a beat has occured in the detector, the "lastBeatRegistered" TimeStamp object will be updated. By simply checking to see if this field gets updated, the application programmer can check accurately when a Beat has occured. The "lastBeatRegistered" object contains information pertaining to where in the song the beat occured, for further accuracy.

For example:

"if(localLastBeatOccured != detector.getLastBeat())
{
	//Beat Occured

	//DO SOMETHING

	//Update localLastBeat
	localLastBeatOccured = detector.getLastBeat()
}	

