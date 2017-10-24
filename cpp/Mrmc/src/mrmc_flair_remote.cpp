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
#include <boost/asio.hpp>

#include <conio.h> 



using boost::asio::ip::tcp;

#define API_DATA_LEN	16
#define API_MARKER		0xABCF

typedef struct
{
	unsigned short	marker;
	unsigned short	major;
	unsigned short	minor;
	unsigned short	length;
	unsigned char	bWrite;
	short			number;
	short			error;
	int				checksum;
	void			*unused;	// Internal Use only
	float			data[API_DATA_LEN];
} ApiData;


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

		for(int i=0; i<5; ++i)
		{

			ApiData dataSent;
			dataSent.marker = i;
			dataSent.major = 1;
			dataSent.minor = 2;
			dataSent.length = 3;
			dataSent.bWrite = API_MARKER;
			dataSent.number = 4;
			dataSent.error = 5;

			for (int j = 0; j < API_DATA_LEN; ++j)
				dataSent.data[j] = j * i + j;

			std::cout
				<< "Send to server   : "
				<< dataSent.marker << ' ' << dataSent.major << ' ' << dataSent.minor << ' '
				<< dataSent.length << ' ' << dataSent.bWrite << ' ' << dataSent.number << ' '
				<< dataSent.error << '\t';
			for (int j = 0; j < API_DATA_LEN; ++j)
				std::cout << dataSent.data[j] << ' ';
			std::cout << std::endl;

			size_t request_length = sizeof(dataSent);
			boost::asio::write(s, boost::asio::buffer(&dataSent, request_length));


			std::this_thread::sleep_for(1s);

			// Receiving back
			ApiData dataReceived;
			
			size_t reply_length = boost::asio::read(s, boost::asio::buffer(&dataReceived, request_length));
			std::cout 
				<< "Reply from server: "
				<< dataReceived.marker << ' ' << dataReceived.major << ' ' << dataReceived.minor << ' '
				<< dataReceived.length << ' ' << dataReceived.bWrite << ' ' << dataReceived.number << ' '
				<< dataReceived.error << '\t';
			for (int j = 0; j < API_DATA_LEN; ++j)
				std::cout << dataReceived.data[j] << ' ';
			std::cout << std::endl << std::endl;
						
			std::this_thread::sleep_for(1s);

		}
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}