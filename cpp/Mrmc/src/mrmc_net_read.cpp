#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include <boost/array.hpp>
#include <iostream>
#include <thread>
#include <mutex>
#include <conio.h> 

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
		std::cout << "\n[" << bytes_transferred << " bytes] received : ";

		if (bytes_transferred == 44)
		{
			float float_buff[11];
			memcpy(&float_buff[0], &recv_buffer[0], bytes_transferred);

			for (int i = 0; i < 11; i++)
				std::cout << std::fixed << float_buff[i] << " ";
			std::cout << std::endl;
		}
		else
			for (int i = 0; i < bytes_transferred; ++i)
				std::cout << recv_buffer[i];

		if (!error || error == boost::asio::error::message_size)
			start_receive();
	}
private:
	boost::asio::ip::udp::socket socket;
	boost::asio::ip::udp::endpoint receiver_endpoint;
	boost::array<char, 256> recv_buffer;

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
	uint16_t port = 10002;
	if (argc > 1)
		port = std::atoi(argv[1]);

	std::thread udp_receive_thread(run_udp_client, port);
	
	while (getch() != 27) {}	// wait for escape key

	mutex.lock();
	{
		io_service.stop();
	}
	mutex.unlock();
	
	udp_receive_thread.join();

	return EXIT_SUCCESS;
}