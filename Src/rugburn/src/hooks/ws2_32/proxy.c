/**
 * Rugburn Proxy Mode
 * 
 * แทนที่จะ redirect ไป localhost แล้วให้ C# server จัดการ encryption
 * ให้ Rugburn ทำหน้าที่เป็น MITM proxy:
 * 
 * Client ? Rugburn (decrypt) ? C# Server (plaintext) ? Rugburn (encrypt) ? Client
 */

#include "proxy.h"
#include "../../common.h"
#include "../../config.h"
#include "../../crypto.h"
#include <stdio.h>
#include <string.h>

// Socket mappings: client socket ? proxy socket
static SOCKET clientToProxy[1024] = {0};
static SOCKET proxyToClient[1024] = {0};

// Encryption keys per socket
static BYTE socketKeys[1024] = {0};

// Track which sockets are game server connections
static BOOL isGameServerSocket[1024] = {0};

static PFNSENDPROC pSend = NULL;
static PFNRECVPROC pRecv = NULL;
static PFNCONNECTPROC pConnect = NULL;

/**
 * Connect Hook - Create proxy connection
 */
static int STDCALL ConnectHook(SOCKET s, const struct sockaddr *name, int namelen) {
    struct sockaddr_in name_in = *(struct sockaddr_in *)name;
    struct sockaddr_in override_in;
    
    memcpy(&override_in, &name_in, sizeof(struct sockaddr_in));

    // Check if this is a game server connection
    USHORT port = ntohs(name_in.sin_port);
    char oldaddr[16];
    char newaddr[16];
    
    sprintf(oldaddr, "%d.%d.%d.%d", 
        (unsigned char)((char*)&name_in.sin_addr)[0],
        (unsigned char)((char*)&name_in.sin_addr)[1],
        (unsigned char)((char*)&name_in.sin_addr)[2],
        (unsigned char)((char*)&name_in.sin_addr)[3]);
    
    if (port >= 20201 && port <= 20299) {
        // Game Server - ตรวจสอบว่าเป็น real server หรือไม่
        if (strcmp(oldaddr, "45.150.130.5") == 0) {
            // กรณี 1: ต้องการ redirect ไป localhost (Private Server)
            // Uncomment บรรทัดนี้ถ้าต้องการใช้ private server
            // override_in.sin_addr.s_addr = inet_addr("127.0.0.1");
            
            // กรณี 2: ปล่อยให้เชื่อมต่อ real server (แค่ log packets)
            // ไม่ต้องทำอะไร - ใช้ค่าเดิม
            
            sprintf(newaddr, "%d.%d.%d.%d", 
                (unsigned char)((char*)&override_in.sin_addr)[0],
                (unsigned char)((char*)&override_in.sin_addr)[1],
                (unsigned char)((char*)&override_in.sin_addr)[2],
                (unsigned char)((char*)&override_in.sin_addr)[3]);
            
            Log("GameServer connection: %s:%d ? %s:%d\r\n", 
                oldaddr, port, newaddr, ntohs(override_in.sin_port));
            
            // Mark as game server socket
            isGameServerSocket[s] = TRUE;
            
            // Store encryption key (will be set later from handshake)
            socketKeys[s] = 1; // Default key, will be updated
        }
    }

    return pConnect(s, (struct sockaddr *)&override_in, namelen);
}

/**
 * Send Hook - Log and optionally decrypt client packets
 */
