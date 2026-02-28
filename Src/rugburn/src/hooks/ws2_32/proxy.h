/**
 * Proxy Hook Header
 */

#ifndef PROXY_H
#define PROXY_H

#include "../../common.h"
#include <winsock2.h>

/**
 * Function pointer types for WinSock2 functions
 */
typedef int (STDCALL *PFNSENDPROC)(SOCKET, const char *, int, int);
typedef int (STDCALL *PFNRECVPROC)(SOCKET, char *, int, int);

/**
 * Initialize proxy hooks for transparent encryption/decryption
 */
VOID InitProxyHook(VOID);

#endif // PROXY_H
