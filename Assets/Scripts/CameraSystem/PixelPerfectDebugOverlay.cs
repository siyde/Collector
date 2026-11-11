using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Collector.CameraSystem
{
    /// <summary>
    /// F1 ile açılıp kapanan küçük bilgi kutusu: ekran çözünürlüğü, piksel ölçeği ve
    /// ölçeğin tam sayı olup olmadığı. Yalnızca Editor ve Development Build'de çalışır.
    /// </summary>
    [RequireComponent(typeof(PixelPerfectCamera))]
    public class PixelPerfectDebugOverlay : MonoBehaviour
    {
        [SerializeField] bool visible;

        PixelPerfectCamera pixelPerfectCamera;
        GUIStyle style;

        void Awake()
        {
            if (!Debug.isDebugBuild) { enabled = false; return; }
            pixelPerfectCamera = GetComponent<PixelPerfectCamera>();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 14 };
                style.normal.textColor = Color.white;
            }

            int refX = pixelPerfectCamera.refResolutionX;
            int refY = pixelPerfectCamera.refResolutionY;
            float scale = Mathf.Min(Screen.width / (float)refX, Screen.height / (float)refY);
            bool isInteger = Screen.width % refX == 0 && Screen.height % refY == 0;

            string text =
                $"Ekran: {Screen.width}x{Screen.height}\n" +
                $"Referans: {refX}x{refY} @ {pixelPerfectCamera.assetsPPU} PPU\n" +
                $"Pixel Ratio: {pixelPerfectCamera.pixelRatio}x   Ölçek: {scale:0.##}x\n" +
                (isInteger ? "Tam sayı ölçek: pikseller kusursuz" : "Tam sayı değil: hafif yumuşama olabilir");

            GUI.Box(new Rect(8, 8, 340, 88), GUIContent.none);
            GUI.Label(new Rect(16, 12, 330, 84), text, style);
        }
    }
}
