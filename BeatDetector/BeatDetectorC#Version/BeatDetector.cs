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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FMOD;

namespace BeatDetectorCSharp
{
    public class BeatDetector
    {
        private BeatDetector(){}
	    private static BeatDetector instance;

        public static BeatDetector Instance()
        {
            if (instance == null)
            {
                instance = new BeatDetector();
            }
            return instance;
        }

	    private FMOD.System system;
        private FMOD.RESULT result;
        private int sampleSize;
        private int test;
        private int fullSeconds;
        private float sampleRate;
        private uint seconds;
        private uint minutes;
        private bool areWePlaying;
        private float [] previousFFT;
        private float specFlux;
        private float difference;
        private uint timeBetween;
        private string songString;
        private bool started;
        private TimeStamp lastBeatRegistered;
        private TimeStamp totalSongTime;
        private string songName;
        private string artistName;

        private FMOD.TAG tag;
        private TimeStamp currentTimeStamp;

        private FMOD.Sound audio;
        private FMOD.Sound audio2;
        private FMOD.ChannelGroup channelMusic;
        private FMOD.Channel songChannel1;
        private FMOD.Channel songChannel2;
        bool delayedSong = false;



        private int initialTime;
        private int currentTime;
        private int currentMillis;
        private int currentSeconds;
        private int lastSeconds;
        private int zero = 0;
        private float zeroF = 0;
        private int currentMinutes;
        private int timeToDelay = 0;

	    //clock_t t1, t2;
        private Stopwatch stopW = new Stopwatch();
        TimeSpan time = new TimeSpan();
        string timeString;


        private float hzRange;

        private List<float> spectrumFluxes = new List<float>();
        private List<float> smootherValues = new List<float>();
        private float median;
        private float smoothMedian;
        private float beatThreshold;
        private float thresholdSmoother;


        //Simple error checking function that returns information about FMOD_RESULT objects
        private void FMODErrorCheck(FMOD.RESULT result)
        {
	        if (result != FMOD.RESULT.OK)
	        {
		        //int meh;
		        Console.WriteLine("FMOD error! (" + result + ") " + FMOD.Error.String(result));
                Console.ReadLine();
		        //exit(-1);
	        }
        }

        //This function is called by the loadSystem function. It sets up FMOD for the rest of
        //the program, like an "init" of sorts. Most of this code is boilerplate that is used in
        //every FMOD application.
        private FMOD.System fmodSetup()
        {
	        FMOD.System t_system = new FMOD.System();
            FMOD.RESULT result = new FMOD.RESULT();
            uint version = 0;
            int numDrivers = 0;
            FMOD.SPEAKERMODE speakerMode = FMOD.SPEAKERMODE.STEREO;
            FMOD.CAPS caps = FMOD.CAPS.NONE; 
            StringBuilder name = null;

	        // Create FMOD interface object
	        result = FMOD.Factory.System_Create(ref t_system);
	        FMODErrorCheck(result);

	        // Check version
            result = t_system.getVersion(ref version);
	        FMODErrorCheck(result);

	        if (version < FMOD.VERSION.number)
	        {
		        Console.WriteLine("Error! You are using an old version of FMOD " + version + ". This program requires " + FMOD.VERSION.number);    
                return null;
	        }

	        //Check Sound Cards, if none, disable sound
            result = t_system.getNumDrivers(ref numDrivers);
	        FMODErrorCheck(result);

	        if (numDrivers == 0)
	        {
                result = t_system.setOutput(FMOD.OUTPUTTYPE.NOSOUND);
		        FMODErrorCheck(result);
	        }
	        // At least one sound card
	        else
	        {
                
		        // Get the capabilities of the default (0) sound card
                result = t_system.getDriverCaps(0, ref caps, ref zero, ref speakerMode);
		        FMODErrorCheck(result);

		        // Set the speaker mode to match that in Control Panel
                result = t_system.setSpeakerMode(speakerMode);
		        FMODErrorCheck(result);

		        // Increase buffer size if user has Acceleration slider set to off
		        if (FMOD.CAPS.HARDWARE_EMULATED.Equals(true))
		        {
                    result = t_system.setDSPBufferSize(1024, 10);
			        FMODErrorCheck(result);
		        }
		        // Get name of driver
                FMOD.GUID temp = new FMOD.GUID();

                result = t_system.getDriverInfo(0, name, 256, ref temp);
		        FMODErrorCheck(result);
	        }
            System.IntPtr temp2 = new System.IntPtr();
	        // Initialise FMOD
            result = t_system.init(100, FMOD.INITFLAGS.NORMAL, temp2);

	        // If the selected speaker mode isn't supported by this sound card, switch it back to stereo
	        if (result == FMOD.RESULT.ERR_OUTPUT_CREATEBUFFER)
	        {
                result = t_system.setSpeakerMode(FMOD.SPEAKERMODE.STEREO);
		        FMODErrorCheck(result);

                result = t_system.init(100, FMOD.INITFLAGS.NORMAL, temp2);
	        }

	        FMODErrorCheck(result);

            return t_system;
        }


