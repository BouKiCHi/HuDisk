# HuDisk

## 説明
ディスクイメージを操作するためのソフトウェアです。  
動作には.NET Framework 4.7が必要になります。

## 使用方法
```
Usage HuDisk image.d88 [files...] [options...]
```


* ファイルをディスクイメージに追加
```
 hudisk -a image.d88 file.bin
 hudisk -a image.d2 file.bin
```
ディスクイメージファイルのimage.d88がなければ自動的に作成します。  
また、拡張子を2dとすることで、プレーンフォーマットも利用できます。


* ディスクイメージからファイルを取り出す
```
 hudisk -x image.d88 
 hudisk -x image.d88 test*.bin
```

* フォーマットのみを行う
```
 hudisk --format image.d88
```

## オプション

+ -a,--add files ...  ファイル追加
+ -x,--extract files ... ファイル展開
+ -l,--list ... ファイル一覧
  
+ --format ... ファイルが存在しても新規でフォーマットする
+ -i,--ipl {iplname} ... IPLバイナリとして追加する
+ -r,--read  {address} ... 読み出しアドレスの設定
+ -g,--go  {address} ... 実行アドレスの設定
  
+ -h,-?,--help ... 表示


## 履歴
* ver 1.01
デフォルトの動作モードをリストにした。
ファイルの日付をリストで表示するようにした。

* ver 1.01
ファイルの日付を設定するようにした。

* ver 1.0
初期リリース

