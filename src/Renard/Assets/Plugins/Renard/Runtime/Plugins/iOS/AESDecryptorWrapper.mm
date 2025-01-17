#import <UnityFramework/UnityFramework-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif
    const char* UnityDecryptAES256(const char* cipherTextBase64, const char* key, const char* iv) {
        @autoreleasepool {
            NSString *cipherText = [NSString stringWithUTF8String:cipherTextBase64];
            NSString *keyString = [NSString stringWithUTF8String:key];
            NSString *ivString = [NSString stringWithUTF8String:iv];

            NSString *decryptedString = [AESDecryptor decryptAES256:cipherTextBase64:keyString:ivString];
            
            if (decryptedString == nil) {
                return NULL;
            }

            // C •¶Žš—ñ‚É•ÏŠ·‚µ‚Ä Unity ‚É“n‚·
            const char* utf8String = [decryptedString UTF8String];
            char* result = strdup(utf8String);
            return result;
        }
    }
#ifdef __cplusplus
}
#endif
