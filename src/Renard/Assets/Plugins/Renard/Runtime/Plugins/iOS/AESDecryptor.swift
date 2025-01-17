import Foundation
import CommonCrypto

@objc public class AESDecryptor: NSObject {
    @objc public func decryptAES256(cipherTextBase64: String, key: String, iv: String) -> String? {
        guard let cipherData = Data(base64Encoded: cipherTextBase64),
              let keyData = key.data(using: .utf8),
              let ivData = iv.data(using: .utf8) else {
            return nil
        }

        let bufferSize = cipherData.count + kCCBlockSizeAES128
        var buffer = Data(count: bufferSize)
        var numBytesDecrypted: size_t = 0

        let cryptStatus = buffer.withUnsafeMutableBytes { bufferBytes in
            cipherData.withUnsafeBytes { cipherBytes in
                keyData.withUnsafeBytes { keyBytes in
                    ivData.withUnsafeBytes { ivBytes in
                        CCCrypt(
                            CCOperation(kCCDecrypt),
                            CCAlgorithm(kCCAlgorithmAES),
                            CCOptions(kCCOptionPKCS7Padding),
                            keyBytes.baseAddress, kCCKeySizeAES256,
                            ivBytes.baseAddress,
                            cipherBytes.baseAddress, cipherData.count,
                            bufferBytes.baseAddress, bufferSize,
                            &numBytesDecrypted
                        )
                    }
                }
            }
        }

        guard cryptStatus == kCCSuccess else { return nil }
        
        return String(data: buffer.prefix(numBytesDecrypted), encoding: .utf8)
    }
}
