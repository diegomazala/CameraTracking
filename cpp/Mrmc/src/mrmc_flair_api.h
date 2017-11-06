#ifndef __FLAIR_API_H__
#define __FLAIR_API_H__
#include <cstdint>

#define FLAIRAPI_DATA_LEN		16		// Maximum data allowed in the packet
#define FLAIRAPI_MARKER			0xABCF	// Identifies it as an API packet, not a mistake
#define FLAIRAPI_MAX_PACKETS	20		// Maximum number of packets that can be received and processed at once

#pragma pack (1)						// Fully pack with no spare bytes to ensure compatability
typedef struct
{
	uint16_t	marker = 0xABCF;
	uint16_t	major;
	uint16_t	minor;
	uint16_t	length;
	uint8_t		bWrite;
	int16_t		number;
	int16_t		error;
	int32_t		checksum;
	int32_t		sender;	// Internal Use only
	float		data[FLAIRAPI_DATA_LEN];
} FlairData;


///////////////////////////////////////////////////////
#define FLAIRAPI_VERSION			0	
#define FLAIRVER_API				0	
#define FLAIRVER_FLAIR				1


////////////////////////////////////////////////////////
#define FLAIRAPI_CMD				1
#define FLAIRCMD_STOP				0
#define FLAIRCMD_SHOOT				1
#define FLAIRCMD_FWDRUN				2
#define FLAIRCMD_BCKRUN				3
#define FLAIRCMD_PARTRUN			4
#define FLAIRCMD_GOTO				5
#define FLAIRCMD_BROWSE				6
#define FLAIRCMD_STEPFWD			7
#define FLAIRCMD_STEPBCK			8
#define FLAIRCMD_TAKE				9
#define FLAIRCMD_TURNOVER			10
#define FLAIRCMD_PLAY				11
#define FLAIRCMD_RECORD				12
#define FLAIRCMD_BACK21				13
#define FLAIRCMD_QUIT				14
#define FLAIRCMD_TRIGON				15
#define FLAIRCMD_TRIGOFF			16


///////////////////////////////////////////////////////
#define FLAIRAPI_GOTO				2
#define FLAIRGOTO_GOTOFRM			0
#define FLAIRGOTO_GOTOPOSN			1
#define FLAIRGOTO_CLRGOTO			2
#define FLAIRGOTO_SETPOSN			3
#define FLAIRGOTO_SETCART			4
#define FLAIRGOTO_SETSPIN			5
#define FLAIRGOTO_POSNGOTO			6	// position
//#define FLAIRGOTO_POSNGOTO		7	// cartesian
//#define FLAIRGOTO_POSNGOTO		8	// spin
#define FLAIRGOTO_DOGOTO			9
#define FLAIRGOTO_GOTOLEN			10


///////////////////////////////////////////////////////
#define FLAIRAPI_STATUS				3
#define FLAIRSTS_STATUS				0
#define FLAIRSTS_RUNSTATE			1
#define FLAIRSTS_ERROR				2
#define FLAIRSTS_LASTERROR			3
#define FLAIRSTS_CURRPOS			4
#define FLAIRSTS_CURRCART			5
#define FLAIRSTS_CURRFRM			6
#define FLAIRSTS_RUNTIME			7
#define FLAIRSTS_ZEROERROR			8
#define FLAIRSTS_AXISERROR			9
#define FLAIRSTS_NETWORKBOARDS		10
#define FLAIRSTS_MULTISTATUS		11


///////////////////////////////////////////////////////
#define FLAIRAPI_EDIT				4
#define FLAIREDIT_CLEARJOB			0
#define FLAIREDIT_ADDLINE			1
#define FLAIREDIT_INSLINE			2
#define FLAIREDIT_DELLINE			3
#define FLAIREDIT_STORE				4
#define FLAIREDIT_SETAXACT			5
#define FLAIREDIT_SETWAYPACT		6
#define FLAIREDIT_SETPOS			7
#define FLAIREDIT_SETFRM			8


///////////////////////////////////////////////////////
#define FLAIRAPI_SETUP				5
#define FLAIRSETUP_FPS				0
#define FLAIRSETUP_EXPOSURE			1
#define FLAIRSETUP_STEREO			2
#define FLAIRSETUP_TTMODE			3
#define FLAIRSETUP_TIMELAPSEDELAY	4
#define FLAIRSETUP_FRAMESPERTAKE	5
#define FLAIRSETUP_FRAMESPERSTEP	6
#define FLAIRSETUP_FRAMEPOSITION	7
#define FLAIRSETUP_CONTINUOUSSTEP	8
#define FLAIRSETUP_MOVINGSTEP		9
#define FLAIRSETUP_CAMTRIG			10
#define FLAIRSETUP_CURRENTFRAME		11
#define FLAIRSETUP_BEGINFRAME		12
#define FLAIRSETUP_ENDFRAME			13
#define FLAIRSETUP_STARTFRAME		14
#define FLAIRSETUP_STOPFRAME		15
#define FLAIRSETUP_TIMECODE			16
#define FLAIRSETUP_TCMODE			17
#define FLAIRSETUP_TCBASE			18
#define FLAIRSETUP_MASTERHEAD		19
#define FLAIRSETUP_AXIS				20


///////////////////////////////////////////////////////
#define FLAIRAPI_MOVE				6
#define FLAIRMOVE_AXISVEL			0
#define FLAIRMOVE_AXISPOS			1
#define FLAIRMOVE_HEADVEL			2
#define FLAIRMOVE_HEADPOS			3
#define FLAIRMOVE_FUNCTION			4
#define FLAIRMOVE_SPEED				5
#define FLAIRMOVE_BROWSE			6
#define FLAIRMOVE_HEADSELECT		7
#define FLAIRMOVE_POSNSELECT		8
#define FLAIRMOVE_JOBSELECT			9


///////////////////////////////////////////////////////
#define FLAIRAPI_JOB				7
#define FLAIRJOB_LOAD				0
#define FLAIRJOB_SAVE				1


///////////////////////////////////////////////////////
#define	FLAIRAPI_AXIS				8
#define	FLAIRAXIS_ENABLE			0
#define	FLAIRAXIS_DISABLE			1
#define	FLAIRAXIS_ACTIVE			2
#define	FLAIRAXIS_INACTIVE			3
#define	FLAIRAXIS_ZEROAT			4
#define	FLAIRAXIS_HOME				5


///////////////////////////////////////////////////////
#define	FLAIRAPI_INFO				9
#define	FLAIRINFO_GETWAYP			0
#define	FLAIRINFO_SETAXIS			7
#define	FLAIRINFO_GETPOS			9
#define	FLAIRINFO_GETFRM			11
#define	FLAIRINFO_GETNWAYP			11



///////////////////////////////////////////////////////
// API Error codes
///////////////////////////////////////////////////////
#define	FE_SUCCESS					0
#define	FE_BADMAJOR					1
#define	FE_BADMINOR					2
#define	FE_ILLSTATE					3
#define	FE_EOD						4
#define	FE_NOTT						5
#define	FE_TTINVALID				6
#define	FE_NOSPINRIG				7
#define	FE_NOTDUN					8



#endif // __FLAIR_API_H__