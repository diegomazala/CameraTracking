/*
 * File:       trk_net.c
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




#include "trk_net.h"

#ifndef UNICODE
#define UNICODE 1
#endif

#ifdef _WIN32 || _WIN64
	#include <winsock2.h> 
	#include <windows.h>
	#include <WinDef.h>
	#include <ws2tcpip.h>
	#include <winioctl.h>
	#define errno WSAGetLastError()  
#endif


#ifdef __linux
 ////// Typical #includes os a BSD sockets program //////
 #include <sys/types.h>
 #include <sys/socket.h>
 #include <netinet/in.h>
 #include <netdb.h>
 #include <arpa/inet.h>
 //////////////////////////////////////////////////////// 
 #include <ctype.h>
 #include <unistd.h>
 #include <sys/ioctl.h>
 #include <errno.h>

 #ifndef FIONBIO
	 #include <sys/filio.h> 
 #endif

#endif

#include <stdlib.h>
#include <stdio.h>
#include <string.h>



#if defined(__linux) && defined(__GLIBC__)
   typedef unsigned int    socket_size_t;
#else
   typedef int             socket_size_t;
#endif
   
#define trkNetMagic           "DMC01"
#define trkNetHeaderType      6
#define trkNetHeaderFormat    7
#define trkNetHeaderSize      8

#define trkMaxDatagramSize    4096
#define trkRingSize           16



/* handles for network transfers */

struct trkNetHandleStruct
   {

   int                  fd;                     /* file descriptor */
   int                  receiver;               /* receiver or sender ? */

   int                  paramsAvailable;        /* used by receiver */
   trkCameraParams      params [trkRingSize];   /* used by receiver */
   int                  lastRead;               /* used by receiver */
   int                  lastWrite;              /* used by receiver */
   unsigned long        lastCounter;            /* used by receiver */
   int                  skipOld;                /* used by receiver */

   int                  constAvailable;         /* used by receiver */
   trkCameraConstants   constants;              /* used by receiver */

   #ifndef ONLY_READ
   int                  binary;                 /* used by sender */
   struct sockaddr_in   destination;            /* used by sender */
   #endif

   };

typedef struct trkNetHandleStruct*     trkNetHandle;



static trkError
readDatagrams (trkNetHandle netHandle)
   {
	char  datagram [trkMaxDatagramSize];
	int   size;
    char  typeChar;
    char  formatChar;
    socket_size_t lengthDummy;
	
	int                  count;
	int					 j;

	struct sockaddr_in senderAddr;
    socket_size_t senderAddrSize = sizeof (senderAddr);


    for (;;)
	//for(count=0; count<10; count++)
    {


	#ifdef _WIN32 || WIN64
		  
		size = recvfrom (netHandle->fd, datagram, trkMaxDatagramSize, 0, (SOCKADDR *) &senderAddr, &senderAddrSize);
	#else
		  /* try to read a UDP datagram */
		  lengthDummy = 0;
		  size = recvfrom (netHandle->fd, (void*) datagram, trkMaxDatagramSize, 0, NULL, &lengthDummy);
	#endif
		  
	  
	  #ifdef __linux__
			if (size < 0 && errno == EWOULDBLOCK)   size = 0;
	  #else 
			if (size == SOCKET_ERROR && errno == WSAEWOULDBLOCK)
			{
					size = 0;	
			}

	  #endif

      if (size < 0)     
		  {
			  return trkFailed;
		  }
      if (size == 0)    
		  {
			  return trkOK;
		  }

      typeChar   = datagram [trkNetHeaderType];
      formatChar = datagram [trkNetHeaderFormat];
      if (size < trkNetHeaderSize + 1 ||
          strcmp (datagram, trkNetMagic) != 0 ||
          (typeChar != 'P' && typeChar != 'C') ||
          (formatChar != 'A' && formatChar != 'B'))
               return trkInvalidVersion;
   
      if (typeChar == 'C')
         {
         /* datagram contains camera constants */
         if (formatChar == 'B')
            {
            /* binary format */
            if (size != trkNetHeaderSize + sizeof (trkCameraConstants))
               return trkInvalidVersion;
            memcpy ((void*) &(netHandle->constants),
                    (const void*) (datagram + trkNetHeaderSize),
                    sizeof (trkCameraConstants));
            }
         else
            {
            /* ASCII format */
            if (!trkStringToConstants (datagram + trkNetHeaderSize,
                                       &(netHandle->constants)))
               return trkInvalidVersion;
            }
         netHandle->constAvailable = 1;
         }
      else
         {
         /* datagram contains camera parameters */
         if (++(netHandle->lastWrite) == trkRingSize)
            netHandle->lastWrite = 0;
         if (netHandle->lastWrite == netHandle->lastRead &&
             ++netHandle->lastRead == trkRingSize)
            netHandle->lastRead = 0;
         if (formatChar == 'B')
            {
            /* binary format */
            if (size != trkNetHeaderSize + sizeof (trkCameraParams))
               return trkInvalidVersion;
            memcpy ((void*) &(netHandle->params [netHandle->lastWrite]),
                    (const void*) (datagram + trkNetHeaderSize),
                    sizeof (trkCameraParams));
            }
         else
            {
            /* ASCII format */
            if (!trkStringToParams (datagram + trkNetHeaderSize,
                           &(netHandle->params [netHandle->lastWrite])))
               return trkInvalidVersion;
            }
         if (netHandle->params [netHandle->lastWrite].id)
            netHandle->skipOld = 0;
         ++(netHandle->paramsAvailable);
         }
      }
   }



