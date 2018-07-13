#pragma once

#include <vector>

template <typename T, const int Size>
class CircularBuffer
{
public:

	CircularBuffer() = default;

	void put(const T& value)
	{
		buffer[index] = value;
		index = (index + 1) % buffer.size();
	}

	std::size_t size() const
	{
		return buffer.size();
	}

	T get() const
	{
		return buffer.at(index);
	}

	T get(std::size_t idx) const
	{
		return buffer.at((index + idx) % buffer.size());
	}

	const T& operator[](std::size_t idx) const 
	{ 
		return buffer.at((index + idx) % buffer.size()); 
	}

private:
	std::size_t index = 0;
	std::vector<T> buffer = std::vector<T>(Size);
};
