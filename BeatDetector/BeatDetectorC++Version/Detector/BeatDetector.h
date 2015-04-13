#ifndef BEAT_DETECTOR
#define BEAT_DETECTOR

#include <algorithm>
#include <queue>
#include <time.h>
#include <array>
#include <math.h>
#include "fmod.hpp"
#include "fmod_errors.h"
#include "TimeStamp.h"
#include <iostream>

class BeatDetector
{
public:
	//BeatDetector(int, char*);
	void LoadSong(int, char*);
	void loadSystem();
	~BeatDetector();
	void updateTime();
	float* getCurrentSpectrum();
	float calculateFluxAndSmoothing(float*);
	void update();
	FMOD::System * fmodSetup();
	void FMODErrorCheck(FMOD_RESULT);
	void setStarted(bool);
	TimeStamp* getLastBeat();
	bool isPlaying();
	char* getSongName();
	char* getArtistName();
	bool stringValid(const std::string &str);
	int getTime();
	void loadSongToDelay(int milliseconds);
	TimeStamp* getCurrentTime();
	TimeStamp* getSongLength();
	FMOD::System* getSystem();


	//Singelton
	static BeatDetector* Instance()
	{
		if (instance == 0)
		{
			instance = new BeatDetector();
		}
		return instance;
	}
	
private:
	BeatDetector(){};
	static BeatDetector* instance;
	FMOD::System *system;
	FMOD_RESULT result;
	int sampleSize;
	int test;
	int fullSeconds;
	float sampleRate;
	unsigned int seconds;
	unsigned int minutes;
	bool areWePlaying;
	float* previousFFT;
	float specFlux;
	float difference;
	unsigned int timeBetween;
	const char* songString;
	bool started;
	TimeStamp* lastBeatRegistered;
	TimeStamp* totalSongTime;
	char songName [50];
	char* artistName;
	FMOD_TAG tag;
	TimeStamp* currentTimeStamp;

	FMOD::Sound *audio;
	FMOD::Sound *audio2;
	FMOD::ChannelGroup *channelMusic;
	FMOD::Channel *songChannel1;
	FMOD::Channel *songChannel2;
	bool delayedSong = false;
	int timeToDelay;


	int initialTime;
	int currentTime;
	int currentMillis;
	int currentSeconds;
	int lastSeconds;
	int currentMinutes;

	clock_t t1, t2;

	float hzRange;

	std::vector<float> spectrumFluxes;
	std::vector<float> smootherValues;
	float median;
	float smoothMedian;
	float beatThreshold;
	float thresholdSmoother;
};

#endif