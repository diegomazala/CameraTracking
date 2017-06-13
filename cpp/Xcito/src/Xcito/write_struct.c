/*
 * File:       write_struct.c
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



#include "trk_mem.h"

#include <string.h>
#include <math.h>

#ifndef M_PI
#define M_PI       3.14159265358979323846
#endif

static unsigned int           formatFlags = 0;

static const double           InitX   =  0.0;
static const double           InitY   = -2.0;
static const double           InitZ   =  1.5;
static const double           InitFov = 45.0;

static double                 x       =  0.0;
static double                 y       = -2.0;
static double                 z       =  1.5;
static double                 pan     =  0.0;
static double                 tilt    =  0.0;
static double                 roll    =  0.0;
static double                 fov     = 45.0;
static double                 centerX =  0.0;
static double                 centerY =  0.0;
static unsigned long          counter =  0;

static double                 chipWidth;
static double                 chipHeight;
static double                 chipDiag;
static double                 ignChipWidth;
static double                 ignChipHeight;
static double                 ignChipDiag;
static double                 ignKeepChipHeight;
static double                 ignKeepChipDiag;
static double                 ignChipShiftX;
static double                 ignChipShiftY;
static double                 chipToImageWidth;
static double                 chipToImageHeight;

static trkCameraParams        p;
static trkCameraParams        standardParams;
static trkCameraConstants     constants;

static int                    testParamIndex = -1;
static double*                testParam = &centerX;
static double                 testRange = 0.0;
static double                 testStart = 0.0;



int
setFormat (int numArgs, char** arg)
   {
   char        formatString [4096];
   int         i;
   double      totalWidth, totalHeight, realWidth, realHeight;

   formatFlags = 0;
   if (numArgs > 0)
      {
      strcpy (formatString, arg [0]);
      for (i = 1; i < numArgs; ++i)
         {
         strcat (formatString, " ");
         strcat (formatString, arg [i]);
         }
      if (!trkStringToFormat (formatString, &formatFlags))
         return 0;
      }

   constants.imageWidth  = 720;
   constants.imageHeight = 576;
   constants.blankLeft   =  10;
   constants.blankRight  =  10;
   constants.blankTop    =   0;
   constants.blankBottom =   0;

   chipWidth  = 6.4;
   chipHeight = 4.8;

   totalWidth  = (double) constants.imageWidth;
   totalHeight = (double) constants.imageHeight;
   realWidth   = (double) (constants.imageWidth
                           - constants.blankLeft
                           - constants.blankRight);
   realHeight  = (double) (constants.imageHeight
                           - constants.blankTop
                           - constants.blankBottom);

   ignChipWidth      = chipWidth  * (totalWidth  / realWidth);
   ignChipHeight     = chipHeight * (totalHeight / realHeight);
   ignKeepChipHeight = chipHeight * (totalWidth  / realWidth);
   chipDiag          = sqrt (chipWidth         * chipWidth +
                             chipHeight        * chipHeight);
   ignChipDiag       = sqrt (ignChipWidth      * ignChipWidth +
                             ignChipHeight     * ignChipHeight);
   ignKeepChipDiag   = sqrt (ignChipWidth      * ignChipWidth +
                             ignKeepChipHeight * ignKeepChipHeight);

   chipToImageWidth  = realWidth  / chipWidth;
   chipToImageHeight = realHeight / chipHeight;

   ignChipShiftX = 0.5 * ((double) constants.blankRight -
                                   constants.blankLeft) /
                   chipToImageWidth;
   ignChipShiftY = 0.5 * ((double) constants.blankTop -
                                   constants.blankBottom) /
                   chipToImageHeight;

   constants.chipWidth      = chipWidth;
   constants.chipHeight     = chipHeight;
   constants.fakeChipWidth  = ignChipWidth;
   constants.fakeChipHeight = (formatFlags & trkKeepAspect) ?
                              ignKeepChipHeight : ignChipHeight;

   standardParams.format = formatFlags;
   if ((formatFlags & trkEuler) == 0)
      {
      standardParams.t.m [0] [3] =
            standardParams.t.m [1] [3] = 
            standardParams.t.m [2] [3] = 0.0;
      standardParams.t.m [3] [3] = 1.0;
      }
   standardParams.k1 = standardParams.k2 =
         standardParams.focdist = standardParams.aperture = 0.0;

   return 1;
   }



static void
makeCameraParams ()
   {
   double      sp, cp, st, ct, sr, cr;
   double      temp, shiftX, shiftY, shiftZ;
   double      (*m) [4];
   double      imageDistance;
   double      resolution;

   memcpy ((void*) &p, (void*) &standardParams, sizeof (trkCameraParams));

   if (formatFlags & trkEuler)
      {
      p.t.e.x    = x;
      p.t.e.y    = (formatFlags & trkStudioY_Up) ?  z : y;
      p.t.e.z    = (formatFlags & trkStudioY_Up) ? -y : z;
      p.t.e.pan  = pan;
      p.t.e.tilt = tilt;
      p.t.e.roll = roll;
      }
   else
      {
      sp = sin (pan  * (M_PI / 180.0));
      cp = cos (pan  * (M_PI / 180.0));
      st = sin (tilt * (M_PI / 180.0));
      ct = cos (tilt * (M_PI / 180.0));
      sr = sin (roll * (M_PI / 180.0));
      cr = cos (roll * (M_PI / 180.0));
      m = p.t.m;

      m [0] [0] = cp * cr - sp * st * sr;
      m [0] [1] = sp * cr + cp * st * sr;
      m [0] [2] = -ct * sr;

      m [1] [0] = -sp * ct;
      m [1] [1] = cp * ct;
      m [1] [2] = st;

      m [2] [0] = cp * sr + sp * st * cr;
      m [2] [1] = sp * sr - cp * st * cr;
      m [2] [2] = ct * cr;

      m [3] [0] = x;
      m [3] [1] = y;
      m [3] [2] = z;

      if (formatFlags & trkCameraY_Up)
         {
         /* exchange middle columns, then negate entries in third column */
         temp = m [2] [0];   m [2] [0] = -m [1] [0];   m [1] [0] = temp;
         temp = m [2] [1];   m [2] [1] = -m [1] [1];   m [1] [1] = temp;
         temp = m [2] [2];   m [2] [2] = -m [1] [2];   m [1] [2] = temp;
         }

      if (formatFlags & trkStudioY_Up)
         {
         /* exchange middle rows, then negate entries in third row */
         temp = m [0] [2];   m [0] [2] = -m [0] [1];   m [0] [1] = temp;
         temp = m [1] [2];   m [1] [2] = -m [1] [1];   m [1] [1] = temp;
         temp = m [2] [2];   m [2] [2] = -m [2] [1];   m [2] [1] = temp;
         temp = m [3] [2];   m [3] [2] = -m [3] [1];   m [3] [1] = temp;
         }

      if (formatFlags & trkStudioToCamera)
         {
         /*
          * invert matrix:
          * since upper left 3x3 matrix is orthogonal, its inverse
          * is calculated by transposing it;
          * the new translation vector in the fourth column is calculated
          * by applying the previous translation vector to the inverted
          * upper left 3x3 matrix
          */
         temp = m [0] [1];   m [0] [1] = m [1] [0];   m [1] [0] = temp;
         temp = m [0] [2];   m [0] [2] = m [2] [0];   m [2] [0] = temp;
         temp = m [1] [2];   m [1] [2] = m [2] [1];   m [2] [1] = temp;
         shiftX = - (m[0][0]*m[3][0] + m[1][0]*m[3][1] + m[2][0]*m[3][2]);
         shiftY = - (m[0][1]*m[3][0] + m[1][1]*m[3][1] + m[2][1]*m[3][2]);
         shiftZ = - (m[0][2]*m[3][0] + m[1][2]*m[3][1] + m[2][2]*m[3][2]);
         m [3] [0] = shiftX;
         m [3] [1] = shiftY;
         m [3] [2] = shiftZ;
         }
      }

   imageDistance = 0.5 * chipWidth / tan (fov * (M_PI / 360.0));
   if (formatFlags & trkFieldOfView)
      {
      resolution = chipWidth;
      if (formatFlags & trkIgnoreBlank)
         {
         resolution = ignChipWidth;
         if (formatFlags & trkKeepAspect)
            {
            if (formatFlags & trkVertical)
               resolution = ignKeepChipHeight;
            else if (formatFlags & trkDiagonal)
               resolution = ignKeepChipDiag;
            }
         else
            {
            if (formatFlags & trkVertical)
               resolution = ignChipHeight;
            else if (formatFlags & trkDiagonal)
               resolution = ignChipDiag;
            }
         }
      else
         {
         if (formatFlags & trkVertical)
            resolution = chipHeight;
         else if (formatFlags & trkDiagonal)
            resolution = chipDiag;
         }
      p.fov = atan (0.5 * resolution / imageDistance) * (360.0 / M_PI);
      }
   else
      p.fov = imageDistance;

   p.centerX = centerX;
   p.centerY = centerY;
   if (formatFlags & trkIgnoreBlank)
      {
      p.centerX += ignChipShiftX;
      p.centerY += ignChipShiftY;
      }
   if (formatFlags & trkShiftInPixels)
      {
      p.centerX *= chipToImageWidth;
      p.centerY *= chipToImageHeight;
      }
   p.counter = ++counter;
   }



