/**
 * PangYa Encryption/Decryption Functions
 * Ported from PangCrypt (https://github.com/pangyatools/PangCrypt)
 */

#include "crypto.h"
#include "common.h"
#include <time.h>
#include <stdlib.h>

// CryptoOracle table from PangCrypt
// This is the CryptTable2 used for encryption/decryption
static BYTE CryptTable2[4096] = {
    0x00, 0x01, 0x55, 0x27, 0x9F, 0x90, 0x1D, 0x92, 0xB2, 0x2A, 0x37, 0xAB, 0x16, 0x1B, 0x8C, 0xCF,
    0xD8, 0xA5, 0x21, 0x35, 0x46, 0x91, 0x71, 0xE3, 0x94, 0xF1, 0xF9, 0xD0, 0x1C, 0x73, 0x6F, 0x26,
    0x39, 0xDF, 0x4F, 0x03, 0x19, 0x2C, 0xC6, 0xAF, 0xAA, 0x02, 0x86, 0x8A, 0x58, 0x64, 0xF2, 0xA1,
    0x4D, 0xDB, 0x38, 0x7A, 0xCB, 0x80, 0x3F, 0xE7, 0x2B, 0x79, 0xD7, 0x34, 0x14, 0xAD, 0x17, 0xCC,
    0x3D, 0xCE, 0xDC, 0x24, 0x48, 0x33, 0x9D, 0x1F, 0x83, 0x0C, 0x75, 0xF5, 0xD9, 0x22, 0xBF, 0x45,
    0x3B, 0xF0, 0x0A, 0x61, 0x98, 0xBE, 0xE4, 0x68, 0xF6, 0x7F, 0x28, 0xD5, 0xEA, 0x2D, 0x1A, 0x8D,
    0xAC, 0x69, 0xC9, 0xC5, 0x97, 0xC3, 0x49, 0x63, 0xA2, 0x6B, 0x9E, 0x4B, 0x07, 0xC8, 0xE9, 0xE2,
    0x95, 0xC0, 0x57, 0xA0, 0x7B, 0x84, 0xA3, 0x76, 0x3E, 0xA7, 0xC4, 0x8F, 0xDD, 0x29, 0x5A, 0xD3,
    0x59, 0x8B, 0x93, 0x66, 0x05, 0x9B, 0xBB, 0x15, 0xA6, 0x36, 0xB0, 0x25, 0xB5, 0x8E, 0x65, 0x6C,
    0x0B, 0x50, 0x96, 0xE0, 0xF4, 0xB7, 0x31, 0xAE, 0xED, 0x18, 0x3A, 0xB8, 0x70, 0x51, 0x42, 0x7D,
    0xD2, 0x4E, 0xFA, 0xEC, 0x7E, 0xB3, 0x12, 0xBA, 0xB9, 0xB6, 0xD4, 0xFF, 0xF8, 0x44, 0x09, 0xE1,
    0x53, 0x9A, 0x5D, 0x11, 0xC2, 0xBC, 0xEF, 0x1E, 0xD6, 0x56, 0xFE, 0x0E, 0xF3, 0xE8, 0x04, 0x62,
    0x88, 0xB4, 0xB1, 0xEB, 0x4C, 0x5B, 0xE5, 0xEE, 0x23, 0x5F, 0x54, 0x2F, 0x60, 0x6D, 0x99, 0x81,
    0x4A, 0x6E, 0xF7, 0x82, 0x2E, 0x5C, 0x08, 0x77, 0xA9, 0x85, 0x52, 0x13, 0x43, 0xFD, 0x20, 0xC7,
    0xCA, 0x06, 0xC1, 0xCD, 0xDE, 0x87, 0x72, 0xBD, 0x78, 0x0F, 0x5E, 0x10, 0xDA, 0xFB, 0x3C, 0x67,
    0x74, 0x0D, 0x47, 0xD1, 0x7C, 0x32, 0x41, 0x89, 0x9C, 0xE6, 0xA4, 0xFC, 0x30, 0x40, 0xA8, 0x6A,
    // ... (total 4096 bytes - truncated for brevity, full table needs to be added)
};


/**
 * Decrypt client-to-server packet
 */
BYTE* DecryptClientPacket(const BYTE* source, int length, int* outLength, BYTE key)
{
    *outLength = 0;

    // Validate inputs
    if (key >= 0x10) {
        Log("[Crypto] DecryptClient: Key too large (%d >= 0x10)\r\n", key);
        return NULL;
    }

    if (length < 5) {
        Log("[Crypto] DecryptClient: Packet too small (%d < 5)\r\n", length);
        return NULL;
    }

    // Allocate buffer
    BYTE* buffer = (BYTE*)AllocMem(length);
    if (!buffer) {
        Log("[Crypto] DecryptClient: Failed to allocate buffer\r\n");
        return NULL;
    }

    memcpy(buffer, source, length);

    // Decrypt: buffer[4] = CryptTable2[(key << 8) + source[0]]
    buffer[4] = CryptTable2[(key << 8) + source[0]];

    // XOR decryption
    for (int i = 8; i < length; i++) {
        buffer[i] ^= buffer[i - 4];
    }

    // Extract output (skip first 5 bytes)
    int outputLen = length - 5;
    BYTE* output = (BYTE*)AllocMem(outputLen);
    if (!output) {
        FreeMem(buffer);
        Log("[Crypto] DecryptClient: Failed to allocate output\r\n");
        return NULL;
    }

    memcpy(output, buffer + 5, outputLen);
    FreeMem(buffer);

    *outLength = outputLen;
    return output;
}

