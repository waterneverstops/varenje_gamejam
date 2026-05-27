using System.Collections.Generic;
using System.IO;
using System.Linq;
using NaughtyAttributes;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Plugins.RProjects.RUtils.Scripts.Editor
{
    [CreateAssetMenu]
    public class TexturePackerAsset : ScriptableObject
    {
        public bool overwriteExisting = true;

        public int maxSize = 512;

        [Range(2, 10)]
        public int padding = 2;
        public Object[] foldersAndTextures;

        [Button("Repack Atlas")]
        public void RepackAtlas()
        {
            if (foldersAndTextures.Length == 0)
            {
                Debug.LogWarning("you didn't fill the list with texture or folder references");
                return;
            }

            //Pack sprites
            var textures = GatherTextures();
            FixImportSettings(textures);
            var texture = new Texture2D(maxSize, maxSize, TextureFormat.RGBA32, false);
            var rects = texture.PackTextures(textures, padding, maxSize);
            var spriteRects = new SpriteRect[rects.Length];

            // store metadata
            var metas = new SpriteMetaData[textures.Length];
            for (var i = 0; i < rects.Length; i++)
            {
                spriteRects[i] = new SpriteRect()
                {
                    name = textures[i].name,
                    spriteID = GUID.Generate(),
                    rect = ConvertToPixelRect(texture.width, texture.height, rects[i]),
                    pivot = Vector2.zero,
                    alignment = SpriteAlignment.BottomLeft
                };
            }

            //Save image
            var path = AssetDatabase.GetAssetPath(this);
            var pngPath = Path.ChangeExtension(path, "png");
            if (!overwriteExisting)
            {
                pngPath = AssetDatabase.GenerateUniqueAssetPath(pngPath);
            }
            Debug.Log($"Create sprite from atlas: {name} path: {path}");

            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(pngPath, bytes);

            //Update sprite settings
            AssetDatabase.Refresh();

            var assetImporter = AssetImporter.GetAtPath(pngPath);
            ((TextureImporter)assetImporter).spriteImportMode = SpriteImportMode.Multiple;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(assetImporter);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(spriteRects);

            // Note: This section is only for Unity 2021.2 and newer
            // Register the new Sprite Rect's name and GUID with the ISpriteNameFileIdDataProvider
            var spriteNameFileIdDataProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            var nameFileIdPairs = spriteNameFileIdDataProvider.GetNameFileIdPairs().ToList();
            nameFileIdPairs.AddRange(
                spriteRects.Select(spriteRect => new SpriteNameFileIdPair(
                    spriteRect.name,
                    spriteRect.spriteID
                ))
            );
            spriteNameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
            // End of Unity 2021.2 and newer section


            // Apply the changes made to the data provider
            dataProvider.Apply();

            // Reimport the asset to have the changes applied
            assetImporter.SaveAndReimport();
        }

        private List<int> maxSizeOptions = new List<int> { 1024, 2048, 4096, 8192, 16384, 32768 };

        private Texture2D[] GatherTextures()
        {
            var textures = foldersAndTextures.OfType<Texture2D>();
            var folderPaths = foldersAndTextures
                .Select(AssetDatabase.GetAssetPath)
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();
            var searchQuery = "t:Texture2D";
            return AssetDatabase
                .FindAssets(searchQuery, folderPaths)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .Concat(textures)
                .ToArray();
        }

        private static void FixImportSettings(Texture2D[] textures)
        {
            foreach (var texture in textures)
            {
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null)
                    continue;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }

        private static Rect ConvertToPixelRect(int width, int height, Rect uvRect)
        {
            var pixelLeft = uvRect.x * width;
            var pixelTop = uvRect.y * height;
            var pixelWidth = uvRect.width * width;
            var pixelHeight = uvRect.height * height;
            return new Rect(pixelLeft, pixelTop, pixelWidth, pixelHeight);
        }
    }
}
 
 