        //Call this function to create the "System" object that the detector will use
        //throughout its lifetime. Should only be called once per instance.
        public void loadSystem()
        {
	        system = fmodSetup();
        }


        //Loads a song into memory given a sample size and file-path to an audio file.
        //The most commonly used and accurate Sample Size is 1024.
        public void LoadSong(int sSize, string audioString)
        {
	        //Take in Aruguments
	        sampleSize = sSize;
	        songString = audioString;

	        stopW.Start();
	        areWePlaying = true;
	        specFlux = 0.0f;
	        timeBetween = 0;
            initialTime = (int)stopW.ElapsedMilliseconds;
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
            audio = new FMOD.Sound();
            songChannel1 = new FMOD.Channel();

            channelMusic = new FMOD.ChannelGroup();

	        previousFFT = new float[sampleSize 
                / 2 + 1];
	        for (int i = 0; i < sampleSize / 2; i++)
	        {
		        previousFFT[i] = 0;
	        }

	        //Brute force for testing
	        //songString = "Music/drums.wav";

	        //Create channel and audio
	        FMODErrorCheck(system.createChannelGroup(null, ref channelMusic));
           // CREATESOUNDEXINFO ex = new CREATESOUNDEXINFO();
            
	        FMODErrorCheck(system.createStream(songString, FMOD.MODE.SOFTWARE, ref audio));

	        audio.getLength(ref seconds, FMOD.TIMEUNIT.MS);
	        audio.getDefaults(ref sampleRate, ref zeroF, ref zeroF, ref zero);
	        seconds = ((seconds + 500) / 1000);
	        minutes = seconds / 60;
	        fullSeconds = (int)seconds;
	        seconds = seconds - (minutes * 60);

	        FMODErrorCheck(system.playSound(FMOD.CHANNELINDEX.FREE, audio, true, ref songChannel1));

	        //hzRange = (sampleRate / 2) / static_cast<float>(sampleSize);
	        songChannel1.setChannelGroup(channelMusic);
	        songChannel1.setPaused(true);

	        Console.WriteLine("Song Length: " + minutes + ":" + seconds);
	        Console.WriteLine("Sample Rate: " + sampleRate);
            
	        //std::cout << "Freq Range: " << hzRange << std::endl;
	        //songChannel1.setVolume(0);

        }


        //This function is used to add a delay in the detection to playback ratio.
        //For example, if an obstacle is spawned to the music, it will be spawned immediately
        //upon the song detecting a beat, when oftentimes we want to line up that obstacle
        //with the point in the music it plays. So, the obstacle will spawn before the song gets
        //to the beat detected point.
        //Use milliseconds to express the amount of delay time you want between playback and detection.
        public void loadSongToDelay(int milliseconds)
        {
            delayedSong = true;
            songChannel1.setVolume(0);

            audio2 = new FMOD.Sound();
            FMODErrorCheck(system.createStream(songString, FMOD.MODE.SOFTWARE, ref audio2));

            songChannel2 = new FMOD.Channel();
            FMODErrorCheck(system.playSound(FMOD.CHANNELINDEX.FREE, audio2, true, ref songChannel2));
            songChannel2.setChannelGroup(channelMusic);
            timeToDelay = milliseconds;
        }

