#include <iostream>
#include <windows.h>
#include <sstream>

#define DllExport __declspec (dllexport)

extern "C"
{
	DllExport void MrmcPlay()
	{
		MessageBox(NULL, "Play Button\n", NULL, NULL);
	}

	DllExport void MrmcStop()
	{
		MessageBox(NULL, "Stop Button\n", NULL, NULL);
	}

	DllExport void MrmcGoto(int frame)
	{
		std::stringstream str;
		str << "Go to frame: " << frame << std::endl;

		MessageBox(NULL, str.str().c_str(), NULL, NULL);
	}
}
	