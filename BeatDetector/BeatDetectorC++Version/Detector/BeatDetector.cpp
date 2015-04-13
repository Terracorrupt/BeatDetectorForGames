/*
	
	Beat Detector for Games Design - Pádraig O Connor
	Final Year Project: Games Design and Development (LIT-Tipperary)
	Project Commenced: Nov 4th 2014
	Project Concluded: Mar 26th 2015

	A program that, given a music file, will detect areas of high-frequency
	in real time. These areas often manifest as Beats in a song, such as a Drum Snare
	or Bass line.

	This program was developed with Video Games in mind, and is akin to the operations
	that occur in games such as "Vib Ribbon", "Audiosurf" and "Beat Hazzard". Please be
	aware that this detector is not 100% accurate, due to the nature of music itself aswell
	as parts of this program that could be improved on.

*/

#include "BeatDetector.h"
#include "LoadoutManager.h"

BeatDetector* BeatDetector::instance = 0;


//Call this function to create the "System" object that the detector will use
//throughout its lifetime. Should only be called once per instance.
void BeatDetector::loadSystem()
{
	system = fmodSetup();
}

//Loads a song into memory given a sample size and file-path to an audio file.
//The most commonly used and accurate Sample Size is 1024.
void BeatDetector::LoadSong(int sSize, char* audioString)
{
	//Take in Aruguments
	sampleSize = sSize;
	songString = audioString;

	areWePlaying = true;
	specFlux = 0.0f;
	timeBetween = 0;
	initialTime = clock();
	currentTime = 0;
	currentSeconds = 0;
	lastSeconds = 0;
	currentMillis = 0;
	currentMinutes = 0;
	median = 0.0f;
	smoothMedian = 0.0f;
	beatThreshold = 0.6f;
	thresholdSmoother = 0.6f;
	started = false;
	lastBeatRegistered = new TimeStamp();
	timeToDelay = 0;

	previousFFT = new float[sampleSize / 2 + 1];
	for (int i = 0; i < sampleSize / 2; i++)
	{
		previousFFT[i] = 0;
	}

	//Brute force for testing
	//songString = "Music/drums.wav";

	//Create channel and audio
	FMODErrorCheck(system->createChannelGroup(NULL, &channelMusic));
	FMODErrorCheck(system->createStream(songString, FMOD_SOFTWARE, 0, &audio));

	audio->getLength(&seconds, FMOD_TIMEUNIT_MS);
	audio->getDefaults(&sampleRate, 0, 0, 0);
	seconds = ((seconds + 500) / 1000);
	minutes = seconds / 60;
	fullSeconds = seconds;
	seconds = seconds - (minutes * 60);

	FMODErrorCheck(system->playSound(FMOD_CHANNEL_FREE, audio, true, &songChannel1));

	hzRange = (sampleRate / 2) / static_cast<float>(sampleSize);
	songChannel1->setChannelGroup(channelMusic);
	songChannel1->setPaused(true);

	std::cout << "Song Length: " << minutes << ":" << seconds << std::endl;
	std::cout << "Sample Rate: " << sampleRate << std::endl;
	std::cout << "Freq Range: " << hzRange << std::endl;
	//songChannel1->setVolume(0);

}


//Deconstructor
BeatDetector::~BeatDetector()
{
	delete system;
}


//This function is used to add a delay in the detection to playback ratio.
//For example, if an obstacle is spawned to the music, it will be spawned immediately
//upon the song detecting a beat, when oftentimes we want to line up that obstacle
//with the point in the music it plays. So, the obstacle will spawn before the song gets
//to the beat detected point.
//Use milliseconds to express the amount of delay time you want between playback and detection.
void BeatDetector::loadSongToDelay(int milliseconds)
{
	delayedSong = true;
	songChannel1->setVolume(0);

	FMODErrorCheck(system->createStream(songString, FMOD_SOFTWARE, 0, &audio2));

	//songChannel2 = new FMOD.Channel();
	FMODErrorCheck(system->playSound(FMOD_CHANNEL_FREE, audio2, true, &songChannel2));
	songChannel2->setChannelGroup(channelMusic);
	timeToDelay = milliseconds;
}