trkError
trkNetReadParams (trkHandle handle, trkCameraParams* cameraParams)
{
   trkNetHandle   netHandle;
   trkError       result;
   int            index;

   netHandle = (trkNetHandle) handle;
   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       !netHandle->receiver)
      return trkInvalidHandle;

   result = readDatagrams (netHandle);
   if (result != trkOK)   return result;
   if (netHandle->paramsAvailable == 0)   return trkUnavailable;
   if (netHandle->skipOld)
      index = netHandle->lastWrite;   /* reads most recent parameters */
   else
      {
      index = netHandle->lastRead + 1;
      if (index == trkRingSize)   index = 0;
      }
   memcpy ((void*) cameraParams,
           (const void*) &(netHandle->params [index]),
           sizeof (trkCameraParams));
   netHandle->lastCounter = netHandle->params [index].counter;
   netHandle->lastRead = index;
   if (netHandle->skipOld)
      netHandle->paramsAvailable = 0;
   else
      {
      netHandle->paramsAvailable = netHandle->lastWrite -
                                   netHandle->lastRead;
      if (netHandle->paramsAvailable < 0)
         netHandle->paramsAvailable += trkRingSize;
      }


   return trkOK;
}



trkError
trkNetBufReadParams (trkHandle handle, trkCameraParams* cameraParams,
                     int numPassed, int numTolerated)
   {
   trkNetHandle   netHandle;
   trkError       result;
   unsigned long  wantedCounter;
   unsigned long  recentCounter;
   unsigned long  testCounter;
   int            useRecent;
   int            index;

   netHandle = (trkNetHandle) handle;
   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       !netHandle->receiver)
      return trkInvalidHandle;

   result = readDatagrams (netHandle);
   if (result != trkOK)   return result;
   if (netHandle->paramsAvailable == 0)   return trkUnavailable;
   /* search parameters with requested counter value */
   wantedCounter = netHandle->lastCounter + numPassed;
   recentCounter = netHandle->params [netHandle->lastWrite].counter;
   useRecent = (netHandle->paramsAvailable >= trkRingSize ||
                recentCounter <= netHandle->lastCounter ||
                recentCounter >  wantedCounter + numTolerated);
   if (!useRecent)
      {
      /* loop through parameters in ring buffer */
      --(netHandle->paramsAvailable);
      index = netHandle->lastRead + 1;
      if (index == trkRingSize)   index = 0;
      for (;;)
         {
         testCounter = netHandle->params [index].counter;
         if (testCounter > recentCounter)
            {
            /* counters in ring buffer are not in ascending order */
            useRecent = 1;
            break;
            }
         if (testCounter >= wantedCounter)
            {
            /* found parameters with requested counter value (or larger) */
            netHandle->lastCounter = wantedCounter;
            break;
            }
         if (netHandle->paramsAvailable == 0)   break;
         --(netHandle->paramsAvailable);
         if (++index == trkRingSize)   index = 0;
         }
      }
   if (useRecent)
      {
      index = netHandle->lastWrite;
      netHandle->lastCounter = recentCounter;
      netHandle->paramsAvailable = 0;
      }
   memcpy ((void*) cameraParams,
           (const void*) &(netHandle->params [index]),
           sizeof (trkCameraParams));
   netHandle->lastRead = index;
   return trkOK;
   }



