import UIKit
import MetalKit

@objc public class ExternalDisplayManager: NSObject {
    private var externalWindow: UIWindow?
    private var metalLayer: CAMetalLayer?
    private var texturePtr: UnsafeRawPointer?

    @objc public func setupExternalDisplay(_ texturePtr: UnsafeRawPointer) {
        self.texturePtr = texturePtr

        NotificationCenter.default.addObserver(
            self,
            selector: #selector(screenDidConnect),
            name: UIScreen.didConnectNotification,
            object: nil
        )
        NotificationCenter.default.addObserver(
            self,
            selector: #selector(screenDidDisconnect),
            name: UIScreen.didDisconnectNotification,
            object: nil
        )
    }

    @objc private func screenDidConnect(notification: Notification) {
        guard let newScreen = notification.object as? UIScreen else { return }

        externalWindow = UIWindow(frame: newScreen.bounds)
        externalWindow?.screen = newScreen

        let metalView = MTKView(frame: newScreen.bounds)
        metalView.device = MTLCreateSystemDefaultDevice()
        metalLayer = metalView.layer as? CAMetalLayer

        // RenderTextureをMetalLayerにバインド
        if let texturePtr = self.texturePtr,
           let texture = texturePtr.assumingMemoryBound(to: MTLTexture?.self).pointee {
            metalLayer?.contents = texture
        } else {
            print("Error: Failed to bind texture.")
        }

        let viewController = UIViewController()
        viewController.view = metalView

        externalWindow?.rootViewController = viewController
        externalWindow?.isHidden = false
    }

    @objc private func screenDidDisconnect(notification: Notification) {
        externalWindow?.isHidden = true
        externalWindow = nil
    }
}
