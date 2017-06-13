/*
 * File:       read_mem.h
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
#ifdef _XCITO_READ_MEM_


#include "trk_mem.h"
#include "read_struct.h"

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>




int
main (int numArgs, char* arg [])
   {
   int                  slot;
   trkHandle            handle;
   trkError             error;
   int                  count;
   trkCameraConstants   c;
   trkCameraParams      p;

   if (numArgs != 2)
      {
      fprintf (stderr, "Usage: %s <slot>\n", arg [0]);
      return 1;
      }
   slot = atoi (arg [1]);
   error = trkMemOpen (slot, &handle);
   if (error != trkOK)
      {
      fprintf (stderr, "trkMemOpen: %s\n", trkErrorMessage (error));
      return 2;
      }

   for (count = 0; count < 30; ++count)
      {
      error = trkMemReadConstants (handle, &c);
      if (error == trkOK)
         printCameraConstants (&c);
      else if (error != trkUnavailable)
         {
         fprintf (stderr, "trkMemReadConstants: %s\n",
                          trkErrorMessage (error));
         trkMemClose (handle);
         return 3;
         }

      error = trkMemReadParams (handle, &p);
      if (error == trkOK)
         printCameraParams (&p);
      else if (error != trkUnavailable)
         {
         fprintf (stderr, "trkMemReadParams: %s\n",
                          trkErrorMessage (error));
         trkMemClose (handle);
         return 3;
         }
      else
         printf ("No new camera parameters.\n\n");

      sleep (1);
      }

   trkMemClose (handle);
   return 0;
   }

#endif