trkError
trkNetReadConstants (trkHandle handle, trkCameraConstants* cameraConstants)
   {
   trkNetHandle   netHandle;
   trkError       result;

   netHandle = (trkNetHandle) handle;

   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       !netHandle->receiver)	   
      return trkInvalidHandle;      

   result = readDatagrams (netHandle); 

   if (result != trkOK) return result;

   if (!netHandle->constAvailable)   return trkUnavailable;
   memcpy ((void*) cameraConstants,
           (const void*) &(netHandle->constants),
           sizeof (trkCameraConstants));
   netHandle->constAvailable = 0;      

   return trkOK;
   }


trkError
trkNetReadConstants2 (trkHandle handle, trkCameraConstants* cameraConstants)
   {
   trkNetHandle   netHandle;
   trkError       result;

   netHandle = (trkNetHandle) handle;

   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       !netHandle->receiver)	   
      return trkInvalidHandle;      

   result = readDatagrams (netHandle); 

   if (result != trkOK) return result;

   if (!netHandle->constAvailable)   return trkUnavailable;
   memcpy ((void*) cameraConstants,
           (const void*) &(netHandle->constants),
           sizeof (trkCameraConstants));
   netHandle->constAvailable = 0;      

   return trkOK;
   }


int trkNetParamsAvailable(trkHandle handle)
{
	trkNetHandle netHandle = (trkNetHandle) handle;
	return netHandle->paramsAvailable;
}


void trkNetSkipOld(int skip, trkHandle handle)
{
	trkNetHandle netHandle = (trkNetHandle) handle;
	netHandle->skipOld = skip;
}


#ifndef ONLY_READ

trkError
trkNetWriteParams (trkHandle handle, const trkCameraParams* cameraParams)
   {
   trkNetHandle   netHandle;
   char           datagram [trkMaxDatagramSize];
   int            size;

   netHandle = (trkNetHandle) handle;
   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       netHandle->receiver)
      return trkInvalidHandle;

   /* create datagram contents */
   size = trkNetHeaderSize;
   strcpy (datagram, trkNetMagic);
   datagram [trkNetHeaderType] = 'P';
   if (netHandle->binary)
      {
      datagram [trkNetHeaderFormat] = 'B';
      memcpy ((void*) (datagram + trkNetHeaderSize),
              (const void*) cameraParams,
              sizeof (trkCameraParams));
      size += sizeof (trkCameraParams);
      }
   else
      {
      datagram [trkNetHeaderFormat] = 'A';
      size += trkParamsToString (cameraParams,
                                 datagram + trkNetHeaderSize) + 1;
      }

   /* send datagram */
   sendto (netHandle->fd, (const void*) datagram, size, 0,
           (struct sockaddr*) &(netHandle->destination),
           sizeof (struct sockaddr_in));
   return trkOK;
   }


