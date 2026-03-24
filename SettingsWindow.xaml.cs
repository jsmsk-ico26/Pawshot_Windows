using System.Windows;

namespace Pawshot_Windows
{
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            // 現在の設定を画面に反映
            PathTextBox.Text = ConfigManager.Current.SavePath;
            AutoSaveCheckBox.IsChecked = ConfigManager.Current.AutoSaveEnabled;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            // Windows Forms のフォルダ選択ダイアログを使用 (UseWindowsForms=trueにより可能)
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "画像を保存するフォルダを選択してください";
                dialog.UseDescriptionForTitle = true;
                
                if (!string.IsNullOrEmpty(PathTextBox.Text))
                {
                    dialog.InitialDirectory = PathTextBox.Text;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 設定を更新して保存
            ConfigManager.Current.SavePath = PathTextBox.Text;
            ConfigManager.Current.AutoSaveEnabled = AutoSaveCheckBox.IsChecked ?? true;
            ConfigManager.Save();
            
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
