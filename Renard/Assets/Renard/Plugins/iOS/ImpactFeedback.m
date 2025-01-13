#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

void TriggerHapticFeedback(int intensity)
{
    if (@available(iOS 13.0, *)) {
        UIImpactFeedbackGenerator *generator;
        switch (intensity) {
            case 0:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                break;

            case 2:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                break;

            case 1:
            default:
                generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                break;
        }
        [generator impactOccurred];
    }
}
