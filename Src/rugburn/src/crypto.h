/**
 * PangYa Encryption/Decryption Functions
 * Ported from PangCrypt (https://github.com/pangyatools/PangCrypt)
 */

#ifndef CRYPTO_H
#define CRYPTO_H

#include "common.h"

/**
 * Decrypt client-to-server packet
 * 
 * @param source Encrypted packet data
 * @param length Length of encrypted data
 * @param outLength Output: length of decrypted data
 * @param key Encryption key (0-15)
 * @return Decrypted data (caller must free with FreeMem), or NULL on error
 */
BYTE* DecryptClientPacket(const BYTE* source, int length, int* outLength, BYTE key);

/**
 * Decrypt server-to-client packet
 * 
 * @param source Encrypted packet data
 * @param length Length of encrypted data
 * @param outLength Output: length of decrypted data (decompressed)
 * @param key Encryption key (0-15)
 * @return Decrypted and decompressed data (caller must free with FreeMem), or NULL on error
 */
BYTE* DecryptServerPacket(const BYTE* source, int length, int* outLength, BYTE key);

/**
 * Encrypt client-to-server packet
 * 
 * @param data Plaintext packet data
 * @param length Length of plaintext data
 * @param outLength Output: length of encrypted data
 * @param key Encryption key (0-15)
 * @return Encrypted data (caller must free with FreeMem), or NULL on error
 */
BYTE* EncryptClientPacket(const BYTE* data, int length, int* outLength, BYTE key);

/**
 * Encrypt server-to-client packet
 * 
 * @param data Plaintext packet data (will be compressed)
 * @param length Length of plaintext data
 * @param outLength Output: length of encrypted data
 * @param key Encryption key (0-15)
 * @return Encrypted data (caller must free with FreeMem), or NULL on error
 */
BYTE* EncryptServerPacket(const BYTE* data, int length, int* outLength, BYTE key);

#endif // CRYPTO_H
