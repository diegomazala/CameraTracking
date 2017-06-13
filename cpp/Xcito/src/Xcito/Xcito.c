#include "Xcito.h"

#include "trk_net.h"
#include "read_struct.h" 
#include "trk_struct.h"

#include <process.h>


static trkHandle			handle;
static trkCameraConstants	camConsts;
static trkCameraParams		camParams;
static trkError				error;

static int					multiThread;
static int					stopThread;
HANDLE						handleThread;
HANDLE						handleEvent;
unsigned					threadId;
static CRITICAL_SECTION		criticalSection;


XCITO_API int XcitoWriteNetOpen(const char* remoteHost, int remotePort)
{
	// Variables used to check the available protocols
	int		iResult = 0;
	WSADATA	wsaData;
	int		binary_format = 0;

	// Initialize Winsock
#ifdef _WIN32 || _WIN64
	iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) 
	{
		printf("WSAStartup failed: %d\n", iResult);        
		return trkFailed;
	}  
#endif

	return trkNetOpenWrite (remoteHost, remotePort, binary_format, &handle);
}


XCITO_API int XcitoWriteNetClose()
{
	int			iResult = 0;

	trkError	err = trkNetClose (handle);

#ifdef _WIN32 || _WIN64
	iResult = WSACleanup();
	if (iResult != 0) 
	{
		printf("WSACleanup failed: %d\n", iResult);        
		return trkFailed;
	}  
#endif

	return err;
}

XCITO_API int XcitoWriteNetUpdate(void* pArrayInt10, void* pArrayDouble27)
{
	int i=0, j=0, k=0;
	trkError err = trkFailed;
	int* pArrayInt = (int*) pArrayInt10;
	double* pArrayDouble = (double*) pArrayDouble27;

	// safeguard - pointer must be not null
	if( !pArrayInt || !pArrayDouble)
		return trkFailed;


	// Copying Camera constants
	camConsts.id			= pArrayInt[CONSTS_ID];
	camConsts.imageWidth	= pArrayInt[CONSTS_IMAGE_WIDTH];
	camConsts.imageHeight	= pArrayInt[CONSTS_IMAGE_HEIGHT];
	camConsts.blankLeft		= pArrayInt[CONSTS_BLANK_LEFT];
	camConsts.blankRight	= pArrayInt[CONSTS_BLANK_RIGHT];
	camConsts.blankTop		= pArrayInt[CONSTS_BLANK_TOP];
	camConsts.blankBottom	= pArrayInt[CONSTS_BLANK_BOTTOM];

	camConsts.chipWidth		= pArrayDouble[CONSTS_CHIP_WIDTH];
	camConsts.chipHeight	= pArrayDouble[CONSTS_CHIP_HEIGTH];
	camConsts.fakeChipWidth	= pArrayDouble[CONSTS_FAKE_CHIP_WIDTH];
	camConsts.fakeChipHeight= pArrayDouble[CONSTS_FAKE_CHIP_HEIGTH];

	err = trkNetWriteConstants (handle, &camConsts);
	

	// Copying Camera parameters
	camParams.id					= pArrayInt[PARAMS_ID];
	camParams.format				= pArrayInt[PARAM_FORMAT];
	camParams.counter				= pArrayInt[PARAMS_COUNTER];

	camParams.fov					= pArrayDouble[PARAMS_FOV]; 
	camParams.centerX				= pArrayDouble[PARAMS_CENTER_X]; 
	camParams.centerY				= pArrayDouble[PARAMS_CENTER_Y];
	camParams.k1					= pArrayDouble[PARAMS_K1];
	camParams.k2					= pArrayDouble[PARAMS_K2];
	camParams.focdist				= pArrayDouble[PARAMS_FOC_DIST];
	camParams.aperture				= pArrayDouble[PARAMS_APERTURE];

	// Copying matrix parameters
	for(i=0; i<4; ++i)
	{
		for(j=0; j<4; ++j)
		{
			camParams.t.m[i][j] = pArrayDouble[PARAMS_MV_00 + k++];
		}
	}

	err = trkNetWriteParams (handle, &camParams);
	
	return err;
}


unsigned __stdcall XcitoReadNetThread(void *argList) 
{
	HANDLE hEvent = *((HANDLE*)argList);
	while (WaitForSingleObject(hEvent, 0) != WAIT_OBJECT_0) 
	{
		Sleep(3); 
		error = trkNetReadConstants (handle, &camConsts);
		error = trkNetReadParams (handle, &camParams);
	}

	_endthreadex( 0 );

	return 0;
}


