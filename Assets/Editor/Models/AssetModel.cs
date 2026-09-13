using System.Collections.Generic;

namespace Editor.Models
{
    public class AssetModel
    {
        public string IconPath;
        public string ModelName;
        public string AuthorName;
        public Dictionary<AssetPlatform, AssetMetadata> Assets;

        public AssetModel(
            string iconPath,
            string modelName,
            string authorName,
            Dictionary<AssetPlatform, AssetMetadata> assets)
        {
            IconPath = iconPath;
            ModelName = modelName;
            AuthorName = authorName;
            Assets = assets;
        }
    }
}