static int STDCALL SendHook(SOCKET s, const char *buf, int len, int flags) {
    // Only intercept game server packets
    if (isGameServerSocket[s]) {
        BYTE key = socketKeys[s];
        
        Log("[Client?Server] Raw packet: %d bytes (key=%d)\r\n", len, key);
        
        // Log first 32 bytes in hex
        Log("  Raw: ");
        for (int i = 0; i < (len < 32 ? len : 32); i++) {
            Log("%02X ", (BYTE)buf[i]);
            if ((i + 1) % 16 == 0) Log("\r\n       ");
        }
        Log("\r\n");
        
        // ถ้าต้องการ decrypt และส่งไป C# server (private server mode)
        // Uncomment ส่วนนี้:
        /*
        int decryptedLen = 0;
        BYTE* decrypted = DecryptClientPacket((const BYTE*)buf, len, &decryptedLen, key);
        
        if (decrypted && decryptedLen > 0) {
            Log("[Client?Server] Decrypted: %d bytes\r\n", decryptedLen);
            
            // Log first 32 bytes
            Log("  Decrypted: ");
            for (int i = 0; i < (decryptedLen < 32 ? decryptedLen : 32); i++) {
                Log("%02X ", decrypted[i]);
                if ((i + 1) % 16 == 0) Log("\r\n             ");
            }
            Log("\r\n");
            
            // Send decrypted packet to C# server
            int result = pSend(s, (const char*)decrypted, decryptedLen, flags);
            
            // Free decrypted buffer
            FreeMem(decrypted);
            
            return result;
        } else {
            Log("[Client?Server] Failed to decrypt!\r\n");
        }
        */
    }

    // Pass through (ส่ง encrypted packet ไปตามปกติ)
    return pSend(s, buf, len, flags);
}

/**
 * Recv Hook - Log and optionally encrypt server packets
 */
static int STDCALL RecvHook(SOCKET s, char *buf, int len, int flags) {
    int result = pRecv(s, buf, len, flags);
    
    if (result > 0 && isGameServerSocket[s]) {
        // Check if this is a handshake packet (0x1800)
        if (result >= 7 && (BYTE)buf[0] == 0x00 && (BYTE)buf[1] == 0x18) {
            BYTE newKey = (BYTE)buf[6];
            socketKeys[s] = newKey;
            Log("[Server?Client] Handshake received! Extracted key: %d\r\n", newKey);
        }
        
        BYTE key = socketKeys[s];
        
        Log("[Server?Client] Raw packet: %d bytes (key=%d)\r\n", result, key);
        
        // Log first 32 bytes
        Log("  Raw: ");
        for (int i = 0; i < (result < 32 ? result : 32); i++) {
            Log("%02X ", (BYTE)buf[i]);
            if ((i + 1) % 16 == 0) Log("\r\n       ");
        }
        Log("\r\n");
        
        // ถ้าอยู่ใน private server mode และรับ plaintext จาก C# server
        // ต้อง encrypt ก่อนส่งกลับไปยัง client
        // Uncomment ส่วนนี้:
        /*
        int encryptedLen = 0;
        BYTE* encrypted = EncryptServerPacket((const BYTE*)buf, result, &encryptedLen, key);
        
        if (encrypted && encryptedLen > 0) {
            Log("[Server?Client] Encrypted: %d bytes\r\n", encryptedLen);
            
            // Copy encrypted data back to buffer
            if (encryptedLen <= len) {
                memcpy(buf, encrypted, encryptedLen);
                result = encryptedLen;
                
                // Log first 32 bytes of encrypted
                Log("  Encrypted: ");
                for (int i = 0; i < (encryptedLen < 32 ? encryptedLen : 32); i++) {
                    Log("%02X ", encrypted[i]);
                    if ((i + 1) % 16 == 0) Log("\r\n             ");
                }
                Log("\r\n");
            } else {
                Log("[Server?Client] ERROR: Encrypted packet too large!\r\n");
                result = -1;
            }
            
            // Free encrypted buffer
            FreeMem(encrypted);
        } else {
            Log("[Server?Client] Failed to encrypt!\r\n");
        }
        */
    }

    return result;
}

/**
 * Initialize Proxy Hook
 */
VOID InitProxyHook(VOID) {
    HMODULE hWinsockModule = LoadLib("ws2_32");
    
    pConnect = (PFNCONNECTPROC)HookProc(hWinsockModule, "connect", (PVOID)ConnectHook);
    pSend = (PFNSENDPROC)HookProc(hWinsockModule, "send", (PVOID)SendHook);
    pRecv = (PFNRECVPROC)HookProc(hWinsockModule, "recv", (PVOID)RecvHook);
    
    Log("Proxy hooks initialized\r\n");
}
