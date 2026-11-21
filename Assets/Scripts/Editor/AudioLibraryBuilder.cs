using System.IO;
using CookingSystem.Audio;
using UnityEditor;
using UnityEngine;

namespace CookingSystem.Editor
{
    /// <summary>
    /// Automatically optimizes AudioImporter settings for Assets/Luto SFX/*.wav
    /// and maintains Assets/Resources/GameAudioLibrary.asset so all BGM and SFX clips
    /// stay synced and ready across Editor, Desktop, and WebGL builds.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioLibraryBuilder
    {
        private const string SfxFolder = "Assets/Luto SFX";
        private const string ResourcesFolder = "Assets/Resources";
        public const string LibraryAssetPath = "Assets/Resources/GameAudioLibrary.asset";

        static AudioLibraryBuilder()
        {
            EditorApplication.delayCall += EnsureAudioLibraryReady;
        }

        private static void EnsureAudioLibraryReady()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Directory.Exists(SfxFolder)) return;

            GameAudioLibrary existing = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryAssetPath);
            bool needsRebuild = existing == null ||
                                existing.mainMenuBg == null ||
                                existing.gameBg == null ||
                                existing.endlessModeBg == null ||
                                existing.click == null ||
                                existing.chop == null ||
                                existing.mix == null ||
                                existing.cooked == null ||
                                existing.overcooked == null ||
                                existing.drink == null ||
                                existing.satisfied == null ||
                                existing.notSatisfied == null ||
                                existing.comboStreak == null ||
                                existing.victory == null ||
                                existing.fail == null;

            if (needsRebuild)
            {
                BuildOrUpdateAudioLibrary(logSummary: false);
            }
            else
            {
                OptimizeClipImportSettings(logChanges: false);
            }
        }

        [MenuItem("Tools/Audio/Rebuild Luto SFX Audio Library")]
        public static void RebuildFromMenu()
        {
            BuildOrUpdateAudioLibrary(logSummary: true);
        }

        public static GameAudioLibrary BuildOrUpdateAudioLibrary(bool logSummary = true)
        {
            OptimizeClipImportSettings(logSummary);

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            GameAudioLibrary library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryAssetPath);
            bool isNew = library == null;
            if (isNew)
            {
                library = ScriptableObject.CreateInstance<GameAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryAssetPath);
            }

            library.mainMenuBg = LoadClip("mainmenubg.wav");
            library.gameBg = LoadClip("gameBG.wav");
            library.endlessModeBg = LoadClip("EndlessmodeBG.wav");

            library.click = LoadClip("click.wav");
            library.chop = LoadClip("chop.wav");
            library.mix = LoadClip("mix.wav");
            library.cooked = LoadClip("Cooked.wav");
            library.overcooked = LoadClip("overcooked.wav");
            library.drink = LoadClip("Drink.wav");
            library.satisfied = LoadClip("Satisfied.wav");
            library.notSatisfied = LoadClip("Not Satisfied.wav");
            library.comboStreak = LoadClip("combo streak.wav");
            library.victory = LoadClip("Victory.wav");
            library.fail = LoadClip("fail.wav");

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            if (logSummary)
            {
                Debug.Log($"[AudioLibraryBuilder] {(isNew ? "Created" : "Updated")} {LibraryAssetPath} with all Luto SFX clips.");
            }

            return library;
        }

        private static void OptimizeClipImportSettings(bool logChanges)
        {
            // Long looping tracks & 11.8s fire alert: CompressedInMemory + Vorbis (low RAM + zero hitch on Editor & WebGL)
            ConfigureAudioImporter("mainmenubg.wav", AudioClipLoadType.CompressedInMemory, 0.72f, forceMono: false, logChanges);
            ConfigureAudioImporter("gameBG.wav", AudioClipLoadType.CompressedInMemory, 0.72f, forceMono: false, logChanges);
            ConfigureAudioImporter("EndlessmodeBG.wav", AudioClipLoadType.CompressedInMemory, 0.72f, forceMono: false, logChanges);
            ConfigureAudioImporter("overcooked.wav", AudioClipLoadType.CompressedInMemory, 0.80f, forceMono: true, logChanges);

            // Short responsive SFX: DecompressOnLoad + preloaded for instant zero-latency clicks & kitchen cues
            ConfigureAudioImporter("click.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("chop.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("mix.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("Cooked.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("Drink.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("Satisfied.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("Not Satisfied.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("combo streak.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: true, logChanges);
            ConfigureAudioImporter("Victory.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: false, logChanges);
            ConfigureAudioImporter("fail.wav", AudioClipLoadType.DecompressOnLoad, 0.88f, forceMono: false, logChanges);
        }

        private static void ConfigureAudioImporter(
            string fileName,
            AudioClipLoadType targetLoadType,
            float targetQuality,
            bool forceMono,
            bool logChanges)
        {
            string path = $"{SfxFolder}/{fileName}";
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            bool changed = false;

            if (settings.loadType != targetLoadType)
            {
                settings.loadType = targetLoadType;
                changed = true;
            }
            if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
            {
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                changed = true;
            }
            if (Mathf.Abs(settings.quality - targetQuality) > 0.02f)
            {
                settings.quality = targetQuality;
                changed = true;
            }
            if (!settings.preloadAudioData)
            {
                settings.preloadAudioData = true;
                changed = true;
            }
            if (importer.forceToMono != forceMono)
            {
                importer.forceToMono = forceMono;
                changed = true;
            }

            if (changed)
            {
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                if (logChanges)
                {
                    Debug.Log($"[AudioLibraryBuilder] Optimized import settings for {fileName} ({targetLoadType}, Preload=true).");
                }
            }
        }

        private static AudioClip LoadClip(string fileName)
        {
            string path = $"{SfxFolder}/{fileName}";
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioLibraryBuilder] Missing expected audio clip at '{path}'.");
            }
            return clip;
        }
    }
}
