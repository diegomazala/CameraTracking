
#include <ctime>
#include <iostream>
#include <string>
#include <thread>
#include <boost/array.hpp>
#include <boost/asio.hpp>
#include <boost/bind.hpp>
#include <conio.h> 


#include <iostream>
#include <boost/array.hpp>
#include <boost/asio.hpp>
#include <mutex>

class udp_server
{
public:
	boost::asio::io_service io_service;
	boost::asio::ip::udp::socket socket;
	boost::asio::ip::udp::endpoint remote_endpoint;
	bool broadcast;


	udp_server(boost::asio::io_service& io_service, const std::string ip_address, uint16_t port, bool broadcast = false) :
		socket(io_service, boost::asio::ip::udp::v4()),
		remote_endpoint(boost::asio::ip::address::from_string(ip_address), port),
		broadcast(broadcast)
	{
		if (broadcast)
		{
			boost::asio::socket_base::broadcast option(true);
			socket.set_option(option);
		}

		// create a ip:port destination address
		remote_endpoint = boost::asio::ip::udp::endpoint(
			boost::asio::ip::address::from_string(ip_address), port);

		std::cout << "Send to " << remote_endpoint << std::endl;
	}

	void start_send()
	{
		// send async data
		socket.async_send_to(boost::asio::buffer("message"), remote_endpoint,
			boost::bind(&udp_server::handle_send, this, boost::asio::placeholders::error, boost::asio::placeholders::bytes_transferred));
	}

	void handle_send(const boost::system::error_code& error, std::size_t bytes_transferred)
	{
		std::cout << " async_send_to return " << error << ": " << bytes_transferred << " transmitted" << std::endl;

		boost::asio::deadline_timer t(io_service, boost::posix_time::seconds(1));
		t.wait();

		float numbers[] = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		
		socket.async_send_to(
			boost::asio::buffer(numbers),
			remote_endpoint,
			boost::bind(&udp_server::handle_send, this, boost::asio::placeholders::error, boost::asio::placeholders::bytes_transferred));
	}
};


static boost::asio::io_service io_service;
static std::mutex mutex;


void run_udp_server(const std::string& ip_address, uint16_t port)
{
	udp_server updclient(io_service, ip_address, port);
	updclient.start_send();

	io_service.run();
}


int main(int argc, char *argv[])
{
	std::string ip_address = "127.0.0.1";
	uint16_t port = 10002;

	if (argc > 2)
	{
		ip_address = argv[1];
		port = atoi(argv[2]);
	}
	else
	{
		std::cerr << "Usage  : app <ip_addrres> <port>" << std::endl;
		std::cerr << "Default: app 127.0.0.1 10002" << std::endl;
	}

	std::thread udp_receive_thread(run_udp_server, ip_address, port);

	while (getch() != 27) {}	// wait for escape key

	mutex.lock();
	{
		io_service.stop();
	}
	mutex.unlock();

	udp_receive_thread.join();
	
	return EXIT_SUCCESS;
}


