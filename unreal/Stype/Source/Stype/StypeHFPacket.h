#pragma once
#include <vector>
#include <algorithm>


struct buffer_index
{
	static const uint8_t header = 0;
	static const uint8_t command = 1;
	static const uint8_t timecode = 2;
	static const uint8_t packagenumber = 5;
	static const uint8_t x = 6;
	static const uint8_t y = 10;
	static const uint8_t z = 14;
	static const uint8_t pan = 18;
	static const uint8_t tilt = 22;
	static const uint8_t roll = 26;
	static const uint8_t fovx = 30;
	static const uint8_t aspectratio = 34;
	static const uint8_t focus = 38;
	static const uint8_t zoom = 42;
	static const uint8_t k1 = 46;
	static const uint8_t k2 = 50;
	static const uint8_t centerx = 54;
	static const uint8_t centery = 58;
	static const uint8_t chipwidth = 62;
	static const uint8_t checksum = 66;
	static const uint8_t total = 67;
};

struct packet_index
{
	static const uint8_t header = 0;
	static const uint8_t command = 1;
	static const uint8_t timecode_0 = 2;
	static const uint8_t timecode_1 = 3;
	static const uint8_t timecode_2 = 4;
	static const uint8_t packagenumber = 5;
	static const uint8_t x = 0;
	static const uint8_t y = 1;
	static const uint8_t z = 2;
	static const uint8_t pan = 3;
	static const uint8_t tilt = 4;
	static const uint8_t roll = 5;
	static const uint8_t fovx = 6;
	static const uint8_t aspectratio = 7;
	static const uint8_t focus = 8;
	static const uint8_t zoom = 9;
	static const uint8_t k1 = 10;
	static const uint8_t k2 = 11;
	static const uint8_t centerx = 12;
	static const uint8_t centery = 13;
	static const uint8_t chipwidth = 14;
};

struct StypeHFPacket
{
	StypeHFPacket() = default;

	StypeHFPacket(uint8_t* buffer)
	{
		FromBuffer(buffer);
	}

	StypeHFPacket& operator=(const StypeHFPacket& other) = default;

	void FromBuffer(uint8_t* buffer)
	{
		std::copy(
			(&buffer[buffer_index::header]),
			(&buffer[buffer_index::header]) + info.size(),
			info.begin());

		std::copy(
			reinterpret_cast<float*>(&buffer[buffer_index::x]),
			reinterpret_cast<float*>(&buffer[buffer_index::x]) + params.size(),
			params.begin());

		is_valid = Checksum(buffer);
	}

	bool IsValid() const
	{
		return is_valid;
	}

	bool Checksum(uint8_t* buffer) const
	{
		uint8_t check = 0;
		for (uint8_t x = 0; x < buffer_index::checksum; ++x)
			check += buffer[x];
		return check == buffer[buffer_index::checksum];
	}

	uint8_t header() const { return info[packet_index::header]; }
	void header(uint8_t val) { info[packet_index::header] = val; }

	uint8_t command() const { return info[packet_index::command]; }
	void command(uint8_t val) { info[packet_index::command] = val; }

	uint8_t package_number() const { return info[packet_index::packagenumber]; }
	void package_number(uint8_t val) { info[packet_index::packagenumber] = val; }

	uint32_t timecode() const { return (info[packet_index::timecode_2] << 16) | (info[packet_index::timecode_1] << 8) | (info[packet_index::timecode_0] << 0); }
	void timecode(uint32_t val)
	{
		info[packet_index::timecode_0] = ((uint8_t*)&val)[0];
		info[packet_index::timecode_1] = ((uint8_t*)&val)[1];
		info[packet_index::timecode_2] = ((uint8_t*)&val)[2];
	}

	float x() const { return params[packet_index::x]; }
	void x(float val) { params[packet_index::x] = val; }

	float y() const { return params[packet_index::y]; }
	void y(float val) { params[packet_index::y] = val; }

	float z() const { return params[packet_index::z]; }
	void z(float val) { params[packet_index::z] = val; }

	float pan() const { return params[packet_index::pan]; }
	void pan(float val) { params[packet_index::pan] = val; }

	float tilt() const { return params[packet_index::tilt]; }
	void til(float val) { params[packet_index::tilt] = val; }

	float roll() const { return params[packet_index::roll]; }
	void roll(float val) { params[packet_index::roll] = val; }

	float fovx() const { return params[packet_index::fovx]; }
	void fovx(float val) { params[packet_index::fovx] = val; }

	float focus() const { return params[packet_index::focus]; }
	void focus(float val) { params[packet_index::focus] = val; }

	float zoom() const { return params[packet_index::zoom]; }
	void zoom(float val) { params[packet_index::zoom] = val; }

	float aspect_ratio() const { return params[packet_index::aspectratio]; }
	void aspect_ratio(float val) { params[packet_index::aspectratio] = val; }

	float k1() const { return params[packet_index::k1]; }
	void k1(float val) { params[packet_index::k1] = val; }

	float k2() const { return params[packet_index::k2]; }
	void k2(float val) { params[packet_index::k2] = val; }

	float center_x() const { return params[packet_index::centerx]; }
	void center_x(float val) { params[packet_index::centerx] = val; }

	float center_y() const { return params[packet_index::centery]; }
	void center_y(float val) { params[packet_index::centery] = val; }

	float chip_width() const { return params[packet_index::chipwidth]; }
	void chip_width(float val) { params[packet_index::chipwidth] = val; }


private:
	std::vector<uint8_t>	info = std::vector<uint8_t>((buffer_index::x - buffer_index::header) / sizeof(uint8_t));	// 6 elems
	std::vector<float>		params = std::vector<float>((buffer_index::checksum - buffer_index::x) / sizeof(float));	// 15 elems
	bool					is_valid = false;	// holds a flag to indicate if this packet was checksumed successfully
};
