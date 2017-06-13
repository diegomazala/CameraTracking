/*
 * File:       trk_file.c
 * Version:    1.2
 *
 * Xync
 * Technopark
 * Rathausallee 10
 * D-53757 Sankt Augustin
 * Germany
 *
 * phone:   +49 2241 / 14 35 35
 * fax:     +49 2241 / 14 35 36
 *
 * E-mail:  support@xync.com
 * WWW:     http://www.xync.com
 *
 * See the file "X-cito_interface.ps" for explanations.
 *
 */



#include "trk_file.h"

#include <stdio.h>
#include <ctype.h>
#include <string.h>



#define trkFileMagic       "DMC01"

#define trkMaxLineLength   4096



static int
getLine (FILE* file, char* line, char** lineStart)
   {
   char*    start;

   do
      {
      fgets (line, trkMaxLineLength - 1, file);
      if (feof (file))     return  0;
      if (ferror (file))   return -1;
      /* find first non-whitespace character */
      for (start = line; isspace (*start); ++start)
         { }
      /* if line is empty or contains comment then read another line */
      }
   while (*start == '\0' || *start == '#');
   *lineStart = start;
   return 1;
   }



static int
removeTrailingBlanks (char* string)
   {
   int      last;

   for (last = strlen (string) - 1; last >= 0; --last)
      if (!isspace (string [last]))   break;
   string [++last] = '\0';
   return last;
   }



trkError
trkFileReadParams (trkHandle handle, trkCameraParams* cameraParams)
   {
   char     line [trkMaxLineLength];
   char*    start = (char*) 0;
   int      result;

   if (handle == (trkHandle) 0)   return trkInvalidHandle;
   result = getLine ((FILE*) handle, line, &start);
   if (result == 0)   return trkUnavailable;
   if (result < 0 || !trkStringToParams (start, cameraParams))
      return trkFailed;
   return trkOK;
   }



#ifndef ONLY_READ

trkError
trkFileWriteParams (trkHandle handle, const trkCameraParams* cameraParams)
   {
   char     line [trkMaxLineLength];

   if (handle == (trkHandle) 0)   return trkInvalidHandle;
   trkParamsToString (cameraParams, line);
   fprintf ((FILE*) handle, "%s\n", line);
   return ferror ((FILE*) handle) ? trkFailed : trkOK;
   }

#endif



trkError
trkFileOpenRead (const char* fileName,
                 char* timeCode,
                 trkCameraConstants* constants,
                 trkHandle* handle)
   {
   FILE*    file;
   char     line [trkMaxLineLength];
   char*    start = (char*) 0;
   int      result;

   file = fopen (fileName, "r");
   if (file == (FILE*) 0)   return trkFailed;
   result = getLine (file, line, &start);
   if (result > 0)
      {
      /* check if first line is equal to trkFileMagic */
      removeTrailingBlanks (start);
      if (strcmp (start, trkFileMagic) != 0)
         { fclose (file);   return trkInvalidVersion; }
      result = getLine (file, line, &start);
      }
   if (result > 0)
      {
      /* interpret input line as time code */
      if (removeTrailingBlanks (start) <= 15)
         {
         strcpy (timeCode, start);
         result = getLine (file, line, &start);
         }
      else
         result = 0;   /* time code string too long */
      }
   if (result > 0)
      {
      /* convert input line to camera constants */
      if (!trkStringToConstants (start, constants))   result = 0;
      }
   if (result <= 0)
      {
      fclose (file);
      return trkFailed;
      }
   *handle = (trkHandle) file;
   return trkOK;
   }



#ifndef ONLY_READ

trkError
trkFileOpenWrite (const char* fileName,
                  const char* timeCode,
                  const trkCameraConstants* constants,
                  trkHandle* handle)
   {
   FILE*    file;
   char     line [trkMaxLineLength];

   file = fopen (fileName, "w");
   if (file == (FILE*) 0)   return trkFailed;
   trkConstantsToString (constants, line);
   fprintf (file, "%s\n"
                  "# Xync Camera Parameters File\n"
                  "# start time code:\n"
                  "%s\n"
                  "# camera constants:\n"
                  "%s\n"
                  "# list of camera parameters:\n",
                  trkFileMagic, timeCode, line);
   if (ferror (file))
      {
      fclose (file);
      return trkFailed;
      }
   *handle = (trkHandle) file;
   return trkOK;
   }

#endif



trkError
trkFileClose (trkHandle handle)
   {
   if (handle == (trkHandle) 0)   return trkInvalidHandle;
   return (fclose ((FILE*) handle) == 0) ? trkOK : trkFailed;
   }