trkCameraParams*
getCameraParams (int testParameter, double testPosition)
   {
   if (testParameter < 1 || testParameter > 9)   testParameter = 1;
   if (testParameter != testParamIndex)
      {
      *testParam = testStart;
      testParamIndex = testParameter;
      switch (testParamIndex)
         {
         case 1:     testParam = &x;
                     testRange = 3.0;
                     testStart = InitX;
                     break;
         case 2:     testParam = &y;
                     testRange = 5.0;
                     testStart = InitY;
                     break;
         case 3:     testParam = &z;
                     testRange = 1.0;
                     testStart = InitZ;
                     break;
         case 4:     testParam = &pan;
                     testRange = 90.0;
                     testStart = 0.0;
                     break;
         case 5:     testParam = &tilt;
                     testRange = 45.0;
                     testStart = 0.0;
                     break;
         case 6:     testParam = &roll;
                     testRange = 60.0;
                     testStart = 0.0;
                     break;
         case 7:     testParam = &fov;
                     testRange = 40.0;
                     testStart = InitFov;
                     break;
         case 8:     testParam = &centerX;
                     testRange = 1.6;
                     testStart = 0.0;
                     break;
         default:    testParam = &centerY;
                     testRange = 1.2;
                     testStart = 0.0;
                     break;
         }
      }
   *testParam = testStart + testRange * sin (testPosition * (2.0 * M_PI));
   makeCameraParams ();
   return &p;
   }



trkCameraConstants*
getCameraConstants ()
   { return &constants; }



const char*
allOptionsString ()
   {
   return "   valid format options:\n"
          "      either \"matrix\" or \"euler\"\n"
          "      if \"matrix\" is used: either \"camera_to_studio\" or "
          "\"studio_to_camera\"\n"
          "         and either \"camera_z_up\" or \"camera_y_up\"\n"
          "      either \"studio_z_up\" or \"studio_y_up\"\n"
          "      either \"image_distance\" or \"field_of_view\"\n"
          "      if \"field_of_view\" is used: either \"horizontal\" or\n"
          "         \"vertical\" or \"diagonal\"\n"
          "      either \"consider_blank\" or \"ignore_blank\"\n"
          "      if \"ignore_blank\" is used: either \"adjust_aspect\" or "
          "\"keep_aspect\"\n"
          "      either \"shift_on_chip\" or \"shift_in_pixels\"\n"
          "   defaults:\n"
          "      matrix, camera_to_studio, camera_z_up, studio_z_up\n"
          "      image_distance, consider_blank, shift_on_chip\n";
   }