        //Updates the timer and creates a "TimeStamp" object. This is used to detect where in the song
        //we are, so timekeeping is a necessity. 
        private void updateTime()
        {

            time = stopW.Elapsed;
            timeString = time.ToString();

            currentTime = (int)stopW.ElapsedMilliseconds;
            currentTime = currentTime - initialTime;

           // if (currentTime > 1000)
                //currentTime = 0;
            //Console.WriteLine(time.ToString("mm\\:ss\\.ff"));

            currentMillis = Int32.Parse(timeString.Substring(9, 2));
            currentSeconds = Int32.Parse(timeString.Substring(6, 2));
            currentMinutes = Int32.Parse(timeString.Substring(3, 2));

            if(timeToDelay!=0)
            {
                if(currentTime>timeToDelay)
                {
                    //songChannel2.setChannelGroup(channelMusic);
                    songChannel2.setPaused(false);
                    timeToDelay = 0;
                }
            }
                

            //if (currentMinutes > 0)
            //    currentSeconds = ((currentTime / 1000) - (60 * currentMinutes));
            //else
            //    currentSeconds = (currentTime / 1000);

            //if (currentSeconds != lastSeconds)
            //{
            //    currentMillis = 0;
            //    lastSeconds = currentSeconds;
            //}
            //else
            //{
            //    currentMillis++;

            //    if (currentMillis > 1000)
            //        currentMillis = 0;
            //}

            //currentMinutes = ((currentTime / 1000) / 60);

            currentTimeStamp = new TimeStamp(currentMinutes, currentSeconds, currentMillis);
        }

        //Gets the current frequency spectrum for the current frame of playback. This is gotten for both left
        //and right channels and then combined into one channel called tempSpec, which the function returns
        private float[] getCurrentSpectrum()
        {
	        float [] specLeft, specRight, tempSpec;
	        specLeft = new float[sampleSize];
	        specRight = new float[sampleSize];
	        tempSpec = new float[sampleSize / 2 + 1];

	        //Get Spectrum of Song Channel for left and right
	        FMODErrorCheck(songChannel1.getSpectrum(specLeft, sampleSize, 0, FMOD.DSP_FFT_WINDOW.HAMMING));
            FMODErrorCheck(songChannel1.getSpectrum(specRight, sampleSize, 1, FMOD.DSP_FFT_WINDOW.HAMMING));

	        //Average spectrum for stereo song channel, Divided by 2 cause Nyquist
	        for (int i = 0; i < sampleSize / 2; i++)
	        {
		        tempSpec[i] = (specLeft[i] + specRight[i]);
		        //std::cout << specStereo[i] << std::endl;
	        }

	        //delete[] specLeft;
	        //delete[] specRight;

	        return tempSpec;
        }

