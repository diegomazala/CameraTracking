/*
 * File:       trk_struct.h
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

#ifndef TRK_STRUCT_H
#define TRK_STRUCT_H



/* format options */

#define trkMatrix          0x0000
#define trkEuler           0x0001

#define trkCameraToStudio  0x0000
#define trkStudioToCamera  0x0002

#define trkCameraZ_Up      0x0000
#define trkCameraY_Up      0x0004

#define trkStudioZ_Up      0x0000
#define trkStudioY_Up      0x0008

#define trkImageDistance   0x0000
#define trkFieldOfView     0x0010

#define trkHorizontal      0x0000
#define trkVertical        0x0020
#define trkDiagonal        0x0040

#define trkConsiderBlank   0x0000
#define trkIgnoreBlank     0x0080

#define trkAdjustAspect    0x0000
#define trkKeepAspect      0x0100

#define trkShiftOnChip     0x0000
#define trkShiftInPixels   0x0200



/* coordinate system transformation */

typedef union
   {

   double         m [4] [4];     /* m [j] [i] is matrix element */
                                 /* in row i, column j */
   struct
      {
      double   x;
      double   y;
      double   z;
      double   pan;
      double   tilt;
      double   roll;
      }           e;             /* "Euler" angles */

   } trkTransform;



/* camera parameters */

typedef struct
   {

   unsigned       id;            /* == 0 if not explicitly specified */

   unsigned       format;        /* bit mask of format options */

   trkTransform   t;             /* coordinate transformation */
   double         fov;           /* field of view or image distance */
   double         centerX;       /* center shift */
   double         centerY;

   double         k1;            /* distorsion coefficients */
   double         k2;

   double         focdist;       /* depth of field simulation */
   double         aperture;

   unsigned long  counter;

   } trkCameraParams;



/* camera constants */

typedef struct
   {

   unsigned       id;            /* == 0 if not explicitly specified */

   int            imageWidth;
   int            imageHeight;
   int            blankLeft;
   int            blankRight;
   int            blankTop;
   int            blankBottom;

   double         chipWidth;
   double         chipHeight;
   double         fakeChipWidth;
   double         fakeChipHeight;

   } trkCameraConstants;



/* handle data type */

typedef void*     trkHandle;



/* error codes */

typedef enum
   {

   trkOK = 0,
   trkFailed,
   trkInvalidHandle,
   trkInvalidVersion,
   trkUnavailable

   } trkError;


const char* trkErrorMessage (trkError);



/* help functions for internal use */

int         trkParamsToString    (const trkCameraParams*, char*);
int         trkStringToParams    (const char*, trkCameraParams*);

int         trkConstantsToString (const trkCameraConstants*, char*);
int         trkStringToConstants (const char*, trkCameraConstants*);

int         trkFormatToString    (unsigned format, char*);
int         trkStringToFormat    (const char*, unsigned* format);



#endif   /* #ifndef TRK_STRUCT_H */

