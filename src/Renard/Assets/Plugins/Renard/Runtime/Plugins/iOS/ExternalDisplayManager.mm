#import <UnityFramework/UnityFramework-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif
	void SetupExternalDisplay(void* texturePtr) {
	    ExternalDisplayManager *manager = [ExternalDisplayManager new];
	    [manager setupExternalDisplay:texturePtr];
	}
#ifdef __cplusplus
}
#endif
