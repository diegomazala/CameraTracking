/*
 * File:       read_file.h
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
#ifdef _XCITO_READ_FILE_


#include "trk_file.h"
#include "read_struct.h"

#include <stdio.h>
#include <unistd.h>



int
main (int numArgs, char* arg [])
   {
   const char*          fileName;
   char                 timeCode [32];
   trkHandle            handle;
   trkError             error;
   trkCameraConstants   c;
   trkCameraParams      p;

   if (numArgs != 2)
      {
      fprintf (stderr, "Usage: %s <file>\n", arg [0]);
      return 1;
      }
   fileName = arg [1];
   error = trkFileOpenRead (fileName, timeCode, &c, &handle);
   if (error != trkOK)
      {
      fprintf (stderr, "trkFileOpenRead: %s\n", trkErrorMessage (error));
      return 2;
      }
   printf ("Time code: <%s>\n\n", timeCode);
   printCameraConstants (&c);

   do
      {
      error = trkFileReadParams (handle, &p);
      if (error == trkOK)
         printCameraParams (&p);
      else if (error != trkUnavailable)
         {
         fprintf (stderr, "trkFileReadParams: %s\n",
                          trkErrorMessage (error));
         trkFileClose (handle);
         return 3;
         }
      }
   while (error != trkUnavailable);

   trkFileClose (handle);
   return 0;
   }

#endif