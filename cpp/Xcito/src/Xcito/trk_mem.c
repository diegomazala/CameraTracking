/*
 * File:       trk_mem.c
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
#include "XcitoDefs.h"
#ifdef _XCITO_TRK_MEM_


#include "trk_mem.h"

#if defined (__sgi) && defined (__mips)
   /* Use shared arena mechanism available on SGI computers running IRIX !
      Otherwise, System V style shared memory is used. */
   #define SHARED_ARENA
   #define SYSV_MEM
#elif defined (__unix)
   #define SYSV_MEM
#endif

#if !defined (SYSV_MEM) && !defined (SHARED_ARENA)
   Either macro SYSV_MEM or macro SHARED_ARENA must be defined !!!
#endif



#include <string.h>
#include <sys/stat.h>
#include <unistd.h>
#include <stdlib.h>

#ifdef SHARED_ARENA
   #include <ulocks.h>
#endif
#ifdef SYSV_MEM
   #include <sys/types.h>
   #include <sys/ipc.h>
   #include <sys/shm.h>
   #include <fcntl.h>
#endif



#define trkMemType         0x000F

#define trkBufferMagic     0x2357BD01UL
#define trkBufferPath      "/tmp/dmc_tracking_buffer."



typedef unsigned short        trkCounter;



/* shared memory buffer contents */

typedef struct
   {
   unsigned long              magic;
   volatile int               connected;
   volatile trkCounter        paramWriteCounter;
   volatile trkCounter        constWriteCounter;
   trkCameraParams            cameraParams [2];
   trkCameraConstants         cameraConstants [2];
   } trkMemoryBuffer;



/* handle for shared memory */

struct trkMemHandleStruct
   {

   int                        slot;
   unsigned                   flags;
   struct trkMemHandleStruct* next;
   trkMemoryBuffer*           buffer;
   trkCounter                 paramReadCounter;
   trkCounter                 constReadCounter;
   int                        lastConnected;

   #ifdef SHARED_ARENA
   usptr_t*                   sharedArena;
   #endif
   #ifdef SYSV_MEM
   int                        memoryID;
   #endif

   };

typedef struct trkMemHandleStruct*     trkMemHandle;



/* pointer to head of global list of memory handles */

static trkMemHandle     firstHandle = (trkMemHandle) 0;

static unsigned         configurationFlags =
                                 #ifdef SHARED_ARENA
                                    trkMemArena;
                                 #else
                                    trkMemSysV;
                                 #endif



/* list of local functions */

static void             composeBufferPath (int slot, char* path);
static trkError         attach (trkMemHandle memHandle, const char* path,
                                int create, int initialize);
static void             detach (trkMemHandle memHandle, const char* path,
                                int kill);
static trkError         recreate (trkMemHandle memHandle);



trkError
trkMemReadParams (trkHandle handle, trkCameraParams* cameraParams)
   {
   trkMemHandle         memHandle;
   trkMemoryBuffer*     buffer;
   trkError             error;
   trkCounter           writeCounter;

   memHandle = (trkMemHandle) handle;
   if (memHandle == (trkMemHandle) 0 ||
       memHandle->buffer == (trkMemoryBuffer*) 0)
      return trkInvalidHandle;
   buffer = memHandle->buffer;
   if (buffer->magic != trkBufferMagic)   return trkInvalidVersion;

   if (memHandle->flags & trkMemRecreate)
      {
      /* check if this process is the only one still connected to memory */
      if (buffer->connected == 1 && memHandle->lastConnected > 1)
         {
         error = recreate (memHandle);
         if (error != trkOK)   return error;
         buffer = memHandle->buffer;
         }
      memHandle->lastConnected = buffer->connected;
      }

   writeCounter = buffer->paramWriteCounter;
   if (writeCounter == memHandle->paramReadCounter)
      return trkUnavailable;

   do
      {
      memcpy ((void*) cameraParams,
              (const void*) &(buffer->cameraParams [writeCounter & 1]),
              sizeof (trkCameraParams));
      memHandle->paramReadCounter = writeCounter;
      writeCounter = buffer->paramWriteCounter;
      }
   while (writeCounter != memHandle->paramReadCounter);

   return trkOK;
   }



