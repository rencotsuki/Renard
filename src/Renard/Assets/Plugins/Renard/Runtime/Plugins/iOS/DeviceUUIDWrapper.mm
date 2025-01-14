#import <UnityFramework/UnityFramework-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif
    const char* getPersistentUUID(const char* key) {
       NSString *keyString = [NSString stringWithUTF8String:key];
           NSString *uuid = [DeviceUUID getPersistentUUIDForKey:keyString];
       return [uuid UTF8String];
    }
    void deletePersistentUUID(const char* key) {
       NSString *keyString = [NSString stringWithUTF8String:key];
       [DeviceUUID deletePersistentUUIDForKey:keyString];
    }
#ifdef __cplusplus
}
#endif
