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
#include <cstring>
#include <iostream>
#include <thread>
#include <cstdlib>
#include <boost/asio.hpp>

#include <conio.h> 



using boost::asio::ip::tcp;

#define API_DATA_LEN	16
#define API_MARKER		0xABCF

typedef struct
{
	unsigned short	marker = API_MARKER;
	unsigned short	major;
	unsigned short	minor;
	unsigned short	length;
	unsigned char	bWrite = TRUE;
	short			number;
	short			error;
	int				checksum;
	void			*unused;	// Internal Use only
	float			data[API_DATA_LEN];
} ApiData;


ApiData dataSent;
ApiData dataReceived;

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
		<< "[ 2 ]   = Goto Frame 5" << std::endl
		<< "[ f ]   = Fwd Run" << std::endl
		<< "[ b ]   = Bck Run" << std::endl
		<< "[ w ]   = bWrite" << std::endl
		<< "[ esc ] = Exit" << std::endl;
}

void printData(const ApiData& data)
{
	std::cout
		<< data.marker << ' ' << data.major << ' ' << data.minor << ' '
		<< data.length << ' ' << (int)(data.bWrite) << ' ' << data.number << ' '
		<< data.error << '\t';
	for (int j = 0; j < API_DATA_LEN; ++j)
		std::cout << data.data[j] << ' ';
	std::cout << std::endl << std::endl;
}

void play()
{
	dataSent.marker = 43983;
	dataSent.major = 1;
	dataSent.minor = 1;
}

void stop()
{
	dataSent.marker = 43983;
	dataSent.major = 1;
	dataSent.minor = 0;
}

void go()
{
	dataSent.marker = 43983;
	dataSent.major = 1;
	dataSent.minor = 5;
}


void fwd()
{
	dataSent.marker = 43983;
	dataSent.major = 1;
	dataSent.minor = 2;
}


void bck()
{
	dataSent.marker = 43983;
	dataSent.major = 1;
	dataSent.minor = 3;
}


void go_clear()
{
	dataSent.marker = 43983;
	dataSent.major = 2;
	dataSent.minor = 2;
}


void go_frame(int frame_index)
{
	dataSent.marker = 43983;
	dataSent.major = 2;
	dataSent.minor = 0;
	dataSent.length = frame_index;
	dataSent.number = frame_index;
	dataSent.data[0] = frame_index;
}

void go_position(int pos_index)
{
	dataSent.marker = 43983;
	dataSent.major = 2;
	dataSent.minor = 1;
	dataSent.length = pos_index;
	dataSent.number = pos_index;
	dataSent.data[0] = pos_index;
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
			play();
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
	using namespace std::chrono_literals;
	try
	{
		std::string host_address = "127.0.0.1";
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

		while(true)
		{
			if (get_key_command())
			{
				std::cout << "Sent to server   : ";
				printData(dataSent);

				// Sending
				size_t request_length = sizeof(dataSent);
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
	}

	return 0;
}