trkError
trkMemReadConstants (trkHandle handle, trkCameraConstants* cameraConstants)
   {
   trkMemHandle         memHandle;
   trkMemoryBuffer*     buffer;
   trkCounter           writeCounter;

   memHandle = (trkMemHandle) handle;
   if (memHandle == (trkMemHandle) 0 ||
       memHandle->buffer == (trkMemoryBuffer*) 0)
      return trkInvalidHandle;
   buffer = memHandle->buffer;
   if (buffer->magic != trkBufferMagic)   return trkInvalidVersion;

   writeCounter = buffer->constWriteCounter;
   if (writeCounter == memHandle->constReadCounter)
      return trkUnavailable;

   do
      {
      memcpy ((void*) cameraConstants,
              (const void*) &(buffer->cameraConstants [writeCounter & 1]),
              sizeof (trkCameraConstants));
      memHandle->constReadCounter = writeCounter;
      writeCounter = buffer->constWriteCounter;
      }
   while (writeCounter != memHandle->constReadCounter);

   return trkOK;
   }



#ifndef ONLY_READ

trkError
trkMemWriteParams (trkHandle handle, const trkCameraParams* cameraParams)
   {
   trkMemHandle         memHandle;
   trkMemoryBuffer*     buffer;
   trkError             error;
   trkCounter           writeCounter;

   memHandle = (trkMemHandle) handle;
   if (memHandle == (trkMemHandle) 0 ||
       memHandle->buffer == (trkMemoryBuffer*) 0)
      return trkInvalidHandle;
   buffer = memHandle->buffer;
   if (buffer->magic != trkBufferMagic)   return trkInvalidVersion;

   if (memHandle->flags & trkMemRecreate)
      {
      /* check if this process is the only one still connected to memory */
      if (buffer->connected == 1 && memHandle->lastConnected > 1)
         {
         error = recreate (memHandle);
         if (error != trkOK)   return error;
         buffer = memHandle->buffer;
         }
      memHandle->lastConnected = buffer->connected;
      }

   writeCounter = buffer->paramWriteCounter + 1;
   memcpy ((void*) &(buffer->cameraParams [writeCounter & 1]),
           (const void*) cameraParams, sizeof (trkCameraParams));
   buffer->paramWriteCounter = writeCounter;

   return trkOK;
   }



trkError
trkMemWriteConstants (trkHandle handle,
                      const trkCameraConstants* cameraConstants)
   {
   trkMemHandle         memHandle;
   trkMemoryBuffer*     buffer;
   trkCounter           writeCounter;

   memHandle = (trkMemHandle) handle;
   if (memHandle == (trkMemHandle) 0 ||
       memHandle->buffer == (trkMemoryBuffer*) 0)
      return trkInvalidHandle;
   buffer = memHandle->buffer;
   if (buffer->magic != trkBufferMagic)   return trkInvalidVersion;

   writeCounter = buffer->constWriteCounter + 1;
   memcpy ((void*) &(buffer->cameraConstants [writeCounter & 1]),
           (const void*) cameraConstants, sizeof (trkCameraConstants));
   buffer->constWriteCounter = writeCounter;

   return trkOK;
   }

#endif



void
trkMemConfigure (unsigned flags)
   {
   unsigned memoryType;

   memoryType = flags & trkMemType;
   #ifndef SYSV_MEM
      if (memoryType == trkMemSysV)    memoryType = trkMemDefault;
   #endif
   #ifndef SHARED_ARENA
      if (memoryType == trkMemArena)   memoryType = trkMemDefault;
   #endif
   if (memoryType == trkMemDefault)
      #ifdef SHARED_ARENA
         memoryType = trkMemArena;
      #else
         memoryType = trkMemSysV;
      #endif
   configurationFlags = (flags & ~trkMemType) | memoryType;
   }



static void
composeBufferPath (int slot, char* path)
   {
   char  slotChar;
   int   length;

   slotChar = '0';
   if (slot > 0)
      {
      if (slot < 10)
         slotChar = '0' + slot;
      else if (slot < 36)
         slotChar = 'A' + (slot - 10);
      else
         slotChar = 'a' + (slot - 36);
      }
   length = strlen (trkBufferPath);
   memcpy ((void*) path, (const void*) trkBufferPath, length);
   path [length] = slotChar;
   path [length + 1] = '\0';
   }



