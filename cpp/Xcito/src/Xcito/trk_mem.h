/*
 * File:       trk_mem.h
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



#ifndef TRK_MEM_H
#define TRK_MEM_H

#include "trk_struct.h"



/* configuration flags for trkMemConfigure */

#define trkMemDefault      0x0000
#define trkMemSysV         0x0001
#define trkMemArena        0x0002

#define trkMemRecreate     0x0010



/* data transfer to and from local camera tracking program
   through shared memory */

void        trkMemConfigure      (unsigned flags);

trkError    trkMemOpen           (int slot, trkHandle*);
trkError    trkMemClose          (trkHandle);
void        trkMemCloseAll       ();

trkError    trkMemReadParams     (trkHandle, trkCameraParams*);
trkError    trkMemReadConstants  (trkHandle, trkCameraConstants*);

trkError    trkMemWriteParams    (trkHandle, const trkCameraParams*);
trkError    trkMemWriteConstants (trkHandle, const trkCameraConstants*);



#endif   /* #ifndef TRK_MEM_H */

