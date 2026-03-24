using System;
using System.IO;
using System.Text.Json;

namespace Pawshot_Windows
{
    /// <summary>
    /// 設定ファイル (config.json) の読み書きを管理する静的クラスです。
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public static AppConfig Current { get; private set; } = new AppConfig();

        /// <summary>
        /// 設定をファイルから読み込みます。ファイルがない場合はデフォルト値を使用します。
        /// </summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    // 初回起動時などはデフォルト設定でファイルを作成
                    Save();
                }
            }
            catch (Exception)
            {
                Current = new AppConfig();
            }
        }

        /// <summary>
        /// 現在の設定をファイルに保存します。
        /// </summary>
        public static void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // 保存失敗時はログ出力など（今回は省略）
            }
        }
    }
}
