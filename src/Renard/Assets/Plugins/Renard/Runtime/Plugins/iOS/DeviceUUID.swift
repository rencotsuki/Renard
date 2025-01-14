import Foundation

@objc public class DeviceUUID: NSObject {
    @objc public static func getPersistentUUID(forKey key: String) -> String {
	
        // 既存のUUIDを取得確認
        if let existingUUID = KeychainHelper.get(key: key) {
            return existingUUID
        }
        
        // 新規UUIDを生成して保存
        let newUUID = UUID().uuidString
        KeychainHelper.save(key: key, value: newUUID)
        return newUUID
    }
    
    @objc public static func deletePersistentUUID(forKey key: String) {        
        KeychainHelper.delete(key: key)
    }
}

class KeychainHelper {
    static func delete(key: String) {
        let query = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrAccount: key
        ] as CFDictionary
        
        SecItemDelete(query)
    }
    
    static func save(key: String, value: String) {
        let data = value.data(using: .utf8)!
        let query = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrAccount: key,
            kSecValueData: data
        ] as CFDictionary
        
        SecItemDelete(query)
        SecItemAdd(query, nil)
    }
    
    static func get(key: String) -> String? {
        let query = [
            kSecClass: kSecClassGenericPassword,
            kSecAttrAccount: key,
            kSecReturnData: true,
            kSecMatchLimit: kSecMatchLimitOne
        ] as CFDictionary
        
        var dataTypeRef: AnyObject?
        let status = SecItemCopyMatching(query, &dataTypeRef)
        
        if status == errSecSuccess, let data = dataTypeRef as? Data {
            return String(data: data, encoding: .utf8)
        }
        return nil
    }
}
