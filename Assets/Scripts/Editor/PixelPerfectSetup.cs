using System.Text;
using Collector.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Collector.EditorTools
{
    /// <summary>
    /// AŞAMA 2: Pixel Perfect ayarlarını uygular ve doğrular.
    /// Menü: Collector > Aşama 2 > Pixel Perfect Ayarlarını Uygula ve Doğrula
    /// (CollectorRoom sahnesi açıkken çalıştır.)
    /// </summary>
    public static class PixelPerfectSetup
    {
        const int RefWidth = AIArtPixelator.TargetWidth;
        const int RefHeight = AIArtPixelator.TargetHeight;
        const int PPU = PixelArtTextureImporter.PixelsPerUnit;

        [MenuItem("Collector/Aşama 2/Pixel Perfect Ayarlarını Uygula ve Doğrula")]
        public static void ApplyAndValidate()
        {
            var report = new StringBuilder();
            int fixes = 0, warnings = 0;

            // 1) Kamera ------------------------------------------------------
            var ppc = Object.FindFirstObjectByType<PixelPerfectCamera>();
            if (ppc == null)
            {
                EditorUtility.DisplayDialog("Aşama 2", "Açık sahnede PixelPerfectCamera bulunamadı. Önce CollectorRoom sahnesini aç.", "Tamam");
                return;
            }

            ConfigureCamera(ppc);
            if (!ppc.TryGetComponent<PixelPerfectDebugOverlay>(out _))
                Undo.AddComponent<PixelPerfectDebugOverlay>(ppc.gameObject);
            EditorSceneManager.MarkSceneDirty(ppc.gameObject.scene);
            EditorSceneManager.SaveScene(ppc.gameObject.scene);
            report.AppendLine($"Kamera: Orthographic Size {RefHeight / (2f * PPU)}, {RefWidth}x{RefHeight} @ {PPU} PPU, Upscale Render Texture, Stretch Fill");

            // 2) URP: MSAA kapalı, render scale 1 --------------------------------
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (urp.msaaSampleCount != 1 || !Mathf.Approximately(urp.renderScale, 1f))
                {
                    urp.msaaSampleCount = 1;
                    urp.renderScale = 1f;
                    EditorUtility.SetDirty(urp);
                    fixes++;
                    report.AppendLine("Düzeltildi: " + urp.name + " (MSAA kapalı, Render Scale 1)");
                }
            }

            // 3) Player: 1920x1080 (640x360'ın tam 3 katı) -------------------------
            PlayerSettings.defaultScreenWidth = RefWidth * 3;
            PlayerSettings.defaultScreenHeight = RefHeight * 3;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            report.AppendLine("Player: 1920x1080, Fullscreen Window, pencere boyutlandırılabilir");

            // 4) Görseller: Point / sıkıştırmasız / mipmap yok / 32 PPU -------------
            int checkedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/Art/Reference/")) continue; // referans görsel sahnede kullanılmaz
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                checkedCount++;

                bool wrong = importer.filterMode != FilterMode.Point
                          || importer.textureCompression != TextureImporterCompression.Uncompressed
                          || importer.mipmapEnabled
                          || !Mathf.Approximately(importer.spritePixelsPerUnit, PPU);
                if (wrong)
                {
                    importer.spritePixelsPerUnit = PPU;
                    importer.SaveAndReimport(); // PixelArtTextureImporter diğer ayarları zorlar
                    fixes++;
                    report.AppendLine("Düzeltildi: " + path);
                }

                // Tam ekran görünümler birebir 640x360 olmalı
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (path.StartsWith("Assets/Art/Views/") && (fileName.EndsWith("_Front") || fileName.EndsWith("_Background")))
                {
                    importer.GetSourceTextureWidthAndHeight(out int w, out int h);
                    if (w != RefWidth || h != RefHeight)
                    {
                        warnings++;
                        report.AppendLine($"UYARI: {path} {w}x{h} — {RefWidth}x{RefHeight} olmalı. Pixelator aracından geçir.");
                    }
                }
            }
            report.AppendLine($"Görseller kontrol edildi: {checkedCount}");

            AssetDatabase.SaveAssets();

            var summary = $"Pixel Perfect kurulumu tamam. Düzeltme: {fixes}, Uyarı: {warnings}";
            Debug.Log("[Collector] " + summary + "\n" + report);
            EditorUtility.DisplayDialog("Aşama 2", summary + "\n\nAyrıntılar Console'da.", "Tamam");
        }

        public static void ConfigureCamera(PixelPerfectCamera ppc)
        {
            var cam = ppc.GetComponent<Camera>();
            Undo.RecordObjects(new Object[] { ppc, cam }, "Pixel Perfect Ayarları");

            cam.orthographic = true;
            cam.orthographicSize = RefHeight / (2f * PPU); // 5.625

            ppc.assetsPPU = PPU;
            ppc.refResolutionX = RefWidth;
            ppc.refResolutionY = RefHeight;
            // Sahne 640x360'lık bir render texture'a çizilip ekrana büyütülür: gerçek piksel görünümü.
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            // Oda görseli tam 640x360 olduğu için kenarlarda boşluk kalmasın; 1080p/1440p/4K'da
            // ölçek zaten tam sayıdır, diğer çözünürlüklerde RetroAA filtresi hafifçe yumuşatır.
            ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
        }
    }
}
