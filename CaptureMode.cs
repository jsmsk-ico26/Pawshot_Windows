namespace Pawshot_Windows
{
    /// <summary>
    /// キャプチャ後の動作モードを表します。
    /// </summary>
    public enum CaptureMode
    {
        /// <summary>キャプチャ後にアノテーションウィンドウを開きます（Ctrl+Shift+6）。</summary>
        Annotation,

        /// <summary>キャプチャ後にOCRを実行し、テキストをクリップボードにコピーします（Ctrl+Shift+7）。</summary>
        Ocr
    }
}