trkError
trkMemOpen (int slot, trkHandle* handle)
   {
   trkMemHandle         memHandle;
   char                 path [256];
   struct stat          statDummy;
   trkError             error;

   if (slot < 0)   return trkFailed;
   memHandle = (trkMemHandle) malloc (sizeof (struct trkMemHandleStruct));
   if (memHandle == (trkMemHandle) 0)   return trkFailed;
   *handle = (trkHandle) 0;

   memHandle->slot = slot;
   memHandle->flags = configurationFlags;
   composeBufferPath (slot, path);
   error = attach (memHandle, path, stat (path, &statDummy) != 0, 1);
   if (error != trkOK)   return error;
   memHandle->next = firstHandle;
   firstHandle = memHandle;
   *handle = (trkHandle) memHandle;
   return trkOK;
   }



static trkError
attach (trkMemHandle memHandle, const char* path, int create, int initialize)
   {
   trkMemoryBuffer*     buffer;
   void*                startAddress;

   #ifdef SHARED_ARENA
   void*                arenaAddress;
   usptr_t*             arena;
   unsigned int         allocateSize;
   unsigned int         savedSegmentSize;
   #endif
   #ifdef SYSV_MEM
   int                  fd;
   key_t                memoryKey;
   int                  memoryID;
   #endif

   buffer = (trkMemoryBuffer*) 0;
   #ifdef SHARED_ARENA
      if ((memHandle->flags & trkMemType) == trkMemArena)
         {
         /*
          * According to the "Topics in IRIX Programming" Guide the
          * MIPS Application Binary Interface specification states that
          * virtual addresses from 0x3000 0000 through 0x3FFC 0000 are
          * reserved for user-defined segment base addresses.
          * (Chapter 1, "Process Address Space", section "Mapping Segments
          *  of Memory", subsection "Choosing a Segment Address")
          * Here I assume that 64KB (== 0x10000) of memory is enough
          * for each slot, with slot #0 starting at the fourth quarter of the
          * reserved memory region.
          */
         arenaAddress = (void*) (0x3C000000UL + memHandle->slot * 0x10000UL);
         usconfig (CONF_ATTACHADDR, arenaAddress);
         allocateSize = sizeof (trkMemoryBuffer) + 2 * getpagesize ();
         savedSegmentSize = usconfig (CONF_INITSIZE, allocateSize);
         arena = usinit (path);
         usconfig (CONF_INITSIZE, savedSegmentSize);
         usconfig (CONF_ATTACHADDR, (void*) (~0));
         if (arena == (usptr_t*) 0)   return trkFailed;
               /* creation of shared arena failed */
         if (uscasinfo (arena, (void*) 0, (void*) 1) == 0)
            {
            /* shared arena already existed */
            startAddress = usgetinfo (arena);
            if (startAddress == (void*) 1)
               {
               sleep (2);
               startAddress = usgetinfo (arena);
               if (startAddress == (void*) 1)
                  {
                  /* incomplete initialization of shared arena */
                  usdetach (arena);
                  return trkFailed;
                  }
               }
            if (startAddress == (void*) 0)
               {
               /* invalid shared memory address in shared arena */
               usdetach (arena);
               return trkFailed;
               }
            create = 0;
            buffer = (trkMemoryBuffer*) startAddress;
            if (buffer->magic != trkBufferMagic)
               {
               usdetach (arena);
               return trkInvalidVersion;
               }
            }
         else
            {
            /* shared arena was just created */
            startAddress = usmalloc (sizeof (trkMemoryBuffer), arena);
            if (startAddress == (void*) 0)
               {
               /* shared memory allocation failed in shared arena */
               usputinfo (arena, (void*) 0);
               usdetach (arena);
               if (create)   unlink (path);
               return trkFailed;
               }
            usconfig (CONF_CHMOD, arena, (mode_t) 0666);
            usputinfo (arena, startAddress);
            create = 1;
            buffer = (trkMemoryBuffer*) startAddress;
            }
         memHandle->sharedArena = arena;
      }
   #endif
   #ifdef SYSV_MEM
      if ((memHandle->flags & trkMemType) == trkMemSysV)
         {
         if (create)
            {
            fd = open (path, O_WRONLY | O_CREAT,
                             S_IRWXU | S_IRWXG | S_IRWXO);
            if (fd == -1)   return trkFailed;
                  /* creating file for shared memory failed */
            close (fd);
            }
         #ifdef __linux
            memoryKey = ftok ((char*) path, 0);
         #else
            memoryKey = ftok (path, 0);
         #endif
         if (memoryKey == -1)
            {
            /* cannot get a key for shared memory */
            if (create)   unlink (path);
            return trkFailed;
            }
         memoryID = shmget (memoryKey, sizeof (trkMemoryBuffer),
                            create ? IPC_CREAT | 0666 : 0666);
         if (memoryID == -1)
            {
            /* creation of shared memory failed */
            if (create)   unlink (path);
            return trkFailed;
            }
         #ifdef __linux
            startAddress = (void*) shmat (memoryID, (char*) 0, 0);
         #else
            startAddress = (void*) shmat (memoryID, (void*) 0, 0);
         #endif
         if (startAddress == (void*) -1 || startAddress == (void*) 0)
            {
            /* cannot get start address of shared memory */
            if (create)
               {
               shmctl (memoryID, IPC_RMID, (struct shmid_ds*) 0);
               unlink (path);
               }
            return trkFailed;
            }
         buffer = (trkMemoryBuffer*) startAddress;
         if (!create && buffer->magic != trkBufferMagic)
            {
            #ifdef __linux
               shmdt ((char*) buffer);
            #else
               shmdt ((void*) buffer);
            #endif
            return trkInvalidVersion;
            }
         memHandle->memoryID = memoryID;
      }
   #endif

   memHandle->buffer = buffer;
   if (initialize)
      {
      if (create)
         {
         buffer->magic = trkBufferMagic;
         buffer->connected = 0;
         buffer->paramWriteCounter = buffer->constWriteCounter = 0;
         }
      memHandle->paramReadCounter = buffer->paramWriteCounter;
      memHandle->constReadCounter = 0;
      memHandle->lastConnected = ++(buffer->connected);
      }
   return trkOK;
   }