//Updates the timer and creates a "TimeStamp" object. This is used to detect where in the song
//we are, so timekeeping is a necessity. 
void BeatDetector::updateTime()
{
	currentTime = clock();
	currentTime = currentTime - initialTime;
	

	if (currentMinutes > 0)
		currentSeconds = ((currentTime / 1000) - (60 * currentMinutes));
	else
		currentSeconds = (currentTime / 1000);

	if (currentSeconds != lastSeconds)
	{
		currentMillis = 0;
		lastSeconds = currentSeconds;
	}
	else
	{
		currentMillis++;
	}

	currentMinutes = ((currentTime / 1000) / 60);

	if (timeToDelay != 0)
	{
		if (currentTime>timeToDelay)
		{
			//songChannel2.setChannelGroup(channelMusic);
			songChannel2->setPaused(false);
			timeToDelay = 0;
		}
	}

	currentTimeStamp = new TimeStamp(currentMinutes, currentSeconds, currentMillis);
}


//Gets the current frequency spectrum for the current frame of playback. This is gotten for both left
//and right channels and then combined into one channel called tempSpec, which the function returns
float* BeatDetector::getCurrentSpectrum()
{
	float *specLeft, *specRight, *tempSpec;
	specLeft = new float[sampleSize];
	specRight = new float[sampleSize];
	tempSpec = new float[sampleSize / 2 + 1];

	//Get Spectrum of Song Channel for left and right
	songChannel1->getSpectrum(specLeft, sampleSize, 0, FMOD_DSP_FFT_WINDOW_HAMMING);
	songChannel1->getSpectrum(specRight, sampleSize, 1, FMOD_DSP_FFT_WINDOW_HAMMING);

	//Average spectrum for stereo song channel, Divided by 2 cause Nyquist
	for (int i = 0; i < sampleSize / 2; i++)
	{
		tempSpec[i] = (specLeft[i] + specRight[i]);
		//std::cout << specStereo[i] << std::endl;
	}

	delete[] specLeft;
	delete[] specRight;

	return tempSpec;
}

//This function calculates a Spectral Flux based
//on the current and previous Spectrum data. This spectral flux is added to a list
//so that a threshold can be calculated, by taking the median of spectral fluxes
//in the list (after being sorted). To help out the detector I also included a
//smoother list that calculates a smoothing average based on beats detected.
//This function therefore maintains an adaptive threshold and returns the
//current threshold this tick/frame.
float BeatDetector::calculateFluxAndSmoothing(float* currentSpectrum)
{
	specFlux = 0.0;

	//Calculate differences
	for (int i = 0; i < sampleSize / 2; i++)
	{
		difference = currentSpectrum[i] - previousFFT[i];

		if (difference > 0) {
			specFlux += difference;
		}
	}

	//Get our median for threshold
	if (spectrumFluxes.size() > 0 && spectrumFluxes.size() < 10)
	{
		std::sort(spectrumFluxes.begin(), spectrumFluxes.end());
		std::sort(smootherValues.begin(), smootherValues.end());

		if (spectrumFluxes.at(spectrumFluxes.size() / 2) > 0)
		{
			median = spectrumFluxes.at(spectrumFluxes.size() / 2);
		}

		if (smootherValues.size() > 0 && smootherValues.size() < 5)
		{

			if (smootherValues.at(smootherValues.size() / 2) > 0)
			{
				smoothMedian = smootherValues.at(smootherValues.size() / 2);
			}
		}
		//std::cout << median << std::endl;
	}

	for (int i = 0; i < sampleSize / 2; i++)
	{
		spectrumFluxes.push_back(specFlux);

		if (spectrumFluxes.size() >= 10)
		{
			spectrumFluxes.erase(spectrumFluxes.begin());
		}
	}

	//Copy spectrum for next spectral flux calculation
	for (int j = 0; j < sampleSize / 2; j++)
		previousFFT[j] = currentSpectrum[j];

	//Smoothing for different averages
	if (smoothMedian > 1)
		thresholdSmoother = 0.8f;
	if (smoothMedian > 2 && smoothMedian < 4)
		thresholdSmoother = 1.0f;
	if (smoothMedian > 4 && smoothMedian < 6)
		thresholdSmoother = 2.2f;
	if (smoothMedian > 6)
		thresholdSmoother = 2.4f;

	return thresholdSmoother + median;
}