        //This function calculates a Spectral Flux based
        //on the current and previous Spectrum data. This spectral flux is added to a list
        //so that a threshold can be calculated, by taking the median of spectral fluxes
        //in the list (after being sorted). To help out the detector I also included a
        //smoother list that calculates a smoothing average based on beats detected.
        //This function therefore maintains an adaptive threshold and returns the
        //current threshold this tick/frame.
        private float calculateFluxAndSmoothing(float[] currentSpectrum)
        {

	        specFlux = 0.0f;

	        //Calculate differences
	        for (int i = 0; i < sampleSize / 2; i++)
	        {
		        difference = currentSpectrum[i] - previousFFT[i];

		        if (difference > 0) {
			        specFlux += difference;
		        }
	        }

	        //Get our median for threshold
	        if (spectrumFluxes.Count > 0 && spectrumFluxes.Count < 10)
	        {
                spectrumFluxes.Sort();
                smootherValues.Sort();
		        //std::sort(spectrumFluxes.begin(), spectrumFluxes.end());
		        //quickSort(spectrumFluxes, 0, spectrumFluxes.size() - 1);
		        //std::sort(smootherValues.begin(), smootherValues.end());

		        if (spectrumFluxes[spectrumFluxes.Count / 2] > 0)
		        {
                    median = spectrumFluxes[spectrumFluxes.Count / 2];
		        }

		        if (smootherValues.Count > 0 && smootherValues.Count < 5)
		        {

                    if (smootherValues[smootherValues.Count / 2] > 0)
			        {
                        smoothMedian = smootherValues[smootherValues.Count / 2];
			        }
		        }
		        //std::cout << median << std::endl;
	        }

	        for (int i = 0; i < sampleSize / 2; i++)
	        {
                if (spectrumFluxes.Count>1)
                    spectrumFluxes.Insert(spectrumFluxes.Count - 1, specFlux);
                else
                    spectrumFluxes.Insert(spectrumFluxes.Count, specFlux);

		        if (spectrumFluxes.Count >= 10)
		        {
			        spectrumFluxes.RemoveAt(0);
		        }
	        }

	        //Copy spectrum for next spectral flux calculation
	        for (int j = 0; j < sampleSize / 2; j++)
		        previousFFT[j] = currentSpectrum[j];

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
        public void update()
        {

           // Console.WriteLine(specFlux);

		        if (started)
		        {
			        float[] specStereo;

			        updateTime();

			        specStereo = getCurrentSpectrum();

                    //Console.WriteLine(specStereo);

			        beatThreshold = calculateFluxAndSmoothing(specStereo);

			        //Beat detected
                    if (specFlux > beatThreshold && ((uint) stopW.ElapsedMilliseconds - timeBetween) > 350)
			        {
                        if (smootherValues.Count > 1)
                            smootherValues.Insert(smootherValues.Count - 1, specFlux);
                        else
                            smootherValues.Insert(smootherValues.Count, specFlux);

				        if (smootherValues.Count >= 5)
				        {
					        smootherValues.Remove(0);
				        }

                        timeBetween = (uint)stopW.ElapsedMilliseconds;

				        TimeStamp t = new TimeStamp(currentMinutes, currentSeconds, currentMillis, specFlux);
				        Console.WriteLine("BEAT AT: " + t.getMinutes() + ":" + t.getSeconds() + ":" + t.getMilliseconds() + " -- BEAT FREQ: " + t.getFrequency() + " -- THRESHOLD: " + beatThreshold);
				        lastBeatRegistered = t;
			        }
                    else if (((uint) stopW.ElapsedMilliseconds - timeBetween) > 5000)
			        {
				        if (thresholdSmoother>0.4f)
					        thresholdSmoother -= 0.4f;

                        timeBetween = (uint)stopW.ElapsedMilliseconds;
			        }

                    if(!delayedSong)
			            songChannel1.isPlaying(ref areWePlaying);
                    else
                        songChannel2.isPlaying(ref areWePlaying);

			        //delete[] specStereo;
		        }
		        else
		        {
			        Console.ReadLine();    
			        if (test == 1)
				        setStarted(true);
		        }
		
        }

        //When a song is loaded it is initially paused, so this function
        //should be called with "false" as it's argument to begin the playback.
        public void setStarted(bool areWeStarted)
        {
	        started = areWeStarted;

	        if (started)
		        songChannel1.setPaused(false);
	        else
		        songChannel1.setPaused(true);
        }

        //Returns the current time in seconds the song has reached
        public int getTime()
        {
	        //std::cout << "FullSecs: " << fullSeconds;
	        return fullSeconds;
        }

        //Returns the current TimeStamp the song has reached
        public TimeStamp getCurrentTime()
        {
	        return currentTimeStamp;
        }

        //Returns the full length of the song that was loaded in
        public TimeStamp getSongLength()
        {
            return new TimeStamp((int)minutes, (int)seconds, 0);
        }

        //Returns the song name retrieved by the file name or title
        public StringBuilder getSongName()
        {
            StringBuilder tempS = new StringBuilder();
            tempS.Append(songName);

	        audio.getName(tempS, 50);

	        return tempS;
        }

        //Returns the song name retrieved by the tag "Artist".
        //Some additional checks must be made here to ensure no
        //garbage data is allowed through. If there is no data in the
        //artist tag, garbage is returned. Still have to code around
        //that for the C# version
        public string getArtistName()
        {
	        string title;
	        audio.getTag("", 0, ref tag);

	        audio.getTag("ARTIST", 0, ref tag);
	        title = tag.data.ToString();

            //if (title && strcmp(title, "major_brand") != 0 && stringValid(title))
            //    return title;
            //else
            //    return "none";

            return title;
        }

        //Returns the last beat detected by the detection code. This function will be the main
        //interface for gameplay programmers to tell when a beat has occured.
        public TimeStamp getLastBeat()
        {
	        return lastBeatRegistered;
        }

        //Checks if song is playing or not
        public bool isPlaying()
        {
	        return areWePlaying;
        }

        //Returns the system object for use outside of this class
        public FMOD.System getSystem()
        {
	        return system;
        }

    }
}
