using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CookingSystem.EditorTools
{
    /// <summary>
    /// Configures uniform, jitter-free multi-frame slicing for all 2D Kitchen Sprite Sheets
    /// and generates crisp half-body cropped customer portraits into Assets/GeneratedCarinderia/Portraits/.
    /// </summary>
    public static class SpriteSheetAssetConfigurator
    {
        public const string SpriteSheetFolder = "Assets/2D Kitchen/Sprite Sheet";
        public const string PortraitsOutputDir = "Assets/GeneratedCarinderia/Portraits";

        public static void EnsureAllSpriteSheetsAndPortraitsReady()
        {
            EnsureUniformGridSlices("berry.png", cols: 4, yBottom: 0, sliceHeight: 360);
            EnsureUniformGridSlices("Calamansi.png", cols: 4, yBottom: 0, sliceHeight: 360);
            // Chopping.png (640x420) has art in top band (top-down Y 37..179 => Unity bottom-up Y 220..410)
            EnsureUniformGridSlices("Chopping.png", cols: 3, yBottom: 220, sliceHeight: 190);
            // Mixing 1.png (640x420 transparent background) has 3 centered bowl frames at X centers 100, 300, 504
            EnsureCustomSlices("Mixing 1.png", new[]
            {
                new Rect(0f, 250f, 200f, 160f),
                new Rect(200f, 250f, 200f, 160f),
                new Rect(404f, 250f, 200f, 160f)
            });
            EnsureUniformGridSlices("Mixing.png", cols: 3, yBottom: 0, sliceHeight: 282);
            EnsureUniformGridSlices("Flames.png", cols: 4, yBottom: 0, sliceHeight: 200);
            EnsureUniformGridSlices("Foam.png", cols: 3, yBottom: 0, sliceHeight: 200);

            // Progress Icon.png (3 circles) & Satisfaction.png (3 emojis) are cleanly sliced; verify readability
            EnsureMultipleSpriteMode("Progress Icon.png");
            EnsureMultipleSpriteMode("Satisfaction.png");
        }

        public static Sprite[] LoadOrderedFrames(string fileName)
        {
            string path = $"{SpriteSheetFolder}/{fileName}";
            if (!File.Exists(path)) return Array.Empty<Sprite>();

            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.rect.x)
                .ToArray();
        }

        public static Sprite EnsureHalfBodyCustomerPortrait(string customerName, string sourceAssetPath)
        {
            if (string.IsNullOrEmpty(sourceAssetPath) || !File.Exists(sourceAssetPath)) return null;

            Directory.CreateDirectory(PortraitsOutputDir);
            string safeName = string.Concat(customerName.Where(char.IsLetterOrDigit));
            string outPath = $"{PortraitsOutputDir}/HalfBody_{safeName}.png";

            if (!File.Exists(outPath))
            {
                GenerateHalfBodyPortraitPng(sourceAssetPath, outPath);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureSingleSpriteImporter(outPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outPath);
            return sprite;
        }

        private static void GenerateHalfBodyPortraitPng(string sourcePath, string outPath)
        {
            byte[] rawBytes = File.ReadAllBytes(sourcePath);
            Texture2D src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!src.LoadImage(rawBytes)) return;

                int w = src.width;
                int h = src.height;
                Color32[] pixels = src.GetPixels32();

                // Unity GetPixels32 is bottom-up (y=0 bottom, y=h-1 top).
                int minX = w;
                int maxX = 0;
                int minY = h;
                int maxY = 0;

                for (int y = 0; y < h; y += 2)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x += 2)
                    {
                        if (pixels[row + x].a > 18)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                if (maxY <= minY || maxX <= minX)
                {
                    minX = 0;
                    maxX = w - 1;
                    minY = 0;
                    maxY = h - 1;
                }

                int visH = maxY - minY;
                // Upper 54% of visible character height (head + hair/ears + shoulders + torso to waist)
                int cropTop = Mathf.Min(h - 1, maxY + Mathf.RoundToInt(visH * 0.03f));
                int cropHeight = Mathf.Max(64, Mathf.RoundToInt(visH * 0.54f));
                int cropBottom = Mathf.Max(0, cropTop - cropHeight);
                cropHeight = Mathf.Max(1, cropTop - cropBottom);

                // Find tight horizontal bounds strictly inside the upper half-body band so wide skirts don't shrink head!
                int upperMinX = w;
                int upperMaxX = 0;
                for (int y = cropBottom; y < cropTop; y += 2)
                {
                    int row = y * w;
                    for (int x = 0; x < w; x += 2)
                    {
                        if (pixels[row + x].a > 18)
                        {
                            if (x < upperMinX) upperMinX = x;
                            if (x > upperMaxX) upperMaxX = x;
                        }
                    }
                }

                if (upperMaxX <= upperMinX)
                {
                    upperMinX = minX;
                    upperMaxX = maxX;
                }

                int upperVisW = upperMaxX - upperMinX;
                int centerX = (upperMinX + upperMaxX) / 2;
                // Keep a clean ~0.86 portrait width-to-height ratio so character fills the frame closely & uniformly
                int targetCropW = Mathf.Max(Mathf.RoundToInt(upperVisW * 1.12f), Mathf.RoundToInt(cropHeight * 0.86f));
                int cropLeft = Mathf.Clamp(centerX - targetCropW / 2, 0, Mathf.Max(0, w - 1));
                int cropRight = Mathf.Clamp(cropLeft + targetCropW, cropLeft + 1, w);
                int cropW = cropRight - cropLeft;

                // Render cropped region onto a clean 512x580 canvas bottom-aligned so waist sits right on the counter ledge
                const int outW = 480;
                const int outH = 540;
                Texture2D dst = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
                Color32[] outPixels = new Color32[outW * outH];

                float scale = Mathf.Min((float)outW / cropW, (float)outH / cropHeight);
                int scaledW = Mathf.RoundToInt(cropW * scale);
                int scaledH = Mathf.RoundToInt(cropHeight * scale);
                int offsetX = (outW - scaledW) / 2;
                int offsetY = 0; // Flush bottom alignment so character rises cleanly from behind counter ledge

                for (int oy = 0; oy < scaledH; oy++)
                {
                    int dstY = offsetY + oy;
                    if (dstY < 0 || dstY >= outH) continue;
                    float srcYf = cropBottom + (oy / (float)Mathf.Max(1, scaledH - 1)) * (cropHeight - 1);
                    int sy = Mathf.Clamp(Mathf.RoundToInt(srcYf), 0, h - 1);

                    for (int ox = 0; ox < scaledW; ox++)
                    {
                        int dstX = offsetX + ox;
                        if (dstX < 0 || dstX >= outW) continue;
                        float srcXf = cropLeft + (ox / (float)Mathf.Max(1, scaledW - 1)) * (cropW - 1);
                        int sx = Mathf.Clamp(Mathf.RoundToInt(srcXf), 0, w - 1);

                        outPixels[dstY * outW + dstX] = pixels[sy * w + sx];
                    }
                }

                dst.SetPixels32(outPixels);
                dst.Apply();
                File.WriteAllBytes(outPath, dst.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(dst);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(src);
            }
        }

        private static void ConfigureSingleSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static void EnsureMultipleSpriteMode(string fileName)
        {
            string path = $"{SpriteSheetFolder}/{fileName}";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static void EnsureCustomSlices(string fileName, Rect[] rects)
        {
            string path = $"{SpriteSheetFolder}/{fileName}";
            if (!File.Exists(path) || rects == null || rects.Length == 0) return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            Sprite[] existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            bool alreadyMatched = importer.textureType == TextureImporterType.Sprite &&
                                  importer.spriteImportMode == SpriteImportMode.Multiple &&
                                  existing.Length == rects.Length;
            if (alreadyMatched)
            {
                for (int i = 0; i < rects.Length; i++)
                {
                    Rect r = existing[i].rect;
                    Rect t = rects[i];
                    if (Mathf.Abs(r.x - t.x) > 2f || Mathf.Abs(r.y - t.y) > 2f ||
                        Mathf.Abs(r.width - t.width) > 2f || Mathf.Abs(r.height - t.height) > 2f)
                    {
                        alreadyMatched = false;
                        break;
                    }
                }
            }
            if (alreadyMatched) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var so = new SerializedObject(importer);
            SerializedProperty spritesProp = so.FindProperty("m_SpriteSheet.m_Sprites");
            if (spritesProp != null)
            {
                spritesProp.arraySize = rects.Length;
                for (int i = 0; i < rects.Length; i++)
                {
                    SerializedProperty elem = spritesProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("m_Name").stringValue = $"{baseName}_{i}";
                    elem.FindPropertyRelative("m_Rect").rectValue = rects[i];
                    elem.FindPropertyRelative("m_Alignment").intValue = (int)SpriteAlignment.Center;
                    elem.FindPropertyRelative("m_Pivot").vector2Value = new Vector2(0.5f, 0.5f);
                    elem.FindPropertyRelative("m_Border").vector4Value = Vector4.zero;
                    SerializedProperty idProp = elem.FindPropertyRelative("m_SpriteID");
                    if (idProp != null && string.IsNullOrEmpty(idProp.stringValue))
                    {
                        idProp.stringValue = GUID.Generate().ToString();
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            importer.SaveAndReimport();
            Debug.Log($"[SpriteSheetAssetConfigurator] Sliced '{fileName}' into {rects.Length} centered transparent frames.");
        }

        private static void EnsureUniformGridSlices(string fileName, int cols, int yBottom, int sliceHeight)
        {
            string path = $"{SpriteSheetFolder}/{fileName}";
            if (!File.Exists(path)) return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int texW = tex != null ? tex.width : 0;
            int texH = tex != null ? tex.height : 0;
            if (texW <= 0 || texH <= 0)
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D probe = new Texture2D(2, 2);
                if (probe.LoadImage(bytes))
                {
                    texW = probe.width;
                    texH = probe.height;
                }
                UnityEngine.Object.DestroyImmediate(probe);
            }
            if (texW <= 0 || texH <= 0) return;

            int cellW = texW / cols;
            int clampedBottom = Mathf.Clamp(yBottom, 0, Mathf.Max(0, texH - 1));
            int clampedH = Mathf.Clamp(sliceHeight, 1, texH - clampedBottom);
            string baseName = Path.GetFileNameWithoutExtension(fileName);

            Sprite[] existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.rect.x).ToArray();
            bool alreadyUniform = importer.textureType == TextureImporterType.Sprite &&
                                  importer.spriteImportMode == SpriteImportMode.Multiple &&
                                  existing.Length == cols;
            if (alreadyUniform)
            {
                for (int i = 0; i < cols; i++)
                {
                    Sprite s = existing[i];
                    if (Mathf.Abs(s.rect.x - i * cellW) > 2f ||
                        Mathf.Abs(s.rect.y - clampedBottom) > 2f ||
                        Mathf.Abs(s.rect.width - cellW) > 2f ||
                        Mathf.Abs(s.rect.height - clampedH) > 2f)
                    {
                        alreadyUniform = false;
                        break;
                    }
                }
            }

            if (alreadyUniform) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;

            var so = new SerializedObject(importer);
            SerializedProperty spritesProp = so.FindProperty("m_SpriteSheet.m_Sprites");
            if (spritesProp != null)
            {
                spritesProp.arraySize = cols;
                for (int i = 0; i < cols; i++)
                {
                    SerializedProperty elem = spritesProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("m_Name").stringValue = $"{baseName}_{i}";
                    elem.FindPropertyRelative("m_Rect").rectValue = new Rect(i * cellW, clampedBottom, cellW, clampedH);
                    elem.FindPropertyRelative("m_Alignment").intValue = (int)SpriteAlignment.Center;
                    elem.FindPropertyRelative("m_Pivot").vector2Value = new Vector2(0.5f, 0.5f);
                    elem.FindPropertyRelative("m_Border").vector4Value = Vector4.zero;
                    SerializedProperty idProp = elem.FindPropertyRelative("m_SpriteID");
                    if (idProp != null && string.IsNullOrEmpty(idProp.stringValue))
                    {
                        idProp.stringValue = GUID.Generate().ToString();
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            importer.SaveAndReimport();
            Debug.Log($"[SpriteSheetAssetConfigurator] Sliced '{fileName}' into {cols} uniform frames ({cellW}x{clampedH}).");
        }
    }
}
