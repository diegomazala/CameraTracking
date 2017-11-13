#include <boost/asio.hpp>
#include <iostream>
#include <sstream>
#include "mrmc_flair_api.h"

#define DllExport extern "C" __declspec (dllexport)


static FlairData flair_data_send;
static std::unique_ptr<boost::asio::ip::tcp::socket> flair_socket;
static std::unique_ptr<boost::asio::io_service> flair_io_service;
static bool flair_is_connected = false;


DllExport bool MrmcConnect(const char* host_address = "127.0.0.1", uint16_t port = 53025)
{
	if (flair_is_connected)
		return flair_is_connected;

	try
	{
		flair_io_service = std::make_unique<boost::asio::io_service>();
		flair_socket = std::make_unique<boost::asio::ip::tcp::socket>(*flair_io_service);
		boost::asio::ip::tcp::resolver resolver(*flair_io_service);
		boost::asio::connect(*flair_socket, resolver.resolve({ host_address, std::to_string(port) }));
		flair_is_connected = true;
	}
	catch (std::exception& e)
	{
		flair_is_connected = false;
		MessageBox(NULL, e.what(), "Mrmc Flair", MB_ICONWARNING | MB_OK);
	}
	return flair_is_connected;
}


DllExport void MrmcDisconnect()
{
	if (!flair_is_connected)
		return;

	try
	{
		flair_socket->close();
		flair_io_service->stop();
		flair_socket.reset();
		flair_is_connected = false;
	}
	catch (std::exception& e)
	{
		MessageBox(NULL, e.what(), "Mrmc Flair", MB_ICONWARNING | MB_OK);
	}
}

DllExport bool MrmcIsConnected()
{
	return flair_is_connected;
}

DllExport void MrmcSendCommand()
{
	// Sending
	size_t request_length = sizeof(flair_data_send);
	boost::asio::write(*flair_socket, boost::asio::buffer(&flair_data_send, request_length));
}


DllExport void MrmcWrite(bool do_write)
{
	flair_data_send.bWrite = do_write ? 1 : 0;
}

DllExport void MrmcShoot()
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_CMD;
	flair_data_send.minor	= FLAIRCMD_SHOOT;
	MrmcSendCommand();
}

DllExport void MrmcStop()
{	
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_CMD;
	flair_data_send.minor	= FLAIRCMD_STOP;
	MrmcSendCommand();
}

DllExport void MrmcForward()
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_CMD;
	flair_data_send.minor	= FLAIRCMD_FWDRUN;
	MrmcSendCommand();
}

DllExport void MrmcBackward()
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_CMD;
	flair_data_send.minor	= FLAIRCMD_BCKRUN;
	MrmcSendCommand();
}

DllExport void MrmcGoto()
{
	flair_data_send.marker = FLAIRAPI_MARKER;
	flair_data_send.major  = FLAIRAPI_CMD;
	flair_data_send.minor  = FLAIRCMD_GOTO;
	MrmcSendCommand();
}

DllExport void MrmcCleanGoto()
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_GOTO;
	flair_data_send.minor	= FLAIRGOTO_CLRGOTO;
	std::memset(flair_data_send.data, 0, sizeof(flair_data_send.data)); // set to zero
	MrmcSendCommand();
}

DllExport void MrmcGotoFrame(int32_t frame_index)
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_GOTO;
	flair_data_send.minor	= FLAIRGOTO_GOTOFRM;
	flair_data_send.bWrite	= TRUE;
	flair_data_send.length	= 1;
	flair_data_send.number	= 0;
	flair_data_send.data[0] = (float)frame_index;
	MrmcSendCommand();
	MrmcGoto();
}

DllExport void MrmcGotoPosition(int16_t position_index)
{
	flair_data_send.marker	= FLAIRAPI_MARKER;
	flair_data_send.major	= FLAIRAPI_GOTO;
	flair_data_send.minor	= FLAIRGOTO_GOTOPOSN;
	flair_data_send.bWrite	= TRUE;
	flair_data_send.length	= 1;
	flair_data_send.number	= 0;
	flair_data_send.data[0] = (float)position_index;
	MrmcSendCommand();
	MrmcGoto();
}
	