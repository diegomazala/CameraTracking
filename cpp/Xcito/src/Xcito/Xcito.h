#ifdef __cplusplus
extern "C" {
#endif


#if _MSC_VER // this is defined when compiling with Visual Studio
	#define XCITO_API __declspec(dllexport) // Visual Studio needs annotating exported functions with this
#else
	#define XCITO_API // XCode does not need annotating exported functions, so define is empty
#endif

#ifdef _WIN32 || _WIN64
	#include <windows.h>
#endif


enum EXcitoFormat
{
	TrkMatrix			= 0x0000,
	TrkEuler			= 0x0001,
	
	TrkCameraToStudio	= 0x0000,
	TrkStudioToCamera	= 0x0002,
	
	TrkCameraZ_Up		= 0x0000,
	TrkCameraY_Up		= 0x0004,
	
	TrkStudioZ_Up		= 0x0000,
	TrkStudioY_Up		= 0x0008,

	TrkImageDistance	= 0x0000,
	TrkFieldOfView		= 0x0010,

	TrkHorizontal		= 0x0000,
	TrkVertical			= 0x0020,
	TrkDiagonal			= 0x0040,

	TrkConsiderBlank	= 0x0000,
	TrkIgnoreBlank		= 0x0080,

	TrkAdjustAspect		= 0x0000,
	TrkKeepAspect		= 0x0100,

	TrkShiftOnChip		= 0x0000,
	TrkShiftInPixels	= 0x0200
};


enum EXcitoArrayInts
{
	CONSTS_ID,
	CONSTS_IMAGE_WIDTH,
	CONSTS_IMAGE_HEIGHT,
	CONSTS_BLANK_LEFT,
	CONSTS_BLANK_RIGHT,
	CONSTS_BLANK_TOP,
	CONSTS_BLANK_BOTTOM,
	PARAMS_ID,
	PARAM_FORMAT,
	PARAMS_COUNTER,
	EIntTotal
};


enum EXcitoArrayDoubles
{
	CONSTS_CHIP_WIDTH,
	CONSTS_CHIP_HEIGTH,
	CONSTS_FAKE_CHIP_WIDTH,
	CONSTS_FAKE_CHIP_HEIGTH,
	PARAMS_FOV,
	PARAMS_CENTER_X,
	PARAMS_CENTER_Y,
	PARAMS_K1,
	PARAMS_K2,
	PARAMS_FOC_DIST,
	PARAMS_APERTURE,
	PARAMS_MV_00,
	PARAMS_MV_01,
	PARAMS_MV_02,
	PARAMS_MV_03,
	PARAMS_MV_10,
	PARAMS_MV_11,
	PARAMS_MV_12,
	PARAMS_MV_13,
	PARAMS_MV_20,
	PARAMS_MV_21,
	PARAMS_MV_22,
	PARAMS_MV_23,
	PARAMS_MV_30,
	PARAMS_MV_31,
	PARAMS_MV_32,
	PARAMS_MV_33,
	EDoublesTotal
};



XCITO_API int XcitoWriteNetOpen(const char* remoteHost, int remotePort);
XCITO_API int XcitoWriteNetClose();
XCITO_API int XcitoWriteNetUpdate(void* pArrayInt10, void* pArrayDouble27);
XCITO_API int XcitoReadNetOpen(int port, int ignoreOldParams, int multithread);
XCITO_API int XcitoReadNetClose();
XCITO_API int XcitoReadNetUpdate(void* pArrayInt10, void* pArrayDouble27);
XCITO_API int XcitoReadNetGetParamsAvailable();
XCITO_API void XcitoReadNetSkipOld(int skip);
XCITO_API const char* XcitoErrorMessage(int trk_error);


XCITO_API int XcitoGetId();
XCITO_API int XcitoGetCounter();
XCITO_API int XcitoGetFormat();
XCITO_API double XcitoGetFov();
XCITO_API double XcitoGetX();
XCITO_API double XcitoGetY();
XCITO_API double XcitoGetZ();
XCITO_API double XcitoGetPan();
XCITO_API double XcitoGetTilt();
XCITO_API double XcitoGetRoll();


#ifdef __cplusplus
};
#endif