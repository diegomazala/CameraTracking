/*
 * File:       trk_struct.c
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



#include "trk_struct.h"

#include <ctype.h>
#include <string.h>
#include <stdio.h>



const char*
trkErrorMessage (trkError error)
   {
   switch (error)
      {
      case trkOK:                return "OK";
      case trkFailed:            return "Operation failed !";
      case trkInvalidHandle:     return "Invalid handle !";
      case trkInvalidVersion:    return "Invalid version !";
      case trkUnavailable:       return "Data unavailable !";
      }
   return "Unknown error !";
   }



static int
skipWords (const char** string, int numWords)
   {
   const char* s = *string;
   int wordCount;

   for (wordCount = 0; wordCount < numWords; ++wordCount)
      {
      while (isspace (*s))   ++s;
      if (*s == '\0')   break;
      while (*s != '\0' && !isspace (*s))   ++s;
      }
   *string = s;
   return wordCount == numWords;
   }



#ifndef ONLY_READ

int
trkParamsToString (const trkCameraParams* p, char* s)
   {
   int n = 0;

   if (p->id)   n = sprintf (s, "I%u ", p->id);
   n += sprintf (s + n, "%X", p->format);
   if (p->format & trkEuler)
      n += sprintf (s + n, " %g %g %g %g %g %g",
                           p->t.e.x,   p->t.e.y,    p->t.e.z,
                           p->t.e.pan, p->t.e.tilt, p->t.e.roll);
   else
      n += sprintf (s + n,
           " %.7e %.7e %.7e %.7e %.7e %.7e %.7e %.7e %.7e %.7e %.7e %.7e",
                           p->t.m [0] [0], p->t.m [0] [1], p->t.m [0] [2],
                           p->t.m [1] [0], p->t.m [1] [1], p->t.m [1] [2],
                           p->t.m [2] [0], p->t.m [2] [1], p->t.m [2] [2],
                           p->t.m [3] [0], p->t.m [3] [1], p->t.m [3] [2]);
   n += sprintf (s + n, " %g %e %e %e %e %g %g %lu",
                        p->fov, p->centerX, p->centerY, p->k1, p->k2,
                        p->focdist, p->aperture, p->counter);
   return n;
   }

#endif



int
trkStringToParams (const char* s, trkCameraParams* p)
   {
   int n;

   n = sscanf (s, "I%u", &p->id);
   if (n != 1)
      p->id = 0;
   else if (!skipWords (&s, 1))
      return 0;
   n = sscanf (s, "%x", &p->format);
   if (n != 1 || !skipWords (&s, 1))   return 0;
   if (p->format & trkEuler)
      {
      n = sscanf (s, "%lg %lg %lg %lg %lg %lg",
                     &p->t.e.x,   &p->t.e.y,    &p->t.e.z,
                     &p->t.e.pan, &p->t.e.tilt, &p->t.e.roll);
      if (n != 6 || !skipWords (&s, 6))   return 0;
      }
   else
      {
      n = sscanf (s, "%lg %lg %lg %lg %lg %lg %lg %lg %lg %lg %lg %lg",
                     &p->t.m [0] [0], &p->t.m [0] [1], &p->t.m [0] [2],
                     &p->t.m [1] [0], &p->t.m [1] [1], &p->t.m [1] [2],
                     &p->t.m [2] [0], &p->t.m [2] [1], &p->t.m [2] [2],
                     &p->t.m [3] [0], &p->t.m [3] [1], &p->t.m [3] [2]);
      if (n != 12 || !skipWords (&s, 12))   return 0;
      /* assume that bottom row of matrix contains 0 0 0 1 */
      p->t.m [0] [3] = p->t.m [1] [3] = p->t.m [2] [3] = 0.0;
      p->t.m [3] [3] = 1.0;
      }
   n = sscanf (s, "%lg %lg %lg %lg %lg %lg %lg %lu",
                  &p->fov, &p->centerX, &p->centerY, &p->k1, &p->k2,
                  &p->focdist, &p->aperture, &p->counter);
   if (n != 8 || !skipWords (&s, 8))   return 0;
   while (isspace (*s))   ++s;
   if (*s != '\0')   return 0;
   return 1;
   }



#ifndef ONLY_READ

int
trkConstantsToString (const trkCameraConstants* c, char* s)
   {
   int n = 0;
   if (c->id)   n = sprintf (s, "I%u ", c->id);
   return n + sprintf (s + n, "%i %i %i %i %i %i %g %g %g %g",
                      c->imageWidth,    c->imageHeight,
                      c->blankLeft,     c->blankRight,
                      c->blankTop,      c->blankBottom,
                      c->chipWidth,     c->chipHeight,
                      c->fakeChipWidth, c->fakeChipHeight);
   }

