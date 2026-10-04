using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Editor.Extensions;
using Editor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.Common
{
    internal static class ModelExporter
    {
        private static string TempDirPath { get; } = Path.Combine(Path.GetTempPath(), "temp_bundles");
    
        private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();

        public static void ExportAsset(IExportableAsset asset, Action onComplete)
        {
            var tempDir = new DirectoryInfo(TempDirPath);
        
            try
            {
                if (!tempDir.Exists) tempDir.Create();
            
                if (!Settings.BeatSaberDirValid())
                {
                    EditorUtility.DisplayDialog("Exportation Failed!", "Export aborted - please set up Beat Saber dir in project settings.", "OK");
                    return;
                }

                Export(asset);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Exportation Failed!", "An unhandled exception occured during export. See console.", "OK");
            }
            finally
            {
                if (tempDir.Exists) tempDir.Delete(true);
                onComplete?.Invoke();
            }
        }
    
        private static void Export(IExportableAsset asset)
        {
            const string pcAssetFileName = "pc";
            const string metadataFileName = "metadata.json";
            const string imageFileName = "cover.png";
            
            if (!asset.GameObject)
            {
                EditorUtility.DisplayDialog("Exportation Failed!", "Asset GameObject is missing.", "OK");
                return;
            }

            var assetName = $"Assets/{asset.PrefabName}.prefab";
            var warnings = new List<string>();
        
            var prefab = PrefabUtility.SaveAsPrefabAsset(asset.GameObject, assetName);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Exportation Failed!", "Failed to create temporary prefab.", "OK");
                return;
            }
        
            var tempDir = new DirectoryInfo(TempDirPath);
        
            if (string.IsNullOrWhiteSpace(asset.Name))
                warnings.Add($"{nameof(asset.Name)} is empty.");
            if (string.IsNullOrWhiteSpace(asset.Author))
                warnings.Add($"{nameof(asset.Author)} is empty.");

            var assetModel = new AssetModel(imageFileName,
                asset.Name,
                asset.Author,
                new() { { AssetPlatform.PC, new(pcAssetFileName) } }
            );
            var jsonContent = JsonConvert.SerializeObject(assetModel, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented
            });
            
            // Create asset bundle
            BuildPipeline.BuildAssetBundles(
                tempDir.FullName,
                new[] { new AssetBundleBuild {
                    assetBundleName = pcAssetFileName,
                    assetNames = new[] { assetName }
                }},
                BuildAssetBundleOptions.None,
                EditorUserBuildSettings.activeBuildTarget
            );
            var assetBundlePath = Path.Combine(tempDir.FullName, pcAssetFileName); 
            var assetBundleFile = new FileInfo(assetBundlePath);
        
            // Create cover image file
            var imageFilePath = Path.Combine(tempDir.FullName, imageFileName);
            var imageFile = new FileInfo(imageFilePath);
            if (asset.AssetIcon) warnings.AddRange(WriteAssetIconFile(imageFile, asset.AssetIcon));
            else warnings.Add("No asset icon has been provided.");

            // Create metadata file
            var metaDataPath = Path.Combine(tempDir.FullName, metadataFileName);
            var metadataFile = new FileInfo(metaDataPath);
            using (var streamWriter = metadataFile.CreateText()) streamWriter.Write(jsonContent);
        
            // Create zip
            var outputFileName = Settings.GetExportFilename(assetModel.ModelName) + asset.FileFormat;
            var outputFile = new FileInfo(Path.Combine(tempDir.FullName, outputFileName));
            if (outputFile.Exists) outputFile.Delete();

            using (var archive = ZipFile.Open(outputFile.FullName, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(assetBundleFile.FullName, assetBundleFile.Name);
                archive.CreateEntryFromFile(metadataFile.FullName, metadataFile.Name);
                if (imageFile.Exists)
                    archive.CreateEntryFromFile(imageFile.FullName, imageFile.Name);
            }
        
            var bsDir = new DirectoryInfo(Path.Combine(Settings.beatSaberPath, asset.TargetBeatSaberDir));
            if (!bsDir.Exists) bsDir.Create();
            var bsFile = new FileInfo(Path.Combine(bsDir.FullName, outputFile.Name));
            outputFile.CopyTo(bsFile.FullName, true);

            Selection.activeObject = asset.GameObject;
            // EditorUtility.SetDirty();
            EditorSceneManager.MarkSceneDirty(asset.GameObject.scene);
            EditorSceneManager.SaveScene(asset.GameObject.scene);

            PrefabUtility.SaveAsPrefabAsset(asset.GameObject, assetName);
        
            EditorUtility.DisplayDialog("Exportation Successful!",
                warnings.Count == 0 ? "Exportation Successful!" : $"Warnings:\n - {string.Join("\n - ", warnings)}",
                "OK");
        }

        private static IEnumerable<string> WriteAssetIconFile(FileInfo imageFile, Texture2D tex)
        {
            if (!tex.isReadable)
            {
                yield return "Failed to write icon image: assigned texture is not readable. " +
                             "Go to texture's import settings -> advanced -> enable Read/Write.";
                yield break;
            }
        
            var renderTexture = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
        
            try
            {
                Graphics.Blit(tex, renderTexture);
                RenderTexture.active = renderTexture;
                var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new(0, 0, tex.width, tex.height), 0, 0);
                copy.Apply();

                var iconData = copy.EncodeToPNG();
                Object.DestroyImmediate(copy);

                if (iconData is not { Length: > 0 }) yield break;
                File.WriteAllBytes(imageFile.FullName, iconData);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}