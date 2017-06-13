/*
 * File:       write_net.h
 * Version:    1.1
 *
 * DMC - Digital Media Consulting & Services
 * Technopark der GMD
 * Rathausallee 10
 * D-53757 Sankt Augustin
 * Germany
 *
 * phone:   +49 2241 / 14 35 40
 * fax:     +49 2241 / 14 29 19
 *
 * E-mail:  support@dmcs.de
 *
 * See the file "dmc_tracking.txt" for explanations.
 *
 */


#include "trk_net.h"
#include "write_struct.h"

#include <stdio.h>
#include <stdlib.h>

#ifdef __linux
	#include <unistd.h> // arquivo de prove acesso a API do sistema POSIX - Base da especificação do UNIX.
	#include <sys/types.h>
	#include <sys/time.h>
#endif

#include <string.h>

#ifdef _WIN32 || _WIN64
	#include <windows.h>  
#endif




static int usage (const char* programName)
{
   fprintf (stderr, "Usage: %s <host> <port> [ binary ] "
                    "{ <format_option> }\n%s",
                    programName, allOptionsString ());
   return 1;
}



#define ParamsPerSecond     50
#define TestParams         250



static void nap ()
{
#ifdef __linux
	struct timeval timeout;
	timeout.tv_sec  = 0;
	timeout.tv_usec = 1000000L / ParamsPerSecond;
	select (0, (fd_set*) 0, (fd_set*) 0, (fd_set*) 0, &timeout);
	/* makes process sleep for 1/ParamsPerSecond seconds */
#endif

#ifdef _WIN32 || _WIN64
	//Sleep(1);
#endif
}



int main (int numArgs, char* arg [])
{
   const char* host;
   int         port;
   int         binary = 0;
   int         firstOption;
   trkHandle   handle;
   trkError    error;
   int         i, j, a;
   double      position;

   const trkCameraConstants *cam_consts;
   const trkCameraParams *cam_params; 
   
   // Variables used to check the available protocols
   int					iResult = 0;
   WSADATA				wsaData;


   if (numArgs < 3)   return usage (arg [0]);
   host = arg [1];
   port = atoi (arg [2]);
   if (port < 0)   return usage (arg [0]);
   firstOption = 3;
   if (numArgs > 3 && strcmp (arg [3], "binary") == 0)
   {
      binary = 1;
      ++firstOption;
   }
   if (!setFormat (numArgs - firstOption, &(arg [firstOption])))
      return usage (arg [0]);


   // Initialize Winsock
#ifdef _WIN32 || _WIN64
   iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
   if (iResult != 0) 
   {
	   printf("WSAStartup failed: %d\n", iResult);        
   }  
#endif

   error = trkNetOpenWrite (host, port, binary, &handle);
   if (error != trkOK)
   {
      fprintf (stderr, "trkNetOpenWrite: %s\n", trkErrorMessage (error));
      return 2;
   }

   cam_consts = getCameraConstants();
   error = trkNetWriteConstants (handle, cam_consts);
   if (error != trkOK)
      {
      fprintf (stderr, "trkNetWriteConstants: %s\n",
                       trkErrorMessage (error));
      trkNetClose (handle);
      return 2;
      }

   for (;;)
   {
	   for (i = 1; i <= 9; ++i)
	   {
		  for (j = 0; j <= TestParams; ++j)
		  {
			 //trkNetWriteConstants (handle, cam_consts);

			 position = ((double) j) / ((double) TestParams);
			 cam_params = getCameraParams (i, position);
			 error = trkNetWriteParams (handle, cam_params);
			 if (error != trkOK)
			 {
				fprintf (stderr, "trkNetWriteParams: %s\n",
								 trkErrorMessage (error));
				trkNetClose (handle);
				return 2;
			 }
			 //printCameraConstants(cam_consts);
			 printCameraParams (cam_params);
			 
#ifdef __linux__
			 sleep (1);
#else
			 Sleep (16);
#endif
		  }
		  /* do nothing for one second */
		  for (j = 0; j < ParamsPerSecond; ++j)
		  {
			 //trkNetWriteConstants (handle, cam_consts);

			 cam_params = getCameraParams (i, 0.0);
			 error = trkNetWriteParams (handle, cam_params);
			 if (error != trkOK)
			 {
				fprintf (stderr, "trkNetWriteParams: %s\n",
								 trkErrorMessage (error));
				trkNetClose (handle);
				return 2;
			 }
			 //printCameraConstants(cam_consts);
			 printCameraParams (cam_params);
			 
#ifdef __linux__
			 sleep (1);
#else
			 Sleep (16);
#endif
		  }
	   }
   }

   trkNetClose (handle);

#ifdef _WIN32 || _WIN64
   WSACleanup();
#endif

   return 0;
}
