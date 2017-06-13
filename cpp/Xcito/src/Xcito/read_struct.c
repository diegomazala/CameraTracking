/*
 * File:       read_struct.c
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



#include "trk_struct.h"

#include <stdio.h>



static char       formatString [4096];



void
printCameraParams (const trkCameraParams* p)
   {
   int i,j;

   trkFormatToString (p->format, formatString);
   printf ("Camera parameters:\n"
           "\tformat:           %s\n", formatString);
   if (p->format & trkEuler)
      printf ("\tx:                %g\n"
              "\ty:                %g\n"
              "\tz:                %g\n"
              "\tpan:              %g\n"
              "\ttilt:             %g\n"
              "\troll:             %g\n",
              p->t.e.x,   p->t.e.y,    p->t.e.z,
              p->t.e.pan, p->t.e.tilt, p->t.e.roll);
   else
      {
      printf ("\tmatrix:");
      for (i = 0; i < 4; ++i)
         {
         for (j = 0; j < 4; ++j)
            {
            if (j == 0)
               printf ("\n\t\t");
            else
               printf (" ");
            printf ("%14g", p->t.m [j] [i]);
            }
         }
      printf ("\n");
      }
   printf ("\tfield of view:    %g\n"
           "\tcenter shift x:   %g\n"
           "\tcenter shift y:   %g\n"
           "\tdistorsion k1:    %g\n"
           "\tdistorsion k2:    %g\n"
           "\tfocal distance:   %g\n"
           "\taperture:         %g\n"
           "\tcounter:          %lu\n\n",
           p->fov, p->centerX, p->centerY, p->k1, p->k2,
           p->focdist, p->aperture, p->counter);
   }



void
printCameraConstants (const trkCameraConstants* c)
   {
   printf ("Camera constants:\n"
           "\timage width:        %i\n"
           "\timage height:       %i\n"
           "\tblank left:         %i\n"
           "\tblank right:        %i\n"
           "\tblank top:          %i\n"
           "\tblank bottom:       %i\n"
           "\tchip width:         %g\n"
           "\tchip height:        %g\n"
           "\tfake chip width:    %g\n"
           "\tfake chip height:   %g\n\n",
           c->imageWidth,    c->imageHeight,
           c->blankLeft,     c->blankRight,
           c->blankTop,      c->blankBottom,
           c->chipWidth,     c->chipHeight,
           c->fakeChipWidth, c->fakeChipHeight);
   }

