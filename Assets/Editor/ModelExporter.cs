using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Editor.Extensions;
using Editor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SaberComponents.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public class ModelExporter
{
    private static string TempDirPath { get; } = Path.Combine(Path.GetTempPath(), "temp_bundles");
    
    private static SaberProjectSettings Settings => SaberProjectSettings.GetOrCreateSettings();

    public static void ExportModel(SaberInfo saber)
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

            ExportSaber(saber);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog("Exportation Failed!", "An unhandled exception occured during export. See console.", "OK");
        }
        finally
        {
            if (tempDir.Exists) tempDir.Delete(true);
        }
    }
    
    private static void ExportSaber(SaberInfo saber)
    {
        const string pcAssetFileName = "pc";
        const string metadataFileName = "metadata.json";
        const string imageFileName = "cover.png";
        const string assetName = "Assets/_CustomSaber.prefab";

        if (!saber.GameObject)
        {
            EditorUtility.DisplayDialog("Exportation Failed!", "Saber GameObject is missing.", "OK");
            return;
        }

        var warnings = new List<string>();
        var descriptor = saber.SaberDescriptor;
        
        var prefab = PrefabUtility.SaveAsPrefabAsset(descriptor.gameObject, assetName);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Exportation Failed!", "Failed to create temporary prefab.", "OK");
            return;
        }
        
        var tempDir = new DirectoryInfo(TempDirPath);
        
        if (string.IsNullOrWhiteSpace(descriptor.SaberName))
            warnings.Add($"{nameof(descriptor.SaberName)} is empty.");
        if (string.IsNullOrWhiteSpace(descriptor.AuthorName))
            warnings.Add($"{nameof(descriptor.AuthorName)} is empty.");

        var saberData = new AssetModel(imageFileName,
            descriptor.SaberName,
            descriptor.AuthorName,
            new() { { AssetPlatform.PC, new(pcAssetFileName) } }
        );
        var jsonContent = JsonConvert.SerializeObject(saberData, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        });
        
        // Remove the descriptor from the prefab
        Object.DestroyImmediate(prefab.GetComponent<SaberDescriptor>(), true);
        
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
        warnings.AddRange(WriteCoverImageFile(imageFile, descriptor));
        
        // Create metadata file
        var metaDataPath = Path.Combine(tempDir.FullName, metadataFileName);
        var metadataFile = new FileInfo(metaDataPath);
        using (var streamWriter = metadataFile.CreateText()) streamWriter.Write(jsonContent);
        
        // Create zip
        var outputFileName = Settings.GetExportFilename(saberData.ModelName);
        var outputFile = new FileInfo(Path.Combine(tempDir.FullName, outputFileName));
        if (outputFile.Exists) outputFile.Delete();

        using (var archive = ZipFile.Open(outputFile.FullName, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(assetBundleFile.FullName, assetBundleFile.Name);
            archive.CreateEntryFromFile(metadataFile.FullName, metadataFile.Name);
            if (imageFile.Exists)
                archive.CreateEntryFromFile(imageFile.FullName, imageFile.Name);
        }
        
        // Move the zip to CustomSabers
        var customSabersDir = new DirectoryInfo(Path.Combine(Settings.beatSaberPath, "CustomSabers"));
        if (!customSabersDir.Exists) customSabersDir.Create();
        var customSabersFile = new FileInfo(Path.Combine(customSabersDir.FullName, outputFile.Name));
        outputFile.CopyTo(customSabersFile.FullName, true);

        Selection.activeObject = saber.GameObject;
        EditorUtility.SetDirty(saber.SaberDescriptor);
        EditorSceneManager.MarkSceneDirty(saber.GameObject.scene);
        EditorSceneManager.SaveScene(saber.GameObject.scene);

        PrefabUtility.SaveAsPrefabAsset(saber.GameObject, assetName);
        
        EditorUtility.DisplayDialog("Exportation Successful!",
            warnings.Count == 0 ? "Exportation Successful!" : $"Warnings:\n - {string.Join("\n - ", warnings)}",
            "OK");
    }

    private static IEnumerable<string> WriteCoverImageFile(FileInfo imageFile, SaberDescriptor descriptor)
    {
        var source = descriptor.CoverImage;
        if (!source) yield break;
        if (!source.isReadable)
        {
            yield return $"Assigned texture for {nameof(descriptor.CoverImage)} is not readable. " +
                         "Go to texture's import settings -> advanced -> enable Read/Write.";
            yield break;
        }
        
        var renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        
        try
        {
            Graphics.Blit(source, renderTexture);
            RenderTexture.active = renderTexture;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new(0, 0, source.width, source.height), 0, 0);
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