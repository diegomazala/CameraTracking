/*
 * File:       trk_net.h
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



#ifndef TRK_NET_H
#define TRK_NET_H

#include "trk_struct.h"



/* data transfer to and from remote camera tracking program
   across network using UDP datagrams */

trkError    trkNetOpenRead       (int localPort, trkHandle*, int skipOld);
trkError    trkNetOpenWrite      (const char* remoteHost, int remotePort,
                                  int binary, trkHandle* handle);
trkError    trkNetClose          (trkHandle);


trkError    trkNetReadParams     (trkHandle, trkCameraParams*);
trkError    trkNetBufReadParams  (trkHandle, trkCameraParams*,
                                  int numPassed, int numTolerated);
trkError    trkNetReadConstants  (trkHandle, trkCameraConstants*);
trkError    trkNetReadConstants2  (trkHandle, trkCameraConstants*);

int			trkNetParamsAvailable(trkHandle);
void		trkNetSkipOld		 (int, trkHandle);

trkError    trkNetWriteParams    (trkHandle, const trkCameraParams*);
trkError    trkNetWriteConstants (trkHandle, const trkCameraConstants*);



void printname(int a);



#endif   /* #ifndef TRK_NET_H */

