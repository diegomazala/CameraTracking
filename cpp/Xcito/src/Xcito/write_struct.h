/*
 * File:       write_struct.h
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



#ifndef WRITE_STRUCT_H
#define WRITE_STRUCT_H

#include "trk_struct.h"



const char*             allOptionsString ();

int                     setFormat (int numArgs, char** arg);

trkCameraParams*        getCameraParams (int testParameter,
                                         double testPosition);
                                 /* 1   <= testParameter <= 9   */
                                 /* 0.0 <= testPosition  <= 1.0 */

trkCameraConstants*     getCameraConstants ();



#endif   /* #ifndef WRITE_STRUCT_H */

