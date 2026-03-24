using System;

namespace Pawshot_Windows
{
    /// <summary>
    /// アプリケーションの設定情報を保持するクラスです。
    /// </summary>
    public class AppConfig
    {
        // 外部保存先のパス (空の場合はアプリ直下の Captures フォルダなど)
        public string SavePath { get; set; } = string.Empty;
        
        // ホットキーの設定 (現在は固定ですが将来的に変更可能にするための準備)
        public string HotkeyModifiers { get; set; } = "Ctrl+Shift";
        public string HotkeyKey { get; set; } = "6";
        
        // 自動保存を有効にするか
        public bool AutoSaveEnabled { get; set; } = true;

        // OCRの有無
        public bool OcrEnabled { get; set; } = true;
    }
}