XCITO_API int XcitoReadNetOpen(int port, int ignoreOldParams, int multithread)
{
	multiThread = multithread;

	if (multiThread)
	{
		InitializeCriticalSection(&criticalSection);	
		
		// Create a manual-reset nonsignaled unnamed event
		handleEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

		handleThread = (HANDLE)_beginthreadex( NULL, 0, XcitoReadNetThread, &handleEvent, 0, &threadId);

		if (handleThread == 0)
		{
			return FALSE;
		}
	}

	return trkNetOpenRead (port, &handle, ignoreOldParams);
}

	
XCITO_API int XcitoReadNetClose()
{
	if (multiThread)
	{
		SetEvent(handleEvent);
		WaitForSingleObject(handleThread, 1000);	// wait 1 second
		CloseHandle(handleEvent);
		CloseHandle(handleThread);
		DeleteCriticalSection(&criticalSection);
	}

	return trkNetClose (handle);
}

	
XCITO_API int XcitoReadNetUpdate(void* pArrayInt10, void* pArrayDouble27)
{
	trkError err = trkOK;
	int* pArrayInt = (int*) pArrayInt10;
	double* pArrayDouble = (double*) pArrayDouble27;
	
	// safeguard - pointer must be not null
 	if( !pArrayInt || !pArrayDouble)
 		return trkFailed;

	if (!multiThread)
		err = trkNetReadConstants (handle, &camConsts);
	else
		EnterCriticalSection(&criticalSection);

	if (err == trkOK)
	{
		pArrayInt[CONSTS_ID]			= camConsts.id;
		pArrayInt[CONSTS_IMAGE_WIDTH]	= camConsts.imageWidth;
		pArrayInt[CONSTS_IMAGE_HEIGHT]	= camConsts.imageHeight;
		pArrayInt[CONSTS_BLANK_LEFT]	= camConsts.blankLeft;
		pArrayInt[CONSTS_BLANK_RIGHT]	= camConsts.blankRight;
		pArrayInt[CONSTS_BLANK_TOP]		= camConsts.blankTop;
		pArrayInt[CONSTS_BLANK_BOTTOM]	= camConsts.blankBottom;

		pArrayDouble[CONSTS_CHIP_WIDTH]			= camConsts.chipWidth;
		pArrayDouble[CONSTS_CHIP_HEIGTH]		= camConsts.chipHeight;
		pArrayDouble[CONSTS_FAKE_CHIP_WIDTH]	= camConsts.fakeChipWidth;
		pArrayDouble[CONSTS_FAKE_CHIP_HEIGTH]	= camConsts.fakeChipHeight;
	}

	if (!multiThread)
		err = trkNetReadParams (handle, &camParams);

	if (err == trkOK)
	{
		pArrayInt[PARAMS_ID]			= camParams.id;
		pArrayInt[PARAM_FORMAT]			= camParams.format;
		pArrayInt[PARAMS_COUNTER]		= camParams.counter;
		
		pArrayDouble[PARAMS_FOV]		= camParams.fov;           
		pArrayDouble[PARAMS_CENTER_X]	= camParams.centerX;       
		pArrayDouble[PARAMS_CENTER_Y]	= camParams.centerY;
		pArrayDouble[PARAMS_K1]			= camParams.k1;            
		pArrayDouble[PARAMS_K2]			= camParams.k2;
		pArrayDouble[PARAMS_FOC_DIST]	= camParams.focdist; 
		pArrayDouble[PARAMS_APERTURE]	= camParams.aperture;


		pArrayDouble[PARAMS_MV_00]	= camParams.t.m[0][0];
		pArrayDouble[PARAMS_MV_01]	= camParams.t.m[0][1];
		pArrayDouble[PARAMS_MV_02]	= camParams.t.m[0][2];
		pArrayDouble[PARAMS_MV_03]	= camParams.t.m[0][3];
		pArrayDouble[PARAMS_MV_10]	= camParams.t.m[1][0];
		pArrayDouble[PARAMS_MV_11]	= camParams.t.m[1][1];
		pArrayDouble[PARAMS_MV_12]	= camParams.t.m[1][2];
		pArrayDouble[PARAMS_MV_13]	= camParams.t.m[1][3];
		pArrayDouble[PARAMS_MV_20]	= camParams.t.m[2][0];
		pArrayDouble[PARAMS_MV_21]	= camParams.t.m[2][1];
		pArrayDouble[PARAMS_MV_22]	= camParams.t.m[2][2];
		pArrayDouble[PARAMS_MV_23]	= camParams.t.m[2][3];
		pArrayDouble[PARAMS_MV_30]	= camParams.t.m[3][0];
		pArrayDouble[PARAMS_MV_31]	= camParams.t.m[3][1];
		pArrayDouble[PARAMS_MV_32]	= camParams.t.m[3][2];
		pArrayDouble[PARAMS_MV_33]	= camParams.t.m[3][3];
	}

	if (!multiThread)
		error = err;
	else
		LeaveCriticalSection(&criticalSection);

	return err;
}

XCITO_API int XcitoReadNetGetParamsAvailable()
{
	return trkNetParamsAvailable(handle);
}

XCITO_API void XcitoReadNetSkipOld(int skip)
{
	trkNetSkipOld(skip, handle);
}



XCITO_API double XcitoGetX()
{
	return camParams.t.e.x;   
}

XCITO_API double XcitoGetY()
{
	return camParams.t.e.y;  
}

XCITO_API double XcitoGetZ()
{
	return camParams.t.e.z;   
}

XCITO_API double XcitoGetPan()
{
	return camParams.t.e.pan;   
}

XCITO_API double XcitoGetTilt()
{
	return camParams.t.e.tilt;   
}

XCITO_API double XcitoGetRoll()
{
	return camParams.t.e.roll;   
}
   


XCITO_API const char* XcitoErrorMessage(int trk_error)
{
	return  trkErrorMessage ((trkError)trk_error);
}



XCITO_API int XcitoGetId()
{
	return camParams.id;
}


XCITO_API int XcitoGetCounter()
{
	return camParams.counter;
}


XCITO_API int XcitoGetFormat()
{
	return camParams.format;
}

XCITO_API double XcitoGetFov()
{
	return camParams.fov;
}

