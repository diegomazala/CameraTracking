#define _SILENCE_CXX17_ALLOCATOR_VOID_DEPRECATION_WARNING 
#define _SILENCE_ALL_CXX17_DEPRECATION_WARNINGS
#define WINVER 0x0A00 
#define _WIN32_WINNT 0x0A00  

#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include <boost/array.hpp>
#include <iostream>
#include <thread>
#include <mutex>
#include <conio.h> 


#define CATCH_CONFIG_MAIN
#define JM_CIRCULAR_BUFFER_CXX14
#include "circular_buffer.hpp"

#include "config.h"	
#include "stype.h"	

static jm::circular_buffer<stype::packet, 4> packet_buffer;


class udp_client
{
public:
	
	udp_client(boost::asio::io_service& io_service, uint16_t port):
		socket(io_service, { boost::asio::ip::udp::v4(), port })
	{
	}


	void start_receive()
	{
		socket.async_receive_from(boost::asio::buffer(recv_buffer), receiver_endpoint,
			boost::bind(&udp_client::handle_receive, this,
				boost::asio::placeholders::error,
				boost::asio::placeholders::bytes_transferred));
	}

	void handle_receive(const boost::system::error_code& error, size_t bytes_transferred)
	{
		//std::cout << "\n[" << bytes_transferred << " bytes] received : ";
		if (bytes_transferred == stype::buffer_index::total)
		{
			static std::mutex io_mutex;
			{
				std::lock_guard<std::mutex> lk(io_mutex);
				packet_buffer.push_back({ recv_buffer.data() });

				std::cout << "Buffer [" << packet_buffer.size() << "] : ";
				std::for_each(packet_buffer.begin(), packet_buffer.end(),
					[](const auto& p) { std::cout << unsigned(p.package_number()) << ' '; });
				std::cout << std::endl;

				//std::cout
				////	<< std::fixed << packet.timecode()
				//	<< ' ' << packet << std::endl;
			}
		}

		if (!error || error == boost::asio::error::message_size)
			start_receive();
	}
private:
	boost::asio::ip::udp::socket socket;
	boost::asio::ip::udp::endpoint receiver_endpoint;
	boost::array<uint8_t, 256> recv_buffer;
};


static boost::asio::io_service io_service;
static std::mutex mutex;

void run_udp_client(uint16_t port)
{
	udp_client updclient(io_service, port);
	updclient.start_receive();

	io_service.run();
}

int main(int argc, char* argv[])
{

	stype::config config;
	config.load((argc > 1) ? argv[1] : "../data/stype.config");

	std::thread udp_receive_thread(run_udp_client, config.port);
	
	while (_getch() != 27) {}	// wait for escape key

	mutex.lock();
	{
		io_service.stop();
	}
	mutex.unlock();
	
	udp_receive_thread.join();

	return EXIT_SUCCESS;
}