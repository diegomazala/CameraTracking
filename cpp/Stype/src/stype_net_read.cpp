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

#if 0 // multicast


const short multicast_port = 6301;

class receiver
{
public:
	receiver(boost::asio::io_service& io_service,
		const boost::asio::ip::address& listen_address,
		const boost::asio::ip::address& multicast_address)
		: socket_(io_service)
	{
		// Create the socket so that multiple may be bound to the same address.
		boost::asio::ip::udp::endpoint listen_endpoint(
			listen_address, multicast_port);
		socket_.open(listen_endpoint.protocol());
		socket_.set_option(boost::asio::ip::udp::socket::reuse_address(true));
		socket_.bind(listen_endpoint);

		// Join the multicast group.
		socket_.set_option(
			boost::asio::ip::multicast::join_group(multicast_address));

		socket_.async_receive_from(
			boost::asio::buffer(data_, max_length), sender_endpoint_,
			boost::bind(&receiver::handle_receive_from, this,
				boost::asio::placeholders::error,
				boost::asio::placeholders::bytes_transferred));
	}

	void handle_receive_from(const boost::system::error_code& error,
		size_t bytes_recvd)
	{
		if (!error)
		{
			//std::cout.write(data_, bytes_recvd);
			//std::cout << std::endl;

			std::cout << "\n[" << bytes_recvd << " bytes] received : ";

			if (bytes_recvd == stype::buffer_index::total)
			{
				static std::mutex io_mutex;
				{
					std::lock_guard<std::mutex> lk(io_mutex);
					packet_buffer.push_back(data_);

					std::cout << "Buffer [" << packet_buffer.size() << "] : ";
					std::for_each(packet_buffer.begin(), packet_buffer.end(),
						[](const auto& p) { std::cout << unsigned(p.package_number()) << ' '; });
					std::cout << std::endl;

					std::cout
					//	<< std::fixed << packet.timecode()
						<< ' ' << packet_buffer.front() << std::endl;
				}
			}


			socket_.async_receive_from(
				boost::asio::buffer(data_, max_length), sender_endpoint_,
				boost::bind(&receiver::handle_receive_from, this,
					boost::asio::placeholders::error,
					boost::asio::placeholders::bytes_transferred));
		}
	}

private:
	boost::asio::ip::udp::socket socket_;
	boost::asio::ip::udp::endpoint sender_endpoint_;
	enum { max_length = 1024 };
	uint8_t data_[max_length];
};

int main(int argc, char* argv[])
{
	try
	{
		std::string listen_address("0.0.0.0");
		std::string multicast_address("224.0.0.2");

		if (argc != 3)
		{
			std::cerr << "Usage: receiver <listen_address> <multicast_address>\n";
			std::cerr << "  For IPv4, try:\n";
			std::cerr << "    receiver 0.0.0.0 239.255.0.1\n";
			std::cerr << "  For IPv6, try:\n";
			std::cerr << "    receiver 0::0 ff31::8000:1234\n";
		}
		else
		{
			listen_address = argv[1];
			multicast_address = argv[2];
		}

		boost::asio::io_service io_service;
		receiver r(io_service,
			boost::asio::ip::address::from_string(listen_address),
			boost::asio::ip::address::from_string(multicast_address));
		io_service.run();
	}
	catch (std::exception& e)
	{
		std::cerr << "Exception: " << e.what() << "\n";
	}

	return 0;
}

#else


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

				//std::cout << "Buffer [" << packet_buffer.size() << "] : ";
				//std::for_each(packet_buffer.begin(), packet_buffer.end(),
				//	[](const auto& p) { std::cout << unsigned(p.package_number()) << ' '; });
				//std::cout << std::endl;

				std::cout
				//	<< std::fixed << packet.timecode()
					<< ' ' << packet_buffer.front() << std::endl;
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
#endif