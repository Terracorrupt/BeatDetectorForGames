Beat Detector C++ Version

INSTRUCTIONS:

1 - Create your new C++ Project
2 - Copy over the .cpp and .h files to their appropriate places in the solution explorer
3 - Place "fmodex.dll" into your applications .exe folder (Usually Debug or Release) aswell as your class folder.
4- Open your Project Properties and perform the following:
   -Configuration Properties -> C/C++: Under "Additional        Include Directories add "..\FMOD\inc"
   -Configuration Properties -> Linker -> General: Under        "Additional Library Directories" add "..\FMOD\lib"
   -Configuration Properties -> Linker -> Input: Under         "Additional Dependencies" add "fmodex_vc.lib"

How to use:

First remember to #include "BeatDetector.h" to whatever class you want to use the class.

Refernce an instance of the Beat Detector with BeatDetector::Instance();

Load the system with "BeatDetector::Instance()->loadSystem();"

Load a song with "BeatDetector::Instance()->LoadSong(sampleSize, filePath);"
Where the sampleSize is how many samples the beat detector will pull per tick, and the filepath referencing the song to load in (preferably a .mp3 or .wav).
For most song's you'll want the sampleSize to be 1024.


Songs by default are paused, so "detector.setStarted(true);" is necessary for the song to begin playing.

Finally, call "BeatDetector::Instance()->update()" for the detection to update per tick.

Each time a beat has occured in the detector, the "lastBeatRegistered" TimeStamp object will be updated. By simply checking to see if this field gets updated, the application programmer can check accurately when a Beat has occured. The "lastBeatRegistered" object contains information pertaining to where in the song the beat occured, for further accuracy.

For example:

"if(localLastBeatOccured != BeatDetector::Instance()->getLastBeat())
{
	//Beat Occured

	//DO SOMETHING

	//Update localLastBeat
	localLastBeatOccured = BeatDetector::Instance()->getLastBeat();
}	

