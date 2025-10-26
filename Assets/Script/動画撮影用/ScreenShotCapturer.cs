using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

/// <summary>
/// スクリーンショットをキャプチャするサンプル
/// </summary>
public class ScreenShotCapturer : MonoBehaviour
{
    private void Update()
    {
        // スペースキーが押されたら
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // スクリーンショットを保存
            CaptureScreenShot("ScreenShot.png");
        }
    }

    // 画面全体のスクリーンショットを保存する
    private void CaptureScreenShot(string filePath)
    {
        ScreenCapture.CaptureScreenshot(@"C:\Users\wakam\Desktop\アイデア\ファイル整理ゲーム\steam\ゲームプレイ画像");
    }
}