//This function should be called every tick/frame. This used the previous functions to
//Update time, get the current spectrum and the adaptive threshold and then does a check
//to see if a beat has occured. It also allows for some post-detection ignore clause.
//This function also does functions such as update the smoothing median list, create a
//timestamp object, update the lastBeatRegistered and checks to see if the song
//is still playing
void BeatDetector::update()
{
		
		if (started)
		{
			float* specStereo;

			updateTime();

			specStereo = getCurrentSpectrum();

			beatThreshold = calculateFluxAndSmoothing(specStereo);

			//Beat detected
			if (specFlux > beatThreshold && (clock() - timeBetween) > 350)
			{
				smootherValues.push_back(specFlux);

				if (smootherValues.size() >= 5)
				{
					smootherValues.erase(smootherValues.begin());
				}

				timeBetween = clock();

				TimeStamp* t = new TimeStamp(currentMinutes, currentSeconds, currentMillis, specFlux);
				std::cout << "BEAT AT: " << t->getMinutes() << ":" << t->getSeconds() << ":" << t->getMilliseconds() << " -- BEAT FREQ: " << t->getFrequency() << " -- THRESHOLD: " << beatThreshold << std::endl;
				lastBeatRegistered = t;
			}
			else if ((clock() - timeBetween) > 5000)
			{
				if (thresholdSmoother>0.4f)
					thresholdSmoother -= 0.4f;

				timeBetween = clock();
			}

			if (!delayedSong)
				songChannel1->isPlaying(&areWePlaying);
			else
				songChannel2->isPlaying(&areWePlaying);

			delete[] specStereo;
		}
		else
		{
			std::cin >> test;

			if (test == 1)
				setStarted(true);
		}
		
}


//This function is called by the loadSystem function above. It sets up FMOD for the rest of
//the program, like an "init" of sorts. Most of this code is boilerplate that is used in
//every FMOD application.
FMOD::System * BeatDetector::fmodSetup()
{
	FMOD::System *system;
	FMOD_RESULT result;
	unsigned int version;
	int numDrivers;
	FMOD_SPEAKERMODE speakerMode;
	FMOD_CAPS caps;
	char name[256];

	// Create FMOD interface object
	result = FMOD::System_Create(&system);
	FMODErrorCheck(result);

	// Check version
	result = system->getVersion(&version);
	FMODErrorCheck(result);

	if (version < FMOD_VERSION)
	{
		std::cout << "Error! You are using an old version of FMOD " << version << ". This program requires " << FMOD_VERSION << std::endl;    return 0;
	}

	//Check Sound Cards, if none, disable sound
	result = system->getNumDrivers(&numDrivers);
	FMODErrorCheck(result);

	if (numDrivers == 0)
	{
		result = system->setOutput(FMOD_OUTPUTTYPE_NOSOUND);
		FMODErrorCheck(result);
	}
	// At least one sound card
	else
	{
		// Get the capabilities of the default (0) sound card
		result = system->getDriverCaps(0, &caps, 0, &speakerMode);
		FMODErrorCheck(result);

		// Set the speaker mode to match that in Control Panel
		result = system->setSpeakerMode(speakerMode);
		FMODErrorCheck(result);

		// Increase buffer size if user has Acceleration slider set to off
		if (caps & FMOD_CAPS_HARDWARE_EMULATED)
		{
			result = system->setDSPBufferSize(1024, 10);
			FMODErrorCheck(result);
		}
		// Get name of driver
		result = system->getDriverInfo(0, name, 256, 0);
		FMODErrorCheck(result);

		// SigmaTel sound devices crackle for some reason if the format is PCM 16-bit.
		// PCM floating point output seems to solve it.
		if (strstr(name, "SigmaTel"))
		{
			result = system->setSoftwareFormat(48000, FMOD_SOUND_FORMAT_PCMFLOAT, 0, 0, FMOD_DSP_RESAMPLER_LINEAR);
			FMODErrorCheck(result);
		}
	}

	// Initialise FMOD
	result = system->init(100, FMOD_INIT_NORMAL, 0);

	// If the selected speaker mode isn't supported by this sound card, switch it back to stereo
	if (result == FMOD_ERR_OUTPUT_CREATEBUFFER)
	{
		result = system->setSpeakerMode(FMOD_SPEAKERMODE_STEREO);
		FMODErrorCheck(result);

		result = system->init(100, FMOD_INIT_NORMAL, 0);
	}

	FMODErrorCheck(result);

	return system;
}


