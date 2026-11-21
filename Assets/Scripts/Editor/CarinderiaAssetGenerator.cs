using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CookingSystem.EditorTools
{
    /// <summary>
    /// Generates crisp PNG sprites for the Carinderia environment (wooden table,
    /// checkered plastic table mat, wall backdrop) and high-res UI / Prep Tray elements
    /// matching UI.svg and Kitchen Tray.svg inside Assets/2D Kitchen/Generated/.
    /// </summary>
    public static class CarinderiaAssetGenerator
    {
        public const string GeneratedDir = "Assets/2D Kitchen/Generated";

        public static void EnsureAllGeneratedAssets()
        {
            if (!Directory.Exists(GeneratedDir))
            {
                Directory.CreateDirectory(GeneratedDir);
            }

            bool anyCreated = false;

            anyCreated |= EnsurePng("Carinderia_WallBackdrop.png", 1280, 360, GenerateWallBackdrop);
            anyCreated |= EnsurePng("Carinderia_WoodenTable.png", 1280, 480, GenerateWoodenTable);
            anyCreated |= EnsurePng("Carinderia_PlasticMat.png", 1200, 440, GeneratePlasticMat);

            // UI.svg-matched sprites
            anyCreated |= EnsurePng("UI_ScorePill.png", 320, 72, GenerateScorePill);
            anyCreated |= EnsurePng("UI_TimerBarBg.png", 760, 76, GenerateTimerBarBg);
            anyCreated |= EnsurePng("UI_TimerTrackSlot.png", 260, 40, GenerateTimerTrackSlot);
            anyCreated |= EnsurePng("UI_TimerFillPill.png", 280, 28, GenerateTimerFillPill);
            anyCreated |= EnsurePng("UI_PatienceFillPill.png", 28, 340, GeneratePatienceFillPill);
            anyCreated |= EnsurePng("UI_StarFilled.png", 128, 128, GenerateStarFilled);
            anyCreated |= EnsurePng("UI_StarEmpty.png", 128, 128, GenerateStarEmpty);
            anyCreated |= EnsurePng("UI_BtnRestart.png", 160, 68, GenerateRestartBtn);
            anyCreated |= EnsurePng("UI_BtnMenu.png", 160, 68, GenerateMenuBtn);

            // Kitchen Tray.svg-matched sprites
            anyCreated |= EnsurePng("Prep_FramePanel.png", 780, 230, GeneratePrepFrame);
            anyCreated |= EnsurePng("Prep_WoodBoard.png", 360, 140, GenerateWoodBoard);
            anyCreated |= EnsurePng("Prep_BtnChop.png", 140, 48, GenerateBtnChop);
            anyCreated |= EnsurePng("Prep_BtnMix.png", 140, 48, GenerateBtnMix);
            anyCreated |= EnsurePng("Prep_BtnClr.png", 104, 48, GenerateBtnClr);
            anyCreated |= EnsurePng("Prep_CardLadle.png", 220, 96, GenerateCardLadle);
            anyCreated |= EnsurePng("Prep_CardSpatula.png", 220, 96, GenerateCardSpatula);
            anyCreated |= EnsurePng("Prep_ReadyDock.png", 210, 196, GenerateReadyDock);

            if (anyCreated)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureSpriteImporters();
        }

        [MenuItem("Tools/Game Scene/Regenerate Carinderia & UI Sprites")]
        public static void RegenerateAll()
        {
            if (Directory.Exists(GeneratedDir))
            {
                foreach (string file in Directory.GetFiles(GeneratedDir, "*.png"))
                {
                    File.Delete(file);
                }
            }
            EnsureAllGeneratedAssets();
            Debug.Log("[CarinderiaAssetGenerator] Regenerated all Carinderia table, plastic mat, and UI sprites.");
        }

        private static bool EnsurePng(string fileName, int width, int height, Func<int, int, Texture2D> generator)
        {
            string fullPath = Path.Combine(GeneratedDir, fileName).Replace("\\", "/");
            if (File.Exists(fullPath)) return false;

            Texture2D tex = generator(width, height);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(fullPath, png);
            return true;
        }

        private static void ConfigureSpriteImporters()
        {
            string[] files = Directory.GetFiles(GeneratedDir, "*.png");
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = files[i].Replace("\\", "/");
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spriteImportMode != SpriteImportMode.Single)
                    {
                        importer.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }
                    if (!importer.alphaIsTransparency)
                    {
                        importer.alphaIsTransparency = true;
                        changed = true;
                    }
                    if (importer.mipmapEnabled)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Bilinear)
                    {
                        importer.filterMode = FilterMode.Bilinear;
                        changed = true;
                    }
                    if (changed)
                    {
                        importer.SaveAndReimport();
                    }
                }
            }
        }

        private static Texture2D GenerateWallBackdrop(int w, int h)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float ny = y / (float)h;
                // Lower wood wainscot band + warm carinderia cream tiles above
                if (ny < 0.16f)
                {
                    float grain = Mathf.Sin(x * 0.04f) * 0.03f + Hash(x / 6, y / 2) * 0.025f;
                    return Shift(Hex("5C3A21"), grain);
                }
                if (ny < 0.18f)
                {
                    return Hex("3F2514");
                }

                Color tileA = Hex("F4EAD8");
                Color tileB = Hex("EEE1CB");
                Color grout = Hex("D4C4AB");
                int tileW = 80;
                int tileH = 48;
                int gx = x % tileW;
                int gy = y % tileH;
                bool isGrout = gx <= 2 || gy <= 2;
                if (isGrout) return grout;

                int row = y / tileH;
                int col = (x + (row % 2) * (tileW / 2)) / tileW;
                Color baseTile = ((row + col) % 2 == 0) ? tileA : tileB;
                float shade = Mathf.Lerp(-0.04f, 0.04f, ny);
                return Shift(baseTile, shade);
            });
        }

        private static Texture2D GenerateWoodenTable(int w, int h)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float nx = x / (float)w;
                float ny = y / (float)h;

                // Dark table front lip (bottom 8%) and counter back shadow (top 4%)
                if (ny < 0.06f)
                {
                    return Shift(Hex("432613"), -0.04f * (1f - ny / 0.06f));
                }
                if (ny < 0.075f)
                {
                    return Hex("2D180B");
                }

                // 6 warm Narra/Mahogany horizontal planks
                int plankCount = 6;
                float plankCoord = ny * plankCount;
                int plankIdx = Mathf.Clamp((int)plankCoord, 0, plankCount - 1);
                float plankLocalY = plankCoord - plankIdx;

                Color[] plankPalette =
                {
                    Hex("85512B"), Hex("7A4925"), Hex("8D5831"),
                    Hex("764623"), Hex("89542D"), Hex("7E4C27")
                };
                Color baseCol = plankPalette[plankIdx % plankPalette.Length];

                // Wood grain waves
                float wave = Mathf.Sin(nx * 28f + plankIdx * 1.9f + Mathf.Sin(ny * 42f) * 1.4f);
                float fineGrain = Mathf.Sin(y * 0.55f + Mathf.Sin(x * 0.015f + plankIdx) * 3.5f);
                float noise = (Hash(x / 5, y) - 0.5f) * 0.03f;
                Color col = Shift(baseCol, wave * 0.035f + fineGrain * 0.025f + noise);

                // Plank seam groove
                if (plankLocalY < 0.045f)
                {
                    col = Color.Lerp(col, Hex("3E2210"), 0.72f);
                }
                else if (plankLocalY < 0.09f)
                {
                    col = Shift(col, 0.035f);
                }

                // Subtle varnish sheen across center
                float sheen = Mathf.Exp(-Mathf.Pow((nx - 0.5f) * 2.1f, 2f)) * 0.06f;
                return Shift(col, sheen);
            });
        }

        private static Texture2D GeneratePlasticMat(int w, int h)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float d = RoundedRectSdf(x - w * 0.5f, y - h * 0.5f, w * 0.5f - 10f, h * 0.5f - 10f, 28f);
                if (d > 4f) return new Color(0f, 0f, 0f, 0f);

                // Soft drop shadow outside mat rim
                if (d > 0f)
                {
                    float a = Mathf.Clamp01(1f - d / 4f) * 0.35f;
                    return new Color(0.08f, 0.04f, 0.02f, a);
                }

                // Scalloped / clear vinyl outer rim
                if (d > -10f)
                {
                    Color rim = new Color(0.98f, 0.96f, 0.92f, 0.88f);
                    if (d > -2.5f) rim = Color.Lerp(rim, new Color(0.75f, 0.25f, 0.20f, 0.92f), 0.55f);
                    return rim;
                }

                // Translucent Filipino Carinderia Gingham / Checkered Plastic Mantel
                int checkSize = 44;
                bool cx = ((x / checkSize) % 2) == 0;
                bool cy = ((y / checkSize) % 2) == 0;

                Color cream = new Color(0.99f, 0.96f, 0.89f, 0.78f);
                Color stripeRed = new Color(0.87f, 0.31f, 0.24f, 0.74f);
                Color crossDark = new Color(0.76f, 0.20f, 0.16f, 0.82f);

                Color matColor;
                if (cx && cy) matColor = cream;
                else if (cx || cy) matColor = stripeRed;
                else matColor = crossDark;

                // Subtle dashed stitch inside border
                if (d > -22f && d < -17f && ((x + y) % 18 < 10))
                {
                    matColor = Color.Lerp(matColor, Color.white, 0.45f);
                }

                // Glossy plastic diagonal highlight strips (simulate clear PVC vinyl cover!)
                float diag = ((x * 0.55f + y) / (w * 0.55f + h));
                float glossA = Mathf.Exp(-Mathf.Pow((diag - 0.34f) * 14f, 2f)) * 0.22f;
                float glossB = Mathf.Exp(-Mathf.Pow((diag - 0.72f) * 18f, 2f)) * 0.15f;
                float gloss = glossA + glossB;
                matColor.r = Mathf.Clamp01(matColor.r + gloss);
                matColor.g = Mathf.Clamp01(matColor.g + gloss);
                matColor.b = Mathf.Clamp01(matColor.b + gloss);
                matColor.a = Mathf.Clamp01(matColor.a + gloss * 0.4f);

                return matColor;
            });
        }

        private static Texture2D GenerateScorePill(int w, int h)
        {
            return DrawCapsule(w, h, 3f, Hex("FFFFFF"), (nx, ny, insideDist) =>
            {
                if (insideDist > -3f) return Hex("FF7AE5");
                Color top = Hex("D926A9");
                Color bot = Hex("7E1996");
                Color col = Color.Lerp(bot, top, ny);
                if (Mathf.Abs(nx - 0.5f) < 0.006f && ny > 0.22f && ny < 0.78f)
                {
                    return Hex("FF7AE5");
                }
                return col;
            });
        }

        private static Texture2D GenerateTimerBarBg(int w, int h)
        {
            return DrawCapsule(w, h, 4f, Hex("5A2D14"), (nx, ny, insideDist) =>
            {
                if (insideDist > -2.5f) return Hex("B87344");
                Color baseWood = Hex("8B4E2B");
                // Left orange clock circle
                float clockCx = 40f;
                float clockCy = h * 0.5f;
                float distToClock = Vector2.Distance(new Vector2(nx * w, ny * h), new Vector2(clockCx, clockCy));
                if (distToClock < 23f)
                {
                    if (distToClock > 19.5f) return Hex("FFF3D6");
                    // Clock hands
                    float dx = nx * w - clockCx;
                    float dy = ny * h - clockCy;
                    if ((Mathf.Abs(dx) < 2f && dy >= -1f && dy < 10f) || (Mathf.Abs(dy + 1f - dx * 0.35f) < 2f && dx >= 0f && dx < 9f))
                    {
                        return Color.white;
                    }
                    return Hex("F59E0B");
                }
                return Shift(baseWood, (ny - 0.5f) * 0.06f);
            });
        }

        private static Texture2D GenerateTimerTrackSlot(int w, int h)
        {
            return DrawCapsule(w, h, 2f, Hex("2F1407"), (_, ny, _) => Shift(Hex("431E0C"), (ny - 0.5f) * 0.04f));
        }

        private static Texture2D GenerateTimerFillPill(int w, int h)
        {
            return DrawCapsule(w, h, 1.5f, new Color(0.95f, 0.98f, 0.95f, 0.65f), (_, ny, _) =>
            {
                // Clean glossy white base so Image.color tints crisply (Green/Amber/Red)
                if (ny >= 0.58f && ny <= 0.84f)
                {
                    return new Color(1f, 1f, 1f, 1f);
                }
                if (ny < 0.28f)
                {
                    return new Color(0.84f, 0.88f, 0.85f, 1f);
                }
                return new Color(0.95f, 0.97f, 0.95f, 1f);
            });
        }

        private static Texture2D GeneratePatienceFillPill(int w, int h)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float d = RoundedRectSdf(x - w * 0.5f, y - h * 0.5f, w * 0.5f - 1.5f, h * 0.5f - 1.5f, w * 0.45f);
                if (d > 1.0f) return new Color(0f, 0f, 0f, 0f);
                float nx = x / (float)w;
                float edgeAlpha = Mathf.Clamp01(1.0f - d);
                // Crisp vertical capsule with subtle left glossy highlight stripe
                float highlight = (nx >= 0.22f && nx <= 0.44f) ? 1.0f : (nx > 0.72f ? 0.85f : 0.94f);
                return new Color(highlight, highlight, highlight, edgeAlpha);
            });
        }

        private static Texture2D GenerateRestartBtn(int w, int h)
        {
            return DrawRoundedPanel(w, h, 18f, 3.5f, Hex("D9A066"), (nx, ny, _) =>
            {
                Color c = Hex("4D2C1A");
                float cx = 28f;
                float cy = h * 0.5f;
                float dist = Vector2.Distance(new Vector2(nx * w, ny * h), new Vector2(cx, cy));
                if (dist > 7f && dist < 11.5f && !(nx * w > cx + 2f && ny * h < cy - 1f))
                {
                    return Hex("FBBF24");
                }
                return Shift(c, (ny - 0.5f) * 0.05f);
            });
        }

        private static Texture2D GenerateMenuBtn(int w, int h)
        {
            return DrawRoundedPanel(w, h, 18f, 3.5f, Hex("F59E0B"), (nx, ny, _) =>
            {
                Color c = Hex("6E231E");
                float px = nx * w;
                float py = ny * h;
                if (px >= 18f && px <= 34f &&
                    (Mathf.Abs(py - h * 0.65f) < 1.8f || Mathf.Abs(py - h * 0.5f) < 1.8f || Mathf.Abs(py - h * 0.35f) < 1.8f))
                {
                    return Hex("FFF3D6");
                }
                return Shift(c, (ny - 0.5f) * 0.05f);
            });
        }

        private static Texture2D GeneratePrepFrame(int w, int h)
        {
            return DrawRoundedPanel(w, h, 20f, 3.5f, Hex("111827"), (_, ny, _) =>
                Color.Lerp(Hex("1F2937"), Hex("374151"), ny));
        }

        private static Texture2D GenerateWoodBoard(int w, int h)
        {
            return DrawRoundedPanel(w, h, 14f, 3f, Hex("78350F"), (nx, ny, _) =>
            {
                Color baseCol = Color.Lerp(Hex("B87344"), Hex("E6B88A"), ny * 0.8f + nx * 0.2f);
                // Subtle plank lines matching Kitchen Tray.svg
                float lineDist = Mathf.Min(Mathf.Abs(ny - 0.25f), Mathf.Min(Mathf.Abs(ny - 0.5f), Mathf.Abs(ny - 0.75f)));
                if (lineDist < 0.012f)
                {
                    return Color.Lerp(baseCol, Hex("9A3412"), 0.25f);
                }
                return baseCol;
            });
        }

        private static Texture2D GenerateBtnChop(int w, int h) =>
            DrawBevelButton(w, h, Hex("EA580C"), Hex("9A3412"), Hex("FED7AA"));

        private static Texture2D GenerateBtnMix(int w, int h) =>
            DrawBevelButton(w, h, Hex("0284C7"), Hex("075985"), Hex("BAE6FD"));

        private static Texture2D GenerateBtnClr(int w, int h) =>
            DrawBevelButton(w, h, Hex("4B5563"), Hex("1F2937"), Hex("D1D5DB"));

        private static Texture2D GenerateCardLadle(int w, int h) =>
            DrawRoundedPanel(w, h, 16f, 3f, Hex("F47280"), (_, ny, _) =>
                Color.Lerp(Hex("111827"), Hex("1F2937"), ny));

        private static Texture2D GenerateCardSpatula(int w, int h) =>
            DrawRoundedPanel(w, h, 16f, 3f, Hex("9CA3AF"), (_, ny, _) =>
                Color.Lerp(Hex("111827"), Hex("1F2937"), ny));

        private static Texture2D GenerateReadyDock(int w, int h)
        {
            return DrawRoundedPanel(w, h, 16f, 3f, Hex("1F2937"), (nx, ny, _) =>
            {
                float px = nx * w;
                float py = ny * h;
                if (ny > 0.79f && nx > 0.08f && nx < 0.92f)
                {
                    return Hex("F59E0B");
                }
                float dist = Vector2.Distance(new Vector2(px, py), new Vector2(w * 0.5f, h * 0.42f));
                if (dist < w * 0.34f)
                {
                    if (dist > w * 0.30f) return Hex("E5E7EB");
                    return Hex("111827");
                }
                return Color.Lerp(Hex("1F2937"), Hex("374151"), ny);
            });
        }

        private static Texture2D DrawBevelButton(int w, int h, Color face, Color shadow, Color border)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float dOuter = RoundedRectSdf(x - w * 0.5f, y - h * 0.5f, w * 0.5f - 2f, h * 0.5f - 2f, 12f);
                if (dOuter > 1f) return new Color(0f, 0f, 0f, 0f);
                if (y < 6) return shadow;
                float dFace = RoundedRectSdf(x - w * 0.5f, y - (h * 0.5f + 2f), w * 0.5f - 2f, h * 0.5f - 4f, 12f);
                if (dFace > -2.5f) return border;
                return Shift(face, (y / (float)h - 0.5f) * 0.08f);
            });
        }

        private static Texture2D DrawCapsule(int w, int h, float borderWidth, Color borderColor, Func<float, float, float, Color> fill)
        {
            return DrawRoundedPanel(w, h, h * 0.48f, borderWidth, borderColor, fill);
        }

        private static Texture2D DrawRoundedPanel(int w, int h, float radius, float borderWidth, Color borderColor, Func<float, float, float, Color> fill)
        {
            return BuildTexture(w, h, (x, y) =>
            {
                float d = RoundedRectSdf(x - w * 0.5f, y - h * 0.5f, w * 0.5f - 2f, h * 0.5f - 2f, radius);
                if (d > 1.2f) return new Color(0f, 0f, 0f, 0f);

                Color c = d > -borderWidth ? borderColor : fill(x / (float)w, y / (float)h, d);
                float edgeAlpha = Mathf.Clamp01(1.2f - d);
                c.a *= edgeAlpha;
                return c;
            });
        }

        private static Texture2D BuildTexture(int w, int h, Func<int, int, Color> pixelFunc)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, mipChain: false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    pixels[row + x] = pixelFunc(x, y);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return tex;
        }

        private static Texture2D GenerateStarFilled(int w, int h)
        {
            return GenerateStarTexture(w, h, Hex("FBBF24"), Hex("FEF08A"), Hex("78350F"));
        }

        private static Texture2D GenerateStarEmpty(int w, int h)
        {
            return GenerateStarTexture(w, h, new Color(0.24f, 0.16f, 0.12f, 0.85f), new Color(0.30f, 0.21f, 0.16f, 0.85f), Hex("140C08"));
        }

        private static Texture2D GenerateStarTexture(int w, int h, Color fill, Color highlight, Color borderColor)
        {
            float outerRadius = w * 0.43f;
            return BuildTexture(w, h, (x, y) =>
            {
                float px = x - w * 0.5f;
                float py = y - h * 0.5f;
                float d = Star5Sdf(px, py, outerRadius, 0.46f);
                if (d > 2.2f) return new Color(0f, 0f, 0f, 0f);

                Color c = d > -2.8f
                    ? borderColor
                    : Color.Lerp(fill, highlight, Mathf.Clamp01((py / outerRadius) * 0.55f + 0.35f));

                if (d > 0f)
                {
                    c.a *= Mathf.Clamp01(1f - d / 2.2f);
                }
                return c;
            });
        }

        private static float Star5Sdf(float px, float py, float r, float rf)
        {
            const float an = Mathf.PI / 5f;
            float angle = Mathf.Atan2(px, py);
            float bn = ((angle % (2f * an)) + (2f * an)) % (2f * an) - an;
            float len = Mathf.Sqrt(px * px + py * py);
            float qx = len * Mathf.Abs(Mathf.Sin(bn));
            float qy = len * Mathf.Cos(bn);

            float ax = 0f;
            float ay = r;
            float bx = rf * r * Mathf.Sin(an);
            float by = rf * r * Mathf.Cos(an);

            float abx = bx - ax;
            float aby = by - ay;
            float apx = qx - ax;
            float apy = qy - ay;

            float t = Mathf.Clamp01((apx * abx + apy * aby) / (abx * abx + aby * aby));
            float cx = ax + abx * t;
            float cy = ay + aby * t;
            float dist = Mathf.Sqrt((qx - cx) * (qx - cx) + (qy - cy) * (qy - cy));
            float sign = Mathf.Sign(abx * apy - aby * apx);
            return dist * (sign == 0f ? -1f : sign);
        }

        private static float RoundedRectSdf(float px, float py, float halfW, float halfH, float r)
        {
            float qx = Mathf.Abs(px) - (halfW - r);
            float qy = Mathf.Abs(py) - (halfH - r);
            float outLen = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return outLen + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static float Hash(int x, int y)
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }

        private static Color Shift(Color c, float delta)
        {
            return new Color(
                Mathf.Clamp01(c.r + delta),
                Mathf.Clamp01(c.g + delta),
                Mathf.Clamp01(c.b + delta),
                c.a);
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;
        }
    }
}
