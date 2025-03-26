# Renard  
　カジュアルコンテンツの基本パッケージ  

## ビルド対象プラットフォーム  
　<img src="https://img.shields.io/badge/-Windows-40AEF0.svg?logo=windows11&style=flat"> <img src="https://img.shields.io/badge/-OSX-000000.svg?logo=apple&style=flat"> <img src="https://img.shields.io/badge/-iOS-000000.svg?logo=apple&style=flat"> <img src="https://img.shields.io/badge/-Android-436653.svg?logo=android&style=flat">  
　※プラットフォームによっては一部機能がまだ対応されていないものがあります。  

## Unity動作環境  
　<img src="https://img.shields.io/badge/-Unity2022.3以降-000000.svg?logo=unity&style=flat"> <img src="https://img.shields.io/badge/-Unity2023.3以降-000000.svg?logo=unity&style=flat"> <img src="https://img.shields.io/badge/-Unity6000.0以降-000000.svg?logo=unity&style=flat">

## UnityPackageManagerのインストールURL  
　`git@github.com:rencotsuki/Renard.git?path=src/Renard/Assets/Plugins/Renard`  

　※注意  
　Windows環境においてはUnityPackageManagerがOpenSSHを利用するので環境設定が必要になります。  
　ssh認証が通らないとパッケージがインストールされないので注意が必要です。  
　さらにWindows環境でSourceTreeのPutty/Plink設定を使っている場合は、  
　OpenSSH設定に変更する必要があります。  

## 備考  
　URPを利用する際は、ScriptingDefineSymbols設定に  
　`RENARD_USING_URP`  
　を設定する必要があります。  

