/*
 * File:       write_file.h
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
#ifdef _XCITO_WRITE_FILE_


#include "trk_file.h"
#include "write_struct.h"

#include <stdio.h>
#include <time.h>



static int
usage (const char* programName)
   {
   fprintf (stderr, "Usage: %s <file> { <format_option> }\n%s",
                    programName, allOptionsString ());
   return 1;
   }



#define TestParams         250



static void
getTimeCode (char* s)
   {
   time_t            seconds = 0;
   struct tm*        t;

   time (&seconds);
   t = localtime (&seconds);

   s [2] = s [5] = s [8] = s [11] = ':';
   s [ 0] = '0' + t->tm_hour / 10;
   s [ 1] = '0' + t->tm_hour % 10;
   s [ 3] = '0' + t->tm_min / 10;
   s [ 4] = '0' + t->tm_min % 10;
   s [ 6] = '0' + t->tm_sec / 10;
   s [ 7] = '0' + t->tm_sec % 10;
   s [ 9] = '0';
   s [10] = '0';
   s [12] = '1';
   s [13] = '\0';
   }



int
main (int numArgs, char* arg [])
   {
   const char* fileName;
   char        timeCode [32];
   trkHandle   handle;
   trkError    error;
   int         i, j;
   double      position;

   if (numArgs < 2)   return usage (arg [0]);
   fileName = arg [1];
   if (!setFormat (numArgs - 2, &(arg [2])))
      return usage (arg [0]);
   getTimeCode (timeCode);

   error = trkFileOpenWrite (fileName, timeCode, getCameraConstants (),
                             &handle);
   if (error != trkOK)
      {
      fprintf (stderr, "trkFileOpenWrite: %s\n", trkErrorMessage (error));
      return 2;
      }

   for (i = 1; i <= 9; ++i)
      {
      for (j = 0; j <= TestParams; ++j)
         {
         position = ((double) j) / ((double) TestParams);
         trkFileWriteParams (handle, getCameraParams (i, position));
         }
      }

   trkFileClose (handle);
   return 0;
   }


#endif