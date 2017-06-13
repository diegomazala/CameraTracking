/*
 * File:       write_mem.h
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
#ifdef _XCITO_WRITE_MEM_


#include "trk_mem.h"
#include "write_struct.h"

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/time.h>



static int
usage (const char* programName)
   {
   fprintf (stderr, "Usage: %s <slot> { <format_option> }\n%s",
                    programName, allOptionsString ());
   return 1;
   }



#define ParamsPerSecond     50
#define TestParams         250



static void
nap ()
   {
   struct timeval timeout;
   timeout.tv_sec  = 0;
   timeout.tv_usec = 1000000L / ParamsPerSecond;
   select (0, (fd_set*) 0, (fd_set*) 0, (fd_set*) 0, &timeout);
   /* makes process sleep for 1/ParamsPerSecond seconds */
   }



int
main (int numArgs, char* arg [])
   {
   int         slot;
   trkHandle   handle;
   trkError    error;
   int         i, j;
   double      position;

   if (numArgs < 2)   return usage (arg [0]);
   slot = atoi (arg [1]);
   if (slot < 0)   return usage (arg [0]);
   if (!setFormat (numArgs - 2, &(arg [2])))
      return usage (arg [0]);

   error = trkMemOpen (slot, &handle);
   if (error != trkOK)
      {
      fprintf (stderr, "trkMemOpen: %s\n", trkErrorMessage (error));
      return 2;
      }

   error = trkMemWriteConstants (handle, getCameraConstants ());
   if (error != trkOK)
      {
      fprintf (stderr, "trkMemWriteConstants: %s\n",
                       trkErrorMessage (error));
      trkMemClose (handle);
      return 2;
      }

   for (i = 1; i <= 9; ++i)
      {
      for (j = 0; j <= TestParams; ++j)
         {
         position = ((double) j) / ((double) TestParams);
         trkMemWriteParams (handle, getCameraParams (i, position));
         nap ();
         }
      /* do nothing for one second */
      for (j = 0; j < ParamsPerSecond; ++j)
         {
         trkMemWriteParams (handle, getCameraParams (i, 0.0));
         nap ();
         }
      }

   trkMemClose (handle);
   return 0;
   }

#endif