trkError
trkNetWriteConstants (trkHandle handle,
                      const trkCameraConstants* cameraConstants)
   {
   trkNetHandle   netHandle;
   char           datagram [trkMaxDatagramSize];
   int            size;

   netHandle = (trkNetHandle) handle;
   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1 ||
       netHandle->receiver)
      return trkInvalidHandle;

   /* create datagram contents */
   size = trkNetHeaderSize;
   strcpy (datagram, trkNetMagic);
   datagram [trkNetHeaderType] = 'C';
   if (netHandle->binary)
      {
      datagram [trkNetHeaderFormat] = 'B';
      memcpy ((void*) (datagram + trkNetHeaderSize),
              (const void*) cameraConstants,
              sizeof (trkCameraConstants));
      size += sizeof (trkCameraConstants);
      }
   else
      {
      datagram [trkNetHeaderFormat] = 'A';
      size += trkConstantsToString (cameraConstants,
                                    datagram + trkNetHeaderSize) + 1;
      }

   /* send datagram */
   sendto (netHandle->fd, (const void*) datagram, size, 0,
           (struct sockaddr*) &(netHandle->destination),
           sizeof (struct sockaddr_in));
   return trkOK;
   }

#endif


trkError
trkNetOpenRead (int localPort, trkHandle* handle, int skipOld)
   {
   trkNetHandle         netHandle;
   int                  fd;
   struct sockaddr_in   local;   
   int                  lsocketError;   
   
   #ifdef _WIN32 || _WIN64   
	u_long nonBlockFlag;
   #else
	int nonBlockFlag;   
   #endif
   
   // Variables used to check tha available protocols
   int					iResult = 0;
   WSADATA				wsaData;

   *handle = (trkHandle) 0;
   if (localPort <= 0)   return trkFailed;

   
   // Initialize Winsock
#ifdef _WIN32 || _WIN64
		iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
		if (iResult != 0) 
		{
			printf("WSAStartup failed: %d\n", iResult);        
		}  
#endif

   /* get socket */
   fd = socket (AF_INET, SOCK_DGRAM, IPPROTO_UDP);

   //printf(" The descriptor is: %d ", fd);
   
   //// Just for tests ////
   //lsocketError = WSAGetLastError();
   //if(lsocketError == WSANOTINITIALISED) 
   //{
	  // printf("\n Winsock was not initialized \n");
   //}   
   //printf("%d %d ", fd, lsocketError);

	#ifdef __linux__
	   // This is from the original code and is tha same as INVALID_SOCKET (Need testing the macro INVALID_SOCKET before use it here).
	   if (fd == -1)   
	   {
			printf("Error at socket(): %ld\n", WSAGetLastError());
			return trkFailed;
	   }		
	#else
		if (fd == INVALID_SOCKET)
		{
		   printf("Error at socket(): %ld\n", WSAGetLastError());
		   return trkFailed;
		}
    #endif

   /* fill in local socket structure */
   memset ((void*) &local, 0, sizeof (local));
   local.sin_family = AF_INET;
   local.sin_addr.s_addr = htonl (INADDR_ANY);
   local.sin_port = htons (localPort);
   printf(" test adress %s ", local.sin_addr.s_addr);


   /* register local port number */
   if (bind (fd, (struct sockaddr*) &local, sizeof (local)) == -1)
      {
		#ifdef _WIN32 || _WIN64
		  closesocket(fd);
		#else
		  close(fd);
		#endif

		return trkFailed;
      }

   /* make read operations non-blocking */
   nonBlockFlag = 1;   

   #ifdef __linux__ 
	ioctl (fd, FIONBIO, (char*) &nonBlockFlag);
   #endif
   #ifdef _WIN32 || _WIN64
	ioctlsocket(fd, FIONBIO, &nonBlockFlag);
   #endif

   netHandle = (trkNetHandle) malloc (sizeof (struct trkNetHandleStruct));
   if (netHandle == (trkNetHandle) 0)
      {
		#ifdef _WIN32 || _WIN64
		  closesocket(fd);
		#else
		  close(fd);
		#endif
      return trkFailed;
      }
   netHandle->fd = fd;
   netHandle->receiver = 1;
   netHandle->paramsAvailable = netHandle->constAvailable = 0;
   netHandle->lastRead = netHandle->lastWrite = trkRingSize - 1;
   netHandle->lastCounter = 0;
   netHandle->skipOld = skipOld;
   *handle = (trkHandle) netHandle;
   return trkOK;
}




