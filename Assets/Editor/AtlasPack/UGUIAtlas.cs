using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Sprites;
using System;
using System.IO;
using System.Text;

public class UGUIAtlas
{

    private static string _shader_path = "";

    private static string _atlasPrefab_path = "";

    private static string _modules_path = "";

    private static string _sourceImage_path = "";

    private static string _mat_path = "";

    private static string _selectedPolicy = "";

    private static string _commonFolder = "";

    private static string _matNameRule = "";

    private static string _atlasPrefabNameRule = "";

    private static string _grayNameRule = "";

    private static string _shaderTextureName = "";

    private static List<string> _filterList = new List<string>();


    /// <summary>
    /// 根据配置表初始化一些参数，如路径之类
    /// </summary>
    private static void InitConfig()
    {
        StreamReader sr = new StreamReader(Application.dataPath + "/Editor/AtlasPack/AtlasPackConfig.txt", Encoding.UTF8);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            if (line != "" && !line.StartsWith("--"))
            {
                string[] spaces = line.Split(',');
                switch (spaces[0])
                {
                    case "shader_path":
                        {
                            _shader_path = "Assets/" + spaces[1];
                            break;
                        }
                    case "atlasPrefab_path":
                        {
                            _atlasPrefab_path = "Assets/" + spaces[1];
                            break;
                        }
                    case "modules_path":
                        {
                            _modules_path = "Assets/" + spaces[1];
                            break;
                        }
                    case "sourceImage_path":
                        {
                            _sourceImage_path = "Assets/" + spaces[1];
                            break;
                        }
                    case "mat_path":
                        {
                            _mat_path = "Assets/" + spaces[1];
                            break;
                        }
                    case "selectedPolicy":
                        {
                            _selectedPolicy = spaces[1];
                            break;
                        }
                    case "commonFolder":
                        {
                            _commonFolder = spaces[1];
                            break;
                        }
                    case "matNameRule":
                        {
                            _matNameRule = spaces[1];
                            break;
                        }
                    case "atlasPrefabNameRule":
                        {
                            _atlasPrefabNameRule = spaces[1];
                            break;
                        }
                    case "grayNameRule":
                        {
                            _grayNameRule = spaces[1];
                            break;
                        }
                    case "shaderTextureName":
                        {
                            _shaderTextureName = spaces[1];
                            break;
                        }
                    case "filter":
                        {
                            _filterList.Clear();
                            _filterList.AddRange(spaces[1].Split('#'));
                            break;
                        }
                    default:
                        break;
                }
            }
        }
        sr.Close();
    }


    /// <summary>
    /// 获取当前平台
    /// </summary>
    /// <returns></returns>
    private static BuildTarget GetCurrentTarget()
    {
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            return BuildTarget.StandaloneWindows;
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            return BuildTarget.Android;
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            return BuildTarget.iOS;
        }

        return BuildTarget.StandaloneWindows;
    }

    private static void UpdateTextureImportor()
    {
        DirectoryInfo textureInfo = new DirectoryInfo(_sourceImage_path);
        if (textureInfo == null)
        {
            Debug.LogError(_sourceImage_path + "文件内容为NULL");
            return;
        }

        DirectoryInfo[] dirInfos = textureInfo.GetDirectories();
        UpdateDirectInfo(dirInfos);

    }

    /// <summary>
    /// 设置图集名称
    /// </summary>
    /// <param name="dirInfos"></param>
    public static void UpdateDirectInfo(DirectoryInfo[] dirInfos)
    {
        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            FileInfo[] files = dirInfo.GetFiles();

            foreach (FileInfo fileInfo in files)
            {
                string texFilePath = fileInfo.FullName;

                if (_filterList.Contains(Path.GetExtension(texFilePath)))
                    continue;

                int startIndex = texFilePath.IndexOf("Assets");
                string assetPath = texFilePath.Substring(startIndex, texFilePath.Length - startIndex);

                TextureImporter texImport = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (texImport.spritePackingTag != dirInfo.Name)
                {
                    texImport.spriteImportMode = SpriteImportMode.Single;
                    texImport.textureType = TextureImporterType.Sprite;
                    texImport.spritePackingTag = dirInfo.Name;
                    texImport.mipmapEnabled = false;
                    texImport.isReadable = false;
                    //散图不需要设置ETC,因为设置后没透明值，就提取不出透明值了
                    //texImport.SetPlatformTextureSettings("iPhone", 1024, TextureImporterFormat.PVRTC_RGB4);/////////////////////////////////
                    //texImport.SetPlatformTextureSettings("Android", 1024, TextureImporterFormat.ETC_RGB4, true);/////////////////////////////////
                    texImport.SaveAndReimport();
                }
            }

            DirectoryInfo[] dirSubInfos = dirInfo.GetDirectories();
            UpdateDirectInfo(dirSubInfos);

        }

    }

    [MenuItem("图集工具/1.删除图集预设、材质")]
    public static void DeleteAll()
    {
        InitConfig();

        //删除材质
        AssetDatabase.DeleteAsset(_mat_path);
        System.IO.Directory.CreateDirectory(_mat_path);

        //删除图集预设
        AssetDatabase.DeleteAsset(_atlasPrefab_path);
        System.IO.Directory.CreateDirectory(_atlasPrefab_path);

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

    }

    [MenuItem("图集工具/2.打包图集")]
    public static void PackAtlas()
    {
        //读取配置表信息
        InitConfig();

        //更新面板图集，设置图集名称
        UpdateTextureImportor();

        //图集生成
        Packer.SelectedPolicy = _selectedPolicy;
        Packer.RebuildAtlasCacheIfNeeded(GetCurrentTarget(), true, Packer.Execution.ForceRegroup);
    }

    [MenuItem("图集工具/3.创建图集预设、材质")]
    public static void CreateTextureGo()
    {
        //读取配置表信息
        InitConfig();

        if (UGUIPackerPolicy.atlasDict.Count == 0)
        {
            Debug.LogWarning("没有图集生成");
            return;
        }

        //获取texture
        string[] dirInfos = System.IO.Directory.GetDirectories(_atlasPrefab_path, "*", System.IO.SearchOption.AllDirectories);

        foreach (string atlasName in UGUIPackerPolicy.atlasDict.Keys)
        {
            Texture2D[] textures = Packer.GetTexturesForAtlas(atlasName);
            if (textures.Length != 1)
            {
                Debug.LogError(textures.Length + "图集生成失败!!!" + atlasName);
                break;
            }

            Texture2D texture = textures[0];

            List<string> pathList = UGUIPackerPolicy.atlasDict[atlasName];

            //创建灰度图
            CreateGrayTexture(texture, _mat_path, atlasName);

            //创建mat文件
            string grayPath = _mat_path + "/" + atlasName + "/" + atlasName + _grayNameRule;
            CreateGrayMat(_mat_path, atlasName, grayPath);

            //创建预设体
            CreateAssetGo(_atlasPrefab_path, atlasName, pathList);
        }

        Debug.Log("生成成功!!!");
    }

    //生成灰度图
    public static void CreateGrayTexture(Texture2D texture,string path,string name)
    {
        if (!Directory.Exists(path + "/" + name))
        {
            Directory.CreateDirectory(Application.dataPath.Replace("Assets", "") + path + "/" + name);
        }

        byte[] datas = texture.GetRawTextureData();

        //生成灰度图集
        Texture2D grayTexture = new Texture2D(texture.width, texture.height, texture.format, false);
        grayTexture.LoadRawTextureData(datas);

        Color[] grayColors = grayTexture.GetPixels();
        for (int i = 0; i < grayColors.Length; ++i)
        {
            grayColors[i] = new Color(grayColors[i].a, grayColors[i].a, grayColors[i].a);
        }
        grayTexture.SetPixels(grayColors);

        string grayPath = path + "/" + name + "/" + name + _grayNameRule;

        File.WriteAllBytes(grayPath, grayTexture.EncodeToPNG());

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        TextureImporter grayTi = AssetImporter.GetAtPath(grayPath) as TextureImporter;
        grayTi.mipmapEnabled = false;
        grayTi.spritePackingTag = "";
        grayTi.spriteImportMode = SpriteImportMode.Single;
        grayTi.textureType = TextureImporterType.Sprite;
        grayTi.mipmapEnabled = false;
        //grayTi.SetPlatformTextureSettings("iPhone", 1024, TextureImporterFormat.PVRTC_RGB4);///////////////////////////////////////////////
        grayTi.SetPlatformTextureSettings("Android", 1024, TextureImporterFormat.ETC_RGB4, true);/////////////////////////////////////////
        grayTi.SaveAndReimport();

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

    }

    //创建引用图集
    private static void CreateAssetGo(string path, string name, List<string> pathList)
    {
        if (!Directory.Exists(path + "/" + name))
        {
            AssetDatabase.CreateFolder(path, name);
        }

        string atlasPath = path + "/" + name + "/" + name + _atlasPrefabNameRule;
        atlasPath = atlasPath.Replace("//", "/");
        GameObject atlasGo = AssetDatabase.LoadAssetAtPath<GameObject>(atlasPath);
        if (atlasGo == null)
        {
            GameObject atlasTempGo = new GameObject();
            atlasTempGo.name = name + _atlasPrefabNameRule.Split('.')[0];

            atlasGo = PrefabUtility.CreatePrefab(atlasPath, atlasTempGo);
            GameObject.DestroyImmediate(atlasTempGo);

            Debug.Log("生成prefabAtlas文件：" + atlasPath);

        }

        ExtendAssetMono atlasMono = atlasGo.GetComponent<ExtendAssetMono>();
        if (atlasMono == null)
        {
            atlasMono = atlasGo.AddComponent<ExtendAssetMono>();
        }
        atlasMono.assetList.Clear();

        foreach (string assetPath in pathList)
        {
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (spr == null)
            {
                Debug.LogError("Texture2D文件错误,path:" + assetPath);
                continue;
            }
            atlasMono.assetList.Add(spr);
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
    }

    //生成材质
    private static void CreateGrayMat(string path,string name,string grayPath)
    {
        Texture2D texGray = AssetDatabase.LoadAssetAtPath<Texture2D>(grayPath);

        string matPath = grayPath.Substring(0, grayPath.LastIndexOf("/") + 1) + name + _matNameRule;

        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(_shader_path);
        if (shader == null)
        {
            Debug.LogError("shader没找到");
            return;
        }

        Material matTex = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (matTex == null)
        {
            matTex = new Material(shader);
            matTex.name = name + _matNameRule.Split('.')[0];

            matTex.SetTexture(_shaderTextureName, texGray);
            AssetDatabase.CreateAsset(matTex, matPath);
        }
        else
        {
            matTex.shader = shader;
            matTex.SetTexture(_shaderTextureName, texGray);
        }


    }

    /// <summary>
    /// 更新UI模块的图集材质
    /// </summary>
    /// 
    [MenuItem("图集工具/4.更新模块的材质")]
    public static void UpdatePrefab()
    {
        InitConfig();

        string[] files = System.IO.Directory.GetFiles(_modules_path, "*", SearchOption.AllDirectories);

        //获取公共图集和材质
        Material commonMat = AssetDatabase.LoadAssetAtPath<Material>(_mat_path + "/" + _commonFolder + "/" + _commonFolder + _matNameRule);
        GameObject commonAtlasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(_atlasPrefab_path + "/" + _commonFolder + "/" + _commonFolder + _atlasPrefabNameRule);

        ExtendAssetMono commomAssets = commonAtlasPrefab.GetComponent<ExtendAssetMono>();

        Dictionary<string, Sprite> name2SpriteDict = new Dictionary<string, Sprite>();
        foreach (UnityEngine.Object obj in commomAssets.assetList)
        {
            Sprite spt = obj as Sprite;
            name2SpriteDict.Add(spt.name, spt);
        }

        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];

            if (_filterList.Contains(Path.GetExtension(path)))
            {
                continue;
            }

            path = path.Replace(@"\", @"/");

            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                Debug.LogError(path);
                break;
            }

            FileInfo fileInfo = new FileInfo(path);
            string dirName = fileInfo.Directory.Name;

            //对应模块的材质
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(_mat_path + "/" + dirName + "/" + dirName + _matNameRule);

            Image[] imgs = go.GetComponentsInChildren<Image>(true);

            foreach (Image img in imgs)
            {
                if (img.sprite != null)
                {
                    if (name2SpriteDict.ContainsKey(img.sprite.name) || mat == null)
                    {
                        img.material = commonMat;
                    }
                    else if (mat != null)
                    {
                        img.material = mat;
                    }
                }
            }

        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

    }

    [MenuItem("图集工具/图集打包管线")]
    public static void PackPipeline()
    {
        DeleteAll();

        PackAtlas();

        CreateTextureGo();

        UpdatePrefab();

    }



}
