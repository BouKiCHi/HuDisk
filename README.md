# HuDisk ディスクイメージ操作ツール

## 説明
X1のHuBASIC形式のフォーマットを読み書きするオープンソースのツールです。  
C#で書かれていて、コンソール上で動作します。

## 動作環境
.NET Framework 4.7

## 使用方法
```
Usage HuDisk image.d88 [files...] [options...]
```


* ファイルをディスクイメージに追加
```
 hudisk -a image.d88 file.bin
 hudisk -a image.2d file.bin

 hudisk -a image.d88 iplprogram.bin --ipl IPLNAME
```
ディスクイメージファイルのimage.d88がなければ自動的に作成します。  
また、拡張子を2dとすることで、プレーンフォーマットも利用できます。  

### IPLについて

iplオプションで追加されたファイルはIPLプログラムとして登録されます。  
入力例では、IPL表示名はIPLNAMEになります。  
IPLプログラムとして登録する場合はファイルサイズ分の連続したクラスタが必要になります。


* ディスクイメージからファイルを取り出す
```
 hudisk -x image.d88 
 hudisk -x image.d88 test*.bin
```

* フォーマットのみを行う
```
 hudisk --format image.d88
```

* 2HDフォーマットを行う
```
 hudisk --format --type 2hd image.d88
```

* 展開時にファイル名を指定する
```
 hudisk image.d88 -x localfile.txt --name entry.txt 
```

* 展開時にファイルを標準出力する
```
 hudisk image.d88 --name entry.txt -x - 
```

* text.txtというファイルをディレクトリDATA/TEXTに追加する
```
 hudisk image.d88 --path DATA/TEXT -a text.txt 
```

## オプション

+ -a,--add files ...  ファイル追加
+ -x,--extract files ... ファイル展開
+ -l,--list ... ファイル一覧
+ -d,--delete ... ファイル削除

+ --format ... イメージをフォーマットする
+ --type {type} ディスク種別を選択する。2HD/2DD/2D。
+ -i,--ipl {iplname} ... IPLバイナリとして追加する
+ -r,--read  {address} ... 読み出しアドレスの設定
+ -g,--go  {address} ... 実行アドレスの設定
+ --x1s ... x1save.exe互換モードに設定
+ --name {name} エントリ名をnameに設定
+ --path {path} ディレクトリ名をpathに設定

+ -h,-?,--help ... 表示

## 説明
### X1SAVE.EXE互換モード
このモードは、アロケーションテーブルをX1SAVE.EXEの出力に合わせる調整を行うオプションです。
具体的には最終セクタ数が1以上で-1にします。ただしファイルサイズの下位8bitが0であれば、+1をします。

### 読み出しアドレスの設定

ファイルを読み出すアドレスを指定します。接頭辞なしの16進数です。  
```
hudisk image.d88 file.bin --read 1234 --go 1234
```
とすると、読み出しアドレスが$1234(10進数で4660)に設定されます。

アドレス未設定時の初期値は以下の通りです。
* 読み出しアドレス = $0000
* 実行アドレス = $0000
* ファイル書き込みの基本はバイナリモード

## 参考
+ [X1SAVE.EXE(Ｘ１ＥＭＵ用 イメージファイルツール集内、リンク切れ)](http://www.geocities.co.jp/SiliconValley-SanJose/3949/)
+ [X1DiskExplorer](http://ceeezet.syuriken.jp/)


## 技術情報
+ [ディスクフォーマット](doc/DISK.md)
+ [HuBASICフォーマット](doc/HuBASIC_Format.md)

## 履歴
+ [CHANGELOG.md](CHANGELOG.md)
