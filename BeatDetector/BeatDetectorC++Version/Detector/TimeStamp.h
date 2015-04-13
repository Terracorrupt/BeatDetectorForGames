#ifndef TIMESTAMP
#define TIMESTAMP

class TimeStamp
{
public:
	TimeStamp();
	TimeStamp(int,int,int);
	TimeStamp(int, int, int, float);
	TimeStamp(int, int, int, char*);
	~TimeStamp();

	void setTime(int, int, int);
	void setSeconds(int);
	void setMinutes(int);
	void setMilliseconds(int);
	void setFrequency(float);

	int getSeconds();
	int getMinutes();
	int getMilliseconds();
	float getFrequency();
	char* getMetaData();

	void setTimeWithMetaData(int, int, int, char*);
	void setTimeWithFrequency(int, int, int, float);
private:
	int minutes;
	int seconds;
	int milliseconds;
	char* metaData;
	float beatFrequency;
};

#endif