trkError
trkNetOpenWrite (const char* remoteHost, int remotePort,
                 int binary, trkHandle* handle)
{
   trkNetHandle         netHandle;
   struct hostent*      hostInfo;
   int                  fd;
   struct sockaddr_in   local;
   int                  broadcastFlag;
   const char*          ttlString;
   unsigned char        ttl;
   

   *handle = (trkHandle) 0;
   if (remotePort <= 0 || remoteHost == (const char*) 0)
      return trkFailed;

   netHandle = (trkNetHandle) malloc (sizeof (struct trkNetHandleStruct));
   if (netHandle == (trkNetHandle) 0)   return trkFailed;
   memset ((void*) &(netHandle->destination), 0,
           sizeof (netHandle->destination));

   /* convert host name into IP address */
   while (isspace (*remoteHost))   ++remoteHost;
   hostInfo = gethostbyname (remoteHost);
   if (hostInfo == (struct hostent*) 0)
   {
      /* host address could not be found */
      free ((void*) netHandle);
      return trkFailed;
   }
   memcpy ((void*) &(netHandle->destination.sin_addr),
           (const void*) hostInfo->h_addr,
           sizeof (struct in_addr));
   netHandle->destination.sin_family = AF_INET;
   netHandle->destination.sin_port = htons (remotePort);

   /* get socket */
   fd = socket (AF_INET, SOCK_DGRAM, IPPROTO_UDP);
   if (fd == -1)   return trkFailed;

   /* fill in local socket structure */
   memset ((void*) &local, 0, sizeof (local));
   local.sin_family = AF_INET;
   local.sin_addr.s_addr = htonl (INADDR_ANY);
   local.sin_port = htons (0);   /* allocate any local port */

   /* register local port number */
   if (bind (fd, (struct sockaddr*) &local, sizeof (local)) == -1)
   {
      close (fd);
      return trkFailed;
   }

   /* enable permission to transmit broadcast messages */
   broadcastFlag = 1;
   setsockopt (fd, SOL_SOCKET, SO_BROADCAST,
               (const void*) &broadcastFlag, sizeof (broadcastFlag));

   /* set TTL (time to live) of multicast datagrams if requested */
   if (IN_MULTICAST (ntohl (netHandle->destination.sin_addr.s_addr)))
   {
      ttlString = getenv ("MULTICAST_TTL");
      if (ttlString)
      {
         if (sscanf (ttlString, "%hhu", &ttl) == 1)
         {
            if (setsockopt (fd, IPPROTO_IP, IP_MULTICAST_TTL,
                            &ttl, sizeof (ttl)) == 0)
               printf ("Multicast TTL was set to %hhu\n", ttl);
            else
               fprintf (stderr, "FAILED TO SET MULTICAST TTL TO %hhu\n",
                        ttl);
         }
         else
            fprintf (stderr, "INVALID MULTICAST TTL: %s\n", ttlString);
         }
   }

   netHandle->fd = fd;
   netHandle->receiver = 0;
   netHandle->binary = binary;
   *handle = (trkHandle) netHandle;
   return trkOK;
}




trkError
trkNetClose (trkHandle handle)
   {
   trkNetHandle   netHandle;
   int            nonBlockFlag;
   u_long		  nonBlockFlagWindows;

   netHandle = (trkNetHandle) handle;
   if (netHandle == (trkNetHandle) 0 || netHandle->fd == -1)
      return trkInvalidHandle;

   if (netHandle->receiver)
      {
      /* turn off non-blocking operations */
      nonBlockFlag = 0;	 
	  nonBlockFlagWindows = 0;
      
	#ifdef __linux
		 ioctl (netHandle->fd, FIONBIO, (char*) &nonBlockFlag); 
	#else		  
		 ioctlsocket(netHandle->fd, FIONBIO, &nonBlockFlagWindows);
	#endif
      }
   
	#ifdef _WIN32 || _WIN64		  
		  if (closesocket (netHandle->fd) != 0)   return trkFailed;
	#else
		  if (close (netHandle->fd) != 0)   return trkFailed;
    #endif
   
   netHandle->fd = -1;
   free ((void*) netHandle);
   return trkOK;
   }



void printname(int a)
{
	printf("%i ",a);
}