trkError
trkMemClose (trkHandle handle)
   {
   trkMemHandle         memHandle;
   trkMemHandle         search;
   trkMemoryBuffer*     buffer;
   char                 path [256];

   memHandle = (trkMemHandle) handle;
   if (memHandle == (trkMemHandle) 0)   return trkInvalidHandle;

   /* remove handle from global list of memory handles */
   if (firstHandle == memHandle)
      firstHandle = memHandle->next;
   else
      {
      search = firstHandle;
      while (search != (trkMemHandle) 0)
         {
         if (search->next == memHandle)
            {
            search->next = memHandle->next;
            break;
            }
         search = search->next;
         }
      }

   /* detach from shared memory (and kill shared memory) */
   buffer = memHandle->buffer;
   if (buffer == (trkMemoryBuffer*) 0)    return trkInvalidHandle;
   if (buffer->magic != trkBufferMagic)   return trkInvalidVersion;

   composeBufferPath (memHandle->slot, path);
   detach (memHandle, path, --(buffer->connected) == 0);

   /* destroy handle */
   memHandle->buffer = (trkMemoryBuffer*) 0;
   free ((void*) memHandle);
   return trkOK;
   }



static void
detach (trkMemHandle memHandle, const char* path, int kill)
   {
   trkMemoryBuffer*     buffer;

   buffer = memHandle->buffer;
   #ifdef SHARED_ARENA
      if ((memHandle->flags & trkMemType) == trkMemArena)
         {
         if (kill)
            {
            usfree ((void*) buffer, memHandle->sharedArena);
            usdetach (memHandle->sharedArena);
            unlink (path);
            }
         else
            usdetach (memHandle->sharedArena);
         }
   #endif
   #ifdef SYSV_MEM
      if ((memHandle->flags & trkMemType) == trkMemSysV)
         {
         #ifdef __linux
            shmdt ((char*) buffer);
         #else
            shmdt ((void*) buffer);
         #endif
         if (kill)
            {
            shmctl (memHandle->memoryID, IPC_RMID, (struct shmid_ds*) 0);
            unlink (path);
            }
         }
   #endif
   }



static trkError
recreate (trkMemHandle memHandle)
   {
   trkMemoryBuffer      savedBuffer;
   char                 path [256];
   trkError             error;

   memcpy ((void*) &savedBuffer, (const void*) memHandle->buffer,
           sizeof (trkMemoryBuffer));
   composeBufferPath (memHandle->slot, path);
   detach (memHandle, path, 1);
   error = attach (memHandle, path, 1, 0);
   if (error != trkOK)   return error;
   memcpy ((void*) memHandle->buffer, (const void*) &savedBuffer,
           sizeof (trkMemoryBuffer));
   return trkOK;
   }



void
trkMemCloseAll ()
   {
   while (firstHandle != (trkMemHandle) 0)
      trkMemClose ((trkHandle) firstHandle);
   }

#endif