/*
 * File:       trk_file.h
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



#ifndef TRK_FILE_H
#define TRK_FILE_H

#include "trk_struct.h"



/* reading and writing of data stored in file by camera tracking program */

trkError    trkFileOpenRead      (const char* fileName,
                                  char* timeCode,
                                  trkCameraConstants*,
                                  trkHandle*);
trkError    trkFileOpenWrite     (const char* fileName,
                                  const char* timeCode,
                                  const trkCameraConstants*,
                                  trkHandle*);
trkError    trkFileClose         (trkHandle);

trkError    trkFileReadParams    (trkHandle, trkCameraParams*);
trkError    trkFileWriteParams   (trkHandle, const trkCameraParams*);




#endif   /* #ifndef TRK_FILE_H */

