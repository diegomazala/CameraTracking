/*
 * File:       read_net.h
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

#include "XcitoDefs.h"
#ifdef _XCITO_READ_NET_

// windows tests
//#ifdef _WIN32 || _WIN64
//	#include <initguid.h>  
//#endif

#ifdef __linux
	#include <unistd.h> // arquivo de prove acesso a API do sistema POSIX - Base da especificação do UNIX.
#endif

#ifdef _WIN32 || _WIN64
	#include <windows.h>
#endif

#include "trk_net.h"
#include "read_struct.h" 
#include "trk_struct.h"

#include <stdio.h>
#include <stdlib.h>



int
main (int numArgs, char* arg [])
   {
   int                  port;
   trkHandle            handle;
   trkError             error;
   int                  count;
   trkCameraConstants   c;
   trkCameraParams      p;

   if (numArgs != 2)
      {
      fprintf (stderr, "Usage: %s <port>\n", arg [0]);
      return 1;
      }
   port = atoi (arg [1]);
   error = trkNetOpenRead (port, &handle, 1);
            
   //printf(" \n checking handle file descriotor: %d ", handle);
      
   if (error != trkOK)
      {
      fprintf (stderr, "(first print) trkNetOpen: %s\n", trkErrorMessage (error));
      return 2;
      }
   
   //for (count = 0; count < 30; ++count)
   for (;;)
      {
		  
      error = trkNetReadConstants (handle, &c);
      if (error == trkOK)
         printCameraConstants (&c);
      else if (error != trkUnavailable)
         {
         fprintf (stderr, "trkNetReadConstants (second print): %s\n",
                          trkErrorMessage (error));
         trkNetClose (handle);
         return 3;
         }

      error = trkNetReadParams (handle, &p);
      if (error == trkOK)
         printCameraParams (&p);
      else if (error != trkUnavailable)
         {
         fprintf (stderr, "trkNetReadParams (third print): %s\n",
                          trkErrorMessage (error));
         trkNetClose (handle);
         return 3;
         }
      else
         printf ("No new camera parameters.\n\n");
	  
	  #ifdef __linux__
		sleep (1);
      #else
		Sleep (33);
	  #endif
      }

   trkNetClose (handle);
   return 0;
   }

#endif