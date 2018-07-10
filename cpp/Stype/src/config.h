#pragma once

#include <string>
#include <fstream>


namespace stype
{
	struct config
	{

		void save(const std::string& filename)
		{
			std::ofstream out(filename, std::ios::out);
			if (out.is_open())
			{
				out
					<< "enabled " << enabled << '\n'
					<< "local_ip " << local_ip << '\n'
					<< "remote_ip " << remote_ip << '\n'
					<< "multicast " << multicast << '\n'
					<< "port " << port << '\n'
					<< "delay " << delay << '\n';
				out.close();
			}
			else
			{
				throw("Error: Could not open file for writing");
			}
		}

		void load(const std::string& filename)
		{
			std::ifstream in(filename, std::ios::in);
			if (in.is_open())
			{
				std::string name;
				in
					>> name >> enabled
					>> name >> local_ip
					>> name >> remote_ip
					>> name >> multicast
					>> name >> port
					>> name >> delay;
				in.close();
			}
			else
			{
				throw("Error: Could not open file for reading");
			}
		}



		bool enabled = true;
		std::string local_ip = "0.0.0.0";
		std::string remote_ip = "0.0.0.0";
		bool multicast = false;
		uint32_t port = 6301;
		uint16_t delay = 0;
	};

	
}
