#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include <boost/array.hpp>
#include <iostream>
#include <thread>
#include <mutex>
#include <conio.h> 

#include "config.h"	
#include "stype.h"	


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

		stype::packet packet(recv_buffer.data());

		std::cout
		//	<< std::fixed << packet.timecode()
			<< ' ' << packet << std::endl;


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
	
	while (getch() != 27) {}	// wait for escape key

	mutex.lock();
	{
		io_service.stop();
	}
	mutex.unlock();
	
	udp_receive_thread.join();

	return EXIT_SUCCESS;
}