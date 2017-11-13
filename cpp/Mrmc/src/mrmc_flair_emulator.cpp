#include <cstdlib>
#include <iostream>
#include <thread>
#include <utility>
#include <boost/asio.hpp>
#include "mrmc_flair_api.h"

using boost::asio::ip::tcp;

const int max_length = 1024;



void session(tcp::socket sock)
{
	try
	{
		for (;;)
		{
			FlairData dataBuffer[1];
			
			boost::system::error_code error;
			size_t length = sock.read_some(boost::asio::buffer(dataBuffer), error);

			if (error == boost::asio::error::eof)
				break; // Connection closed cleanly by peer.
			else if (error)
				throw boost::system::system_error(error); // Some other error.

			const FlairData& dataReceived = dataBuffer[0];
			std::cout
				<< "-- Received -- " << std::endl
				<< "Marker : " << dataReceived.marker << std::endl
				<< "Major  : " << dataReceived.major << std::endl
				<< "Minor  : " << dataReceived.minor << std::endl
				<< "Length : " << dataReceived.length << std::endl
				<< "bWrite : " << static_cast<uint16_t>(dataReceived.bWrite) << std::endl
				<< "Number : " << dataReceived.number << std::endl
				<< "Error  : " << dataReceived.error << std::endl
				<< "Data   : " << std::defaultfloat;

			for (int j = 0; j < FLAIRAPI_DATA_LEN; ++j)
				std::cout << dataReceived.data[j] << ' ';
			std::cout << std::endl << std::endl;

			// sending back
			boost::asio::write(sock, boost::asio::buffer(dataBuffer, length));
		}
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception in thread: " << e.what() << "\n";
	}
}

void server(boost::asio::io_service& io_service, unsigned short port)
{
	tcp::acceptor a(io_service, tcp::endpoint(tcp::v4(), port));
	for (;;)
	{
		tcp::socket sock(io_service);
		a.accept(sock);
		std::thread(session, std::move(sock)).detach();
	}
}

int main(int argc, char* argv[])
{
	try
	{
		int port = 53025;
		if (argc < 2)
		{
			std::cerr
				<< "Usage  : flair_emulator <port>" << std::endl
				<< "Running: flair_emulator " << port << std::endl;
		}
		else
		{
			port = std::atoi(argv[1]);
		}

		boost::asio::io_service io_service;

		server(io_service, port);
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}