/**
 * Decrypt server-to-client packet (with LZO decompression)
 */
BYTE* DecryptServerPacket(const BYTE* source, int length, int* outLength, BYTE key)
{
    *outLength = 0;

    // Validate inputs
    if (key >= 0x10) {
        Log("[Crypto] DecryptServer: Key too large (%d >= 0x10)\r\n", key);
        return NULL;
    }

    if (length < 8) {
        Log("[Crypto] DecryptServer: Packet too small (%d < 8)\r\n", length);
        return NULL;
    }

    // Calculate oracle byte
    BYTE oracleByte = CryptTable2[(key << 8) + source[0]];

    // Allocate buffer
    BYTE* buffer = (BYTE*)AllocMem(length);
    if (!buffer) {
        Log("[Crypto] DecryptServer: Failed to allocate buffer\r\n");
        return NULL;
    }

    memcpy(buffer, source, length);

    // XOR with oracle byte
    buffer[7] ^= oracleByte;

    // XOR decryption
    for (int i = 10; i < length; i++) {
        buffer[i] ^= buffer[i - 4];
    }

    // Extract compressed data (skip first 8 bytes)
    int compressedLen = length - 8;
    BYTE* compressedData = (BYTE*)AllocMem(compressedLen);
    if (!compressedData) {
        FreeMem(buffer);
        Log("[Crypto] DecryptServer: Failed to allocate compressed buffer\r\n");
        return NULL;
    }

    memcpy(compressedData, buffer + 8, compressedLen);
    FreeMem(buffer);

    // TODO: Decompress using MiniLZO
    // For now, return compressed data as-is
    // You need to implement MiniLZO decompression here
    
    Log("[Crypto] DecryptServer: Warning - LZO decompression not implemented\r\n");
    Log("[Crypto] DecryptServer: Returning compressed data (%d bytes)\r\n", compressedLen);
    
    *outLength = compressedLen;
    return compressedData;
}

/**
 * Encrypt client-to-server packet
 */
BYTE* EncryptClientPacket(const BYTE* data, int length, int* outLength, BYTE key)
{
    *outLength = 0;

    // Validate inputs
    if (key >= 0x10) {
        Log("[Crypto] EncryptClient: Key too large (%d >= 0x10)\r\n", key);
        return NULL;
    }

    // Allocate buffer: [RandomByte:1][Data:length]
    int bufferLen = 5 + length;
    BYTE* buffer = (BYTE*)AllocMem(bufferLen);
    if (!buffer) {
        Log("[Crypto] EncryptClient: Failed to allocate buffer\r\n");
        return NULL;
    }

    // Generate random byte for position 0
    srand((unsigned int)time(NULL));
    buffer[0] = (BYTE)(rand() % 256);

    // Calculate oracle byte
    BYTE oracleByte = CryptTable2[(key << 8) + buffer[0]];
    buffer[4] = oracleByte;

    // Copy data to buffer (starting at position 5)
    memcpy(buffer + 5, data, length);

    // Apply XOR encryption (reverse of decryption)
    for (int i = bufferLen - 1; i >= 8; i--) {
        buffer[i] ^= buffer[i - 4];
    }

    *outLength = bufferLen;
    return buffer;
}

/**
 * Encrypt server-to-client packet (with LZO compression)
 */
BYTE* EncryptServerPacket(const BYTE* data, int length, int* outLength, BYTE key)
{
    *outLength = 0;

    // Validate inputs
    if (key >= 0x10) {
        Log("[Crypto] EncryptServer: Key too large (%d >= 0x10)\r\n", key);
        return NULL;
    }

    // TODO: Compress data using MiniLZO
    // For now, assume data is already compressed
    
    Log("[Crypto] EncryptServer: Warning - LZO compression not implemented\r\n");
    Log("[Crypto] EncryptServer: Treating data as pre-compressed\r\n");
    
    BYTE* compressedData = (BYTE*)AllocMem(length);
    if (!compressedData) {
        Log("[Crypto] EncryptServer: Failed to allocate compressed buffer\r\n");
        return NULL;
    }
    memcpy(compressedData, data, length);
    int compressedLen = length;

    // Allocate buffer: [Random:1][Reserved:1][Length:2][Unknown:4][CompressedData...]
    int bufferLen = 8 + compressedLen;
    BYTE* buffer = (BYTE*)AllocMem(bufferLen);
    if (!buffer) {
        FreeMem(compressedData);
        Log("[Crypto] EncryptServer: Failed to allocate buffer\r\n");
        return NULL;
    }

    // Generate random byte for position 0
    srand((unsigned int)time(NULL));
    buffer[0] = (BYTE)(rand() % 256);

    // Calculate oracle byte
    BYTE oracleByte = CryptTable2[(key << 8) + buffer[0]];

    // Copy compressed data to buffer (starting at position 8)
    memcpy(buffer + 8, compressedData, compressedLen);
    FreeMem(compressedData);

    // Apply XOR encryption (reverse of decryption)
    for (int i = bufferLen - 1; i >= 10; i--) {
        buffer[i] ^= buffer[i - 4];
    }

    // XOR position 7 with oracle byte
    buffer[7] ^= oracleByte;

    *outLength = bufferLen;
    return buffer;
}
