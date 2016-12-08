using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Sprites;
using System.Collections.Generic;

public class UGUIPackerPolicy : IPackerPolicy
{

    public static Dictionary<string, List<string>> atlasDict = new Dictionary<string, List<string>>();

    public class Entery
    {
        public Sprite sprite;
        public AtlasSettings setting;
        public string atlasName;
        public SpriteImportMode packingMode;
    }

    public virtual int GetVersion()
    {
        return 1;
    }

    public virtual void OnGroupAtlases(BuildTarget target, PackerJob job, int[] textureImporterInstanceIDs)
    {
        atlasDict.Clear();

        foreach (int instanceId in textureImporterInstanceIDs)
        {
            TextureImporter ti = EditorUtility.InstanceIDToObject(instanceId) as TextureImporter;
            if (ti == true)
            {
                ti.mipmapEnabled = false;
                AssetDatabase.ImportAsset(ti.assetPath);
            }

            if (!atlasDict.ContainsKey(ti.spritePackingTag))
            {
                atlasDict[ti.spritePackingTag] = new List<string>();
            }

            atlasDict[ti.spritePackingTag].Add(ti.assetPath);

        }

        foreach (string atlasName in atlasDict.Keys)
        {
            job.AddAtlas(atlasName, GetAtlasSetting(target));

            foreach (string path in atlasDict[atlasName])
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    continue;

                job.AssignToAtlas(atlasName, sprite, SpritePackingMode.Rectangle, SpritePackingRotation.None);

            }
        }
    }

    protected virtual AtlasSettings GetAtlasSetting(BuildTarget target)
    {
        AtlasSettings setting = new AtlasSettings();
        setting.filterMode = FilterMode.Bilinear;
        setting.colorSpace = ColorSpace.Linear;
        setting.generateMipMaps = false;
        setting.maxHeight = 2048;
        setting.maxWidth = 2048;

        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
            setting.format = TextureFormat.RGBA32;
        }
        else if (target == BuildTarget.Android)
        {
            setting.format = TextureFormat.ETC_RGB4;
        }
        else if (target == BuildTarget.iOS)
        {
            setting.format = TextureFormat.PVRTC_RGB4;
        }

        return setting;

    }

}
