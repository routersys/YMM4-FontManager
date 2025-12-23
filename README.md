# フォントマネージャー for YMM4

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](#)
[![Release](https://img.shields.io/github/v/release/routersys/YMM4-FontManager.svg)](https://github.com/routersys/YMM4-FontManager/releases)

YukkuriMovieMaker4 (YMM4) 向けの多機能フォント管理プラグインです。
Google Fonts をシームレスに検索・プレビューし、システムへのインストールを簡単に行うことができます。

![Thumbnail](https://github.com/routersys/YMM4-FontManager/blob/main/docs/FonrManager.png?raw=true)

## 特徴

* **クラウドフォント対応**: Google Fonts の膨大なライブラリにアクセス。
* **簡単インストール**: ワンクリックでフォントのインストール・アンインストールが可能。
* **リアルタイムプレビュー**: 任意のテキストでフォントの形状を即座に確認。
* **お気に入り管理**: よく使うフォントを「お気に入り」に登録して素早くアクセス。
* **高度なフィルタリング**: カテゴリ（Serif, Sans-serifなど）やサブセット、フォント名での検索に対応。
* **多言語対応**: UIはYMM4で設定可能なすべての言語に対応しています。

## インストール

1. リリースページから最新の `FontManager` プラグインをダウンロードします。
2. `FontManager.ymme`をクリックしてインストールします。
3. インストール完了です。

## 使い方

### メイン画面
* **リスト切り替え**: ラジオボタンで「クラウドフォント」と「インストール済み」リストを切り替えます。
* **プレビュー**: 各フォントカード内のテキストボックスに文字を入力して確認できます。
* **インストール**: 未インストールのフォントには「インストール」ボタンが表示されます。
* **設定**: 「ファイル」-> 「設定」->「フォントマネージャー」から設定画面へアクセスできます。

### 設定項目
* **Google Fonts API設定**: デフォルトではGitHub経由でリストを取得しますが、APIキーを設定することでGoogleから直接最新データを取得可能です。
* **RAMキャッシュ**: 「キャッシュファイルをRAMに展開する」を有効にすると、プレビュー表示が高速化されます。
