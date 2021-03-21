# HuDisk ディスクイメージ操作ツール

## 説明

X1で使用されるHuBASIC形式のディスクイメージを読み書きするオープンソースのツールです。  
C#で書かれていて、コンソール上で動作します。

## 動作環境

.NET Framework 4.7

## 使用方法
```
Usage HuDisk image.d88 [files...] [options...]
```

## 基本動作
* ファイルをディスクイメージに追加
```
 hudisk -a image.d88 file.bin
 hudisk -a image.2d file.bin
```
ディスクイメージファイルのimage.d88がなければ自動的に作成します。  
また、拡張子を2d、または2hdとすることで、プレーンフォーマットも利用できます。  

* ディスクイメージからファイルを取り出す
```
 hudisk -x image.d88 
 hudisk -x image.d88 test*.bin
```

* フォーマットを行う
```
# 2Dフォーマット(デフォルト)
 hudisk --format image.d88

# 2HDフォーマット
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

# 「/」をパス区切りで使用
 hudisk image.d88 --path DATA/TEXT -a text.txt 

# 「\」をパス区切りで使用
 hudisk image.d88 --path DATA\TEXT -a text.txt 

```


### IPLについて

iplオプションで追加されたファイルはIPLプログラムとして登録されます。  
入力例では、IPL表示名はIPLNAMEになります。  
IPLプログラムとして登録する場合はファイルサイズ分の連続したクラスタが必要になります。

* ファイルをIPLとしてディスクイメージに追加
```
 hudisk -a image.d88 iplprogram.bin --ipl IPLNAME
```

## オプション

### 基本動作

+ -a,--add files ...  ファイル追加
+ -x,--extract files ... ファイル展開
+ -l,--list ... ファイル一覧
+ -d,--delete ... ファイル削除
  
### 動作設定

+ --format ... イメージをフォーマットするように設定
+ --type {type} ディスク種別を選択する。2HD/2DD/2D。
+ -i,--ipl {iplname} ... IPLバイナリとして追加する
+ -r,--read  {address} ... 読み出しアドレスの設定
+ -g,--go  {address} ... 実行アドレスの設定
+ --x1s ... x1save.exe互換モードに設定
+ --name {name} 出力エントリ名をnameに設定
+ --path {path} イメージ内ディレクトリ名をpathに設定
+ -v,--verbose 詳細出力モードに設定
+ --ascii 展開時に強制的にASCIIモードに設定
+ --binary 展開時に強制的にバイナリモードに設定

### ヘルプ
+ -h,-?,--help ... 表示

## 説明

### 新規ディスクの作成

フォーマットオプションを指定して、ファイルを追加しない場合に空のディスクが作成できます。

```
hudisk image.d88 --format
```

### X1SAVE.EXE互換モード
このモードは、アロケーションテーブルの出力をX1SAVE.EXEと同等になるように調整します。
具体的には次の条件になります。

* アロケーションテーブルの最終セクタ数の値が1以上でその値に-1を加算
* ただしファイルサイズの下位8bitが0の場合はその値に1を加算

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
+ [ディスクフォーマット](docs/DISK.md)
+ [HuBASICフォーマット](docs/HuBASIC_Format.md)

## 履歴
+ [CHANGELOG.md](CHANGELOG.md)
