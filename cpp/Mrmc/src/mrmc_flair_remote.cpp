//
// blocking_tcp_echo_client.cpp
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//
// Copyright (c) 2003-2017 Christopher M. Kohlhoff (chris at kohlhoff dot com)
//
// Distributed under the Boost Software License, Version 1.0. (See accompanying
// file LICENSE_1_0.txt or copy at http://www.boost.org/LICENSE_1_0.txt)
//

#include <cstdlib>
#include <cstdint>
#include <cstring>
#include <iostream>
#include <thread>
#include <cstdlib>
#include <boost/asio.hpp>

#include <conio.h> 
#include "mrmc_flair_api.h"


using boost::asio::ip::tcp;



FlairData dataSent;
FlairData dataReceived;

void printMenu()
{
	std::cout
		<< "Menu: " << std::endl
		<< "[ p ]   = Play" << std::endl
		<< "[ s ]   = Stop" << std::endl
		<< "[ c ]   = Goto Clear" << std::endl
		<< "[ g ]   = Goto" << std::endl
		<< "[ 1 ]   = Goto Position 1" << std::endl
		<< "[ 2 ]   = Goto Position 2" << std::endl
		<< "[ 3 ]   = Goto Position 3" << std::endl
		<< "[ 5 ]   = Goto Frame 50" << std::endl
		<< "[ f ]   = Fwd Run" << std::endl
		<< "[ b ]   = Bck Run" << std::endl
		<< "[ w ]   = bWrite" << std::endl
		<< "[ esc ] = Exit" << std::endl;
}

void printData(const FlairData& data)
{
	std::cout
		<< data.marker << ' ' << data.major << ' ' << data.minor << ' '
		<< data.length << ' ' << (int)(data.bWrite) << ' ' << data.number << ' '
		<< data.error << '\t';
	for (int j = 0; j < FLAIRAPI_DATA_LEN; ++j)
		std::cout << data.data[j] << ' ';
	std::cout << std::endl << std::endl;
}

void shoot()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_CMD;
	dataSent.minor = FLAIRCMD_SHOOT;
}

void stop()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_CMD;
	dataSent.minor = FLAIRCMD_STOP;
}

void go()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_CMD;
	dataSent.minor = FLAIRCMD_GOTO;
}


void fwd()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_CMD;
	dataSent.minor = FLAIRCMD_FWDRUN;
}


void bck()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_CMD;
	dataSent.minor = FLAIRCMD_BCKRUN;
}


void go_clear()
{
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_GOTO;
	dataSent.minor = FLAIRGOTO_CLRGOTO;
	std::memset(dataSent.data, 0, sizeof(dataSent.data)); // set to zero
}


void go_frame(float frame_index)
{
	std::cout << "GoTo Frame " << frame_index << std::endl;
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_GOTO;
	dataSent.minor = FLAIRGOTO_GOTOFRM;
	dataSent.length = 1;
	dataSent.number = 0;
	dataSent.data[0] = frame_index;
}

void go_position(short pos_index)
{
	std::cout << "GoTo Position " << pos_index << std::endl;
	dataSent.marker = FLAIRAPI_MARKER;
	dataSent.major = FLAIRAPI_GOTO;
	dataSent.minor = FLAIRGOTO_GOTOPOSN;
#if 0
	dataSent.length = 0;
	dataSent.number = pos_index;
#else
	dataSent.length		= 1;
	dataSent.number		= 0;
	dataSent.data[0]	= pos_index;
#endif
}

void write()
{
	if (dataSent.bWrite == TRUE)
		dataSent.bWrite = FALSE;
	else
		dataSent.bWrite = TRUE;
}

bool get_key_command()
{
	bool command = true;
	int key = getch();

	switch (key)
	{
		case 112:	// p
		case 80:	// P
			shoot();
			break;

		case 115:	// s
		case 83:	// S
			stop();
			break;

		case 102:	// f
		case 70:	// F
			fwd();
			break;

		case 98:	// b
		case 66:	// B
			bck();
			break;

		case 99:	// c
		case 67:	// C
			go_clear();
			break;

		case 49:	// 1
			go_position(1);
			break;

		case 50:	// 2
			go_position(2);
			break;

		case 51:	// 3
			go_position(3);
			break;

		case 53:	// 5
			go_frame(50);
			break;

		case 103:	// g
		case 71:	// G
			go();
			break;

		case 119:	// w
		case 87:	// W
			write();
			printData(dataSent);
			break;

		case 113:	// q
		case 81:	// Q
		case 120:	// x
		case 88:	// X
		case 27:	// esc
			std::exit(EXIT_SUCCESS);
			break;

		default:
			printMenu();
			command = false;
			break;
	}
	
	return command;
}


int main(int argc, char* argv[])
{
	dataSent.bWrite = TRUE;
	using namespace std::chrono_literals;
	try
	{
		std::string host_address = "127.0.0.1";
		//std::string host_address = "192.168.43.44";
		std::string port = "53025";

		if (argc != 3)
		{
			std::cerr << "Usage: tcp_client <host> <port>\n";
			std::cerr << "Using: tcp_client " << host_address << ' ' << port << std::endl;
			//return 1;
		}
		else
		{
			host_address = argv[1];
			std::string port = argv[2];
		}

		boost::asio::io_service io_service;

		tcp::socket s(io_service);
		tcp::resolver resolver(io_service);
		boost::asio::connect(s, resolver.resolve({ host_address, port }));

		std::cout << "Connected to " << host_address << std::endl;

		while(true)
		{
			if (get_key_command())
			{
				std::cout << "Sent to server   : ";
				printData(dataSent);

				// Sending
				size_t request_length = sizeof(dataSent);
				
				//Sending Side
				//char buffer[sizeof(dataSent)];
				//memcpy(buffer, &dataSent, sizeof(dataSent));

				boost::asio::write(s, boost::asio::buffer(&dataSent, request_length));

				//// Receiving back
				//size_t reply_length = boost::asio::read(s, boost::asio::buffer(&dataReceived, request_length));
				//std::cout << "Reply from server: ";
				//printData(dataReceived);
			}
		}
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
		getch();
	}

	return 0;
}