#endif



int
trkStringToConstants (const char* s, trkCameraConstants* c)
   {
   int n;

   n = sscanf (s, "I%u", &c->id);
   if (n != 1)
      c->id = 0;
   else if (!skipWords (&s, 1))
      return 0;
   n = sscanf (s, "%i %i %i %i %i %i %lg %lg %lg %lg",
                  &c->imageWidth,    &c->imageHeight,
                  &c->blankLeft,     &c->blankRight,
                  &c->blankTop,      &c->blankBottom,
                  &c->chipWidth,     &c->chipHeight,
                  &c->fakeChipWidth, &c->fakeChipHeight);
   if (n != 10 || !skipWords (&s, 10))   return 0;
   while (isspace (*s))   ++s;
   if (*s != '\0')   return 0;
   return 1;
   }



typedef struct
   {
   unsigned int      bitmask;
   const char*       string;
   }                             Option;



static const Option     options [] = {
                           { trkMatrix,         "matrix" },
                           { trkEuler,          "euler" },
                           { trkCameraToStudio, "camera_to_studio" },
                           { trkStudioToCamera, "studio_to_camera" },
                           { trkCameraZ_Up,     "camera_z_up" },
                           { trkCameraY_Up,     "camera_y_up" },
                           { trkStudioZ_Up,     "studio_z_up" },
                           { trkStudioY_Up,     "studio_y_up" },
                           { trkImageDistance,  "image_distance" },
                           { trkFieldOfView,    "field_of_view" },
                           { trkHorizontal,     "horizontal" },
                           { trkVertical,       "vertical" },
                           { trkDiagonal,       "diagonal" },
                           { trkConsiderBlank,  "consider_blank" },
                           { trkIgnoreBlank,    "ignore_blank" },
                           { trkAdjustAspect,   "adjust_aspect" },
                           { trkKeepAspect,     "keep_aspect" },
                           { trkShiftOnChip,    "shift_on_chip" },
                           { trkShiftInPixels,  "shift_in_pixels" },
                           { 0,                 "" } };



int
trkFormatToString (unsigned format, char* s)
   {
   if (format & trkEuler)
      strcpy (s, "euler");
   else
      {
      strcpy (s, "matrix");
      if (format & trkStudioToCamera)
         strcat (s, " studio_to_camera");
      else
         strcat (s, " camera_to_studio");
      if (format & trkCameraY_Up)
         strcat (s, " camera_y_up");
      else
         strcat (s, " camera_z_up");
      }
   if (format & trkStudioY_Up)
      strcat (s, " studio_y_up");
   else
      strcat (s, " studio_z_up");
   if (format & trkFieldOfView)
      {
      strcat (s, " field_of_view");
      if (format & trkVertical)
         strcat (s, " vertical");
      else if (format & trkDiagonal)
         strcat (s, " diagonal");
      else
         strcat (s, " horizontal");
      }
   else
      strcat (s, " image_distance");
   if (format & trkIgnoreBlank)
      {
      strcat (s, " ignore_blank");
      if (format & trkKeepAspect)
         strcat (s, " keep_aspect");
      else
         strcat (s, " adjust_aspect");
      }
   else
      strcat (s, " consider_blank");
   if (format & trkShiftInPixels)
      strcat (s, " shift_in_pixels");
   else
      strcat (s, " shift_on_chip");
   return strlen (s);
   }



#ifndef ONLY_READ

int
trkStringToFormat (const char* s, unsigned* format)
   {
   int            found;
   const Option*  testOption;
   const char*    testString;
   const char*    t;
   char           testChar;

   *format = 0;
   while (isspace (*s))   ++s;
   while (*s != '\0')
      {
      found = 0;
      for (testOption = options;
           testOption->string [0] != '\0';
           ++testOption)
         {
         testString = testOption->string;
         t = s;
         testChar = tolower (*t);
         while (testChar == *testString)
            {
            if (testChar == '\0')
               {
               found = 1;
               break;
               }
            ++t;
            testChar = isspace (*t) ? '\0' : tolower (*t);
            ++testString;
            }
         if (found)
            {
            *format |= testOption->bitmask;
            break;
            }
         }
      if (!found)   return 0;
      while (*s != '\0' && !isspace (*s))   ++s;
      while (isspace (*s))   ++s;
      }
   return 1;
   }

#endif

