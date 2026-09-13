using System;
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

    public static void ExportModel(SaberInfo saber, bool silent = false)
    {
        var tempDir = new DirectoryInfo(TempDirPath);
        
        try
        {
            if (!tempDir.Exists) tempDir.Create();
            
            if (!Settings.BeatSaberDirValid())
            {
                // Beat Saber dir is not set up properly
                Debug.LogWarning("Export aborted - please set up Beat Saber dir in project settings.");
                return;
            }

            ExportSaber(saber, silent);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            if (tempDir.Exists) tempDir.Delete(true);
        }
    }
    
    private static void ExportSaber(SaberInfo saber, bool silent = false)
    {
        const string pcAssetFileName = "pc";
        const string assetName = "Assets/_CustomSaber.prefab";
        
        var saberObject = saber.GameObject;

        if (!saberObject)
        {
            if (!silent)
            {
                EditorUtility.DisplayDialog("Exportation Failed!", "Saber GameObject is missing.", "OK");
            }
            return;
        }

        var descriptor = saber.SaberDescriptor;
        
        var prefab = PrefabUtility.SaveAsPrefabAsset(descriptor.gameObject, assetName);
        if (prefab == null)
        {
            Debug.LogError("Failed to create temporary prefab.");
            return;
        }

        var saberData = new AssetModel(null, // todo - add cover icon
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
        
        var assetBundleBuild = new AssetBundleBuild
        {
            assetBundleName = pcAssetFileName,
            assetNames = new[] { assetName }
        };

        var tempDir = new DirectoryInfo(TempDirPath);
        
        BuildPipeline.BuildAssetBundles(
            tempDir.FullName,
            new[] { assetBundleBuild },
            BuildAssetBundleOptions.None,
            EditorUserBuildSettings.activeBuildTarget
        );
        
        var assetBundlePath = Path.Combine(tempDir.FullName, assetBundleBuild.assetBundleName); 
        var assetBundleFile = new FileInfo(assetBundlePath);
        
        var metadataFile = new FileInfo(Path.Combine(tempDir.FullName, "metadata.json"));
        using (var streamWriter = metadataFile.CreateText())
        {
            streamWriter.Write(jsonContent);
        }
        
        var outputFileName = Settings.GetExportFilename(saberData.ModelName);
        var outputFile = new FileInfo(Path.Combine(tempDir.FullName, outputFileName));
        if (outputFile.Exists) outputFile.Delete();

        using (var archive = ZipFile.Open(outputFile.FullName, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(assetBundleFile.FullName, assetBundleFile.Name);
            archive.CreateEntryFromFile(metadataFile.FullName, "metadata.json");
        }
        
        // Move the result archive to CustomSabers
        var customSabersDir = new DirectoryInfo(Path.Combine(Settings.beatSaberPath, "CustomSabers"));
        if (!customSabersDir.Exists) customSabersDir.Create();
        var customSabersFile = new FileInfo(Path.Combine(customSabersDir.FullName, outputFile.Name));
        outputFile.CopyTo(customSabersFile.FullName, true);

        Selection.activeObject = saberObject;
        EditorUtility.SetDirty(saber.SaberDescriptor);
        EditorSceneManager.MarkSceneDirty(saberObject.scene);
        EditorSceneManager.SaveScene(saberObject.scene);

        PrefabUtility.SaveAsPrefabAsset(saberObject, "Assets/_CustomSaber.prefab");

        if (!silent)
        {
            EditorUtility.DisplayDialog("Exportation Successful!", "Exportation Successful!", "OK");
        }
    }
}