//Simple error checking function that returns information about FMOD_RESULT objects
void BeatDetector::FMODErrorCheck(FMOD_RESULT result)
{
	if (result != FMOD_OK)
	{
		int meh;
		std::cout << "FMOD error! (" << result << ") " << FMOD_ErrorString(result) << std::endl;
		std::cin >> meh;
		exit(-1);
	}
}


//When a song is loaded it is initially paused, so this function
//should be called with "false" as it's argument to begin the playback.
void BeatDetector::setStarted(bool areWeStarted)
{
	started = areWeStarted;

	if (started)
		songChannel1->setPaused(false);
	else
		songChannel1->setPaused(true);
}


//Returns the last beat detected by the detection code. This function will be the main
//interface for gameplay programmers to tell when a beat has occured.
TimeStamp* BeatDetector::getLastBeat()
{
	return lastBeatRegistered;
}


//Checks if song is playing or not
bool BeatDetector::isPlaying()
{
	return areWePlaying;
}

//Returns the system object for use outside of this class
FMOD::System* BeatDetector::getSystem()
{
	return system;
}

//Returns the song name retrieved by the file name or title
char* BeatDetector::getSongName()
{
	audio->getName(songName, 50);

	return songName;
}

//Returns the song name retrieved by the tag "Artist".
//Some additional checks must be made here to ensure no
//garbage data is allowed through. If there is no data in the
//artist tag, garbage is returned, so I have coded around that.
char* BeatDetector::getArtistName()
{
	char* title;
	audio->getTag(NULL, 0, &tag);

	audio->getTag("ARTIST", 0, &tag);
	title = (char*)tag.data;


	if (title && strcmp(title, "major_brand") != 0 && stringValid(title))
		return title;
	else
		return "none";
}

//Ensures that a string is comprised of alphanumeric values and appropriate
//characters
bool BeatDetector::stringValid(const std::string &str)
{
	return find_if(str.begin(), str.end(),
		[](char c) { return !(isalnum(c) || (c == ' ')); }) == str.end();
}


//Returns the current time in seconds the song has reached
int BeatDetector::getTime()
{
	//std::cout << "FullSecs: " << fullSeconds;
	return fullSeconds;
}

//Returns the current TimeStamp the song has reached
TimeStamp* BeatDetector::getCurrentTime()
{
	return currentTimeStamp;
}

//Returns the full length of the song that was loaded in
TimeStamp* BeatDetector::getSongLength()
{
	return new TimeStamp(minutes,seconds,0);
}
