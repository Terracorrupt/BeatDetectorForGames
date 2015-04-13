using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatDetectorCSharp
{
    public class TimeStamp
    {
        private int minutes;
        private int seconds;
        private int milliseconds;
        private string metaData;
        private float beatFrequency;

        public TimeStamp()
        {
            minutes = 0;
            seconds = 0;
            milliseconds = 0;
            metaData = "";
        }



        public TimeStamp(int m, int s, int mil)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
            metaData = "";
        }

        public TimeStamp(int m, int s, int mil, float f)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
            beatFrequency = f;
        }

        public TimeStamp(int m, int s, int mil, string md)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
            metaData = md;
        }

        public void setTime(int m, int s, int mil)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
        }

        public void setMinutes(int m)
        {
            minutes = m;
        }

        public void setSeconds(int s)
        {
            seconds = s;
        }

        public void setMilliseconds(int mil)
        {
            milliseconds = mil;
        }

        public void setFrequency(float f)
        {
            beatFrequency = f;
        }

        public void setTimeWithMetaData(int m, int s, int mil, string md)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
            metaData = md;
        }

        public void setTimeWithFrequency(int m, int s, int mil, float f)
        {
            minutes = m;
            seconds = s;
            milliseconds = mil;
            beatFrequency = f;
        }


        public int getMinutes()
        {
            return minutes;
        }

        public int getSeconds()
        {
            return seconds;
        }

        public int getMilliseconds()
        {
            return milliseconds;
        }

        public float getFrequency()
        {
            return beatFrequency;
        }

        public string getMetaData()
        {
            return metaData;
        }

    } 
}
