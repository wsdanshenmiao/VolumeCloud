using UnityEngine;
using UnityEditor;
using System.IO;
using System.Globalization;


public class NoiseTextureEditor : EditorWindow, IHasCustomMenu
{
    [MenuItem("Tool/NoiseTextureMaker #%T", false, priority: 1)]
    static void WindowOpen()
    {
        EditorWindow.GetWindowWithRect<NoiseTextureEditor>(new Rect(0, 0, 500, 500), false, "Noise Texture Maker", false);
    }

    internal enum NoiseResolution
    {
        _32x32, _64x64, _128x128, _256x256, _512x512, _1024x1024, _2048x2048
    }
    internal enum NoiseType
    {
        Perlin, Worley, WorleyOpposition, Value
    }

    int toolBarIndex = 0;

    bool previewOn = false;
    PreviewWindow previewWindow;
    Texture2D saveTexture;

    NoiseResolution resolution = NoiseResolution._256x256;
    NoiseType noiseType = NoiseType.Perlin;
    int cellSize = 16;
    int layer = 3;
    bool useFBMAdditive = false;
    string saveTextureName = "";

    private void OnGUI()
    {
        if (previewWindow == null)
            previewOn = false;

        EditorGUI.BeginChangeCheck();

        noiseType = (NoiseType)EditorGUILayout.EnumPopup("NoiseType", noiseType);
        toolBarIndex = (int)noiseType;

        EditorGUILayout.BeginVertical(GUILayout.Height(450));

        saveTextureName = EditorGUILayout.TextField(new GUIContent("TextureName", "Save Texture Name"), saveTextureName);
        if (saveTextureName == "")
            EditorGUILayout.HelpBox("If  'TextureName' is null, the final texture will be saved which named 'NewNoiseTexture.png' .", MessageType.Info);
        else
            EditorGUILayout.HelpBox("File name : " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(saveTextureName.ToLower()) + ".png", MessageType.Info);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        resolution = (NoiseResolution)EditorGUILayout.EnumPopup(new GUIContent("Resolution", "TextureResolution"), resolution);
        cellSize = EditorGUILayout.IntField(new GUIContent("CellSize", "Grid (power of 2)"), cellSize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        useFBMAdditive = EditorGUILayout.Toggle(new GUIContent("UseFBM", "Fractal Brownian Motion"), useFBMAdditive);
        if (useFBMAdditive)
        {
            layer = EditorGUILayout.IntField(new GUIContent("Layer", "Layer Add Num"), layer);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck() && previewOn && previewWindow != null)
        {
            int textureSize = 0;
            TextureSize(ref textureSize);
            previewWindow.Init(textureSize, useFBMAdditive, cellSize, layer, toolBarIndex);
        }

        if ((cellSize & cellSize - 1) != 0 || cellSize < 1)
        {
            EditorGUILayout.HelpBox("The 'CellSize' is not the power of 2 ! The final texture may be error !", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Save"))
        {
            SaveTexture2D();
        }
    }

    public void AddItemsToMenu(GenericMenu menu)
    {
        menu.AddItem(new GUIContent("Preview"), previewOn, () =>
        {
            previewOn = !previewOn;
            if (previewOn)
            {
                int textureSize = 0;
                TextureSize(ref textureSize);
                previewWindow = EditorWindow.GetWindowWithRect<PreviewWindow>(new Rect(0, 0, 500, 500), false, "Preview Window", false);
                previewWindow.Init(textureSize, useFBMAdditive, cellSize, layer, toolBarIndex);
                saveTexture = previewWindow.texture;
            }
            else
            {
                if (previewWindow != null)
                    previewWindow.Close();
            }
        });
    }


    #region SaveTexture
    void TextureSize(ref int textureSize)
    {
        switch (resolution)
        {
            case NoiseResolution._32x32:
                textureSize = 32;
                break;
            case NoiseResolution._64x64:
                textureSize = 64;
                break;
            case NoiseResolution._128x128:
                textureSize = 128;
                break;
            case NoiseResolution._256x256:
                textureSize = 256;
                break;
            case NoiseResolution._512x512:
                textureSize = 512;
                break;
            case NoiseResolution._1024x1024:
                textureSize = 1024;
                break;
            case NoiseResolution._2048x2048:
                textureSize = 2048;
                break;
        }
    }
    void SaveTexture2D()
    {
        string folderPath = EditorUtility.OpenFolderPanel("SaveTexture", Application.dataPath, "");
        if (folderPath != "")
        {
            if (saveTexture != null)
            {
                WriteTextureFile(saveTexture, folderPath);
            }
            else
            {
                int textureSize = 0;
                TextureSize(ref textureSize);
                saveTexture = new Texture2D(textureSize, textureSize);
                if (!useFBMAdditive)
                {
                    switch (toolBarIndex)
                    {
                        case 0:
                            BaseNoise.CalculatePerlinNoise(ref saveTexture, cellSize);
                            break;
                        case 1:
                            BaseNoise.CalculateWorleyNoise(ref saveTexture, cellSize, false);
                            break;
                        case 2:
                            BaseNoise.CalculateWorleyNoise(ref saveTexture, cellSize, true);
                            break;
                        case 3:
                            BaseNoise.CalculateValueNoise(ref saveTexture, cellSize);
                            break;
                    }
                }
                else
                    BaseNoise.CalculateFBMNoise(ref saveTexture, cellSize, layer, toolBarIndex);
                WriteTextureFile(saveTexture, folderPath);
            }
        }
        if (saveTexture != null)
            DestroyImmediate(saveTexture);
        AssetDatabase.Refresh();
    }
    void WriteTextureFile(Texture2D texture, string folderPath)
    {
        string fileParh = null;
        if (saveTextureName == "")
            fileParh = folderPath + "/NewNoiseTexture.png";
        else
            fileParh = folderPath + "/" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(saveTextureName.ToLower()) + ".png";

        if (!File.Exists(fileParh))
        {
            var bytes = texture.EncodeToPNG();
            var file = File.Create(fileParh);
            var binary = new BinaryWriter(file);
            binary.Write(bytes);
            file.Close();
        }
        else
        {
            if (EditorUtility.DisplayDialog("Waring", "The file has been created !", "Cover", "Cancel"))
            {
                var bytes = texture.EncodeToPNG();
                File.Delete(fileParh);
                var file = File.Create(fileParh);
                var binary = new BinaryWriter(file);
                binary.Write(bytes);
                file.Close();
            }
        }
    }
    #endregion

    private void OnDisable()
    {
        if (previewWindow != null)
            previewWindow.Close();
        if (saveTexture != null)
            DestroyImmediate(saveTexture);
    }

    #region  预览窗口
    public class PreviewWindow : EditorWindow
    {
        Editor previewTexture;
        public Texture2D texture;
        public void Init(int textureSize, bool useFBMAdditive, int cellSize, int layer, int toolBarIndex)
        {
            if (previewTexture != null)
                DestroyImmediate(previewTexture);
            if (texture != null)
                DestroyImmediate(texture);

            texture = new Texture2D(textureSize, textureSize);
            if (useFBMAdditive && layer > 0)
                BaseNoise.CalculateFBMNoise(ref texture, cellSize, layer, toolBarIndex);
            else
            {
                switch (toolBarIndex)
                {
                    case 0:
                        BaseNoise.CalculatePerlinNoise(ref texture, cellSize);
                        break;
                    case 1:
                        BaseNoise.CalculateWorleyNoise(ref texture, cellSize, false);
                        break;
                    case 2:
                        BaseNoise.CalculateWorleyNoise(ref texture, cellSize, true);
                        break;
                    case 3:
                        BaseNoise.CalculateValueNoise(ref texture, cellSize);
                        break;
                }
            }

            previewTexture = Editor.CreateEditor(texture);
        }
        private void OnGUI()
        {
            if (previewTexture != null && previewTexture.HasPreviewGUI())
            {
                EditorGUILayout.BeginVertical();
                using (new EditorGUILayout.HorizontalScope()) //水平布局
                {
                    GUILayout.FlexibleSpace();//填充间隔
                    previewTexture.OnPreviewSettings(); //显示预览设置项
                }
                EditorGUILayout.Space(50);
                previewTexture.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(400, 400), EditorStyles.whiteLabel);
                EditorGUILayout.EndVertical();
            }
        }
        private void OnDisable()
        {
            if (previewTexture != null)
                DestroyImmediate(previewTexture);
            if (texture != null)
                DestroyImmediate(texture);
        }
    }
    #endregion


    #region 噪声
    internal static class BaseNoise
    {
        #region CalculateMethod
        /// <summary>
        /// 计算柏林噪声
        /// </summary>
        /// <param name="texture">贴图</param>
        /// <param name="cellSize">晶格大小（最好2的幂次）</param>
        internal static void CalculatePerlinNoise(ref Texture2D texture, int cellSize)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float grayscale = perlinNoise(x / (float)cellSize, y / (float)cellSize);
                    texture.SetPixel(x, y, new Color(grayscale, grayscale, grayscale));
                }
            }
            texture.Apply();
        }

        /// <summary>
        /// 计算分形噪声
        /// </summary>
        /// <param name="texture">贴图</param>
        /// <param name="cellSize">晶格大小（最好2的幂次）</param>
        /// <param name="layer">叠加层数（越高越消耗性能）</param>
        internal static void CalculateFBMNoise(ref Texture2D texture, int cellSize, int layer, int toolBarIndex)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float grayscale = fbmNoise(x / (float)cellSize, y / (float)cellSize, layer, toolBarIndex);
                    texture.SetPixel(x, y, new Color(grayscale, grayscale, grayscale));
                }
            }
            texture.Apply();
        }

        /// <summary>
        /// 计算Worley噪声
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="cellSize"></param>
        internal static void CalculateWorleyNoise(ref Texture2D texture, int cellSize, bool opposition)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float grayscale = worleynoise(x / (float)cellSize, y / (float)cellSize, opposition);
                    texture.SetPixel(x, y, new Color(grayscale, grayscale, grayscale));
                }
            }
            texture.Apply();
        }

        internal static void CalculateValueNoise(ref Texture2D texture, int cellSize)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float grayscale = valuenoise(x / (float)cellSize, y / (float)cellSize);
                    texture.SetPixel(x, y, new Color(grayscale, grayscale, grayscale));
                }
            }

            texture.Apply();
        }
        #endregion

        #region BaseNoiseMethod

        /// <summary>
        /// 柏林噪声
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static float perlinNoise(float x, float y)
        {
            //声明二维坐标
            Vector2 pos = new Vector2(x, y);

            //声明该点所处的'格子'的四个顶点坐标
            Vector2 rightUp = new Vector2((int)x + 1, (int)y + 1);
            Vector2 rightDown = new Vector2((int)x + 1, (int)y);
            Vector2 leftUp = new Vector2((int)x, (int)y + 1);
            Vector2 leftDown = new Vector2((int)x, (int)y);

            //计算x上的插值
            float v1 = dotGridGradient(leftDown, pos);
            float v2 = dotGridGradient(rightDown, pos);
            float interpolation1 = perlinLerp(v1, v2, x - (int)x);

            //计算y上的插值
            float v3 = dotGridGradient(leftUp, pos);
            float v4 = dotGridGradient(rightUp, pos);
            float interpolation2 = perlinLerp(v3, v4, x - (int)x);

            float value = perlinLerp(interpolation1, interpolation2, y - (int)y);
            return value;
        }

        /// <summary>
        /// Worley噪声
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static float worleynoise(float x, float y, bool opposition)
        {
            float distance = 1;
            for (int Y = -1; Y <= 1; Y++)
            {
                for (int X = -1; X <= 1; X++)
                {
                    Vector2 cellPoint = randomFeaturePoint(new Vector2((int)x + X, (int)y + Y));
                    distance = Mathf.Min(distance, Vector2.Distance(cellPoint, new Vector2(x, y)));
                }
            }
            if (opposition)
                return 1 - distance;
            else
                return distance;
        }

        /// <summary>
        /// Value噪声
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        static float valuenoise(float x, float y)
        {
            //声明四个顶点
            Vector2 rightUp = new Vector2((int)x + 1, (int)y + 1);
            Vector2 rightDown = new Vector2((int)x + 1, (int)y);
            Vector2 leftUp = new Vector2((int)x, (int)y + 1);
            Vector2 leftDown = new Vector2((int)x, (int)y);

            //正方形插值
            float v1 = Mathf.SmoothStep(randomValuePoint(leftDown), randomValuePoint(rightDown), x - (int)x);
            float v2 = Mathf.SmoothStep(randomValuePoint(leftUp), randomValuePoint(rightUp), x - (int)x);

            return Mathf.SmoothStep(v1, v2, y - (int)y);
        }

        /// <summary>
        /// FBM噪声
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="layer">叠加层数</param>
        /// <returns></returns>
        static float fbmNoise(float x, float y, int layer, int toolBarIndex)
        {
            float value = 0;
            float frequency = 1f;
            float amplitude = 0.5f;
            for (int i = 0; i < layer; i++)
            {
                switch (toolBarIndex)
                {
                    case 0:
                        value += perlinNoise(x * frequency, y * frequency) * amplitude;
                        break;
                    case 1:
                        value += worleynoise(x * frequency, y * frequency, false) * amplitude;
                        break;
                    case 2:
                        value += worleynoise(x * frequency, y * frequency, true) * amplitude;
                        break;
                    case 3:
                        value += valuenoise(x * frequency, y * frequency) * amplitude;
                        break;
                }
                frequency *= 2;
                amplitude *= 0.5f;
            }
            return value;
        }

        #endregion

        #region BaseCalculate

        /// <summary>
        /// 插值计算
        /// </summary>
        /// <param name="a0"></param>
        /// <param name="a1"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        static float perlinLerp(float a0, float a1, float t)
        {
            t = ((6 * t - 15) * t + 10) * t * t * t;
            return Mathf.Lerp(a0, a1, t);
        }

        /// <summary>
        /// 权重值
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        static float dotGridGradient(Vector2 p1, Vector2 p2)
        {
            Vector2 gradient = randomGradient(p1);
            Vector2 offset = p2 - p1;
            return (Vector2.Dot(gradient, offset) + 1) / 2;
        }


        /*******************************************************************************************/

        /// <summary>
        /// Perlin随机梯度
        /// </summary>
        /// <param name="p">位置点</param>
        /// <returns></returns>
        static Vector2 randomGradient(Vector2 p)
        {
            float random = Mathf.Sin(666 + p.x * 5678 + p.y * 1234) * 4321;
            return new Vector2(Mathf.Sin(random), Mathf.Cos(random));
        }
        /// <summary>
        /// Worley随机特征点
        /// </summary>
        /// <param name="p">位置点</param>
        /// <returns></returns>
        static Vector2 randomFeaturePoint(Vector2 p)
        {
            float random = Mathf.Sin(666 + p.x * 5678 + p.y * 1234) * 4321;
            return new Vector2(p.x + Mathf.Sin(random) / 2 + 0.5f, p.y + Mathf.Cos(random) / 2 + 0.5f);
        }

        /// <summary>
        /// Value随机特征点
        /// </summary>
        /// <param name="p">位置点</param>
        /// <returns></returns>
        static float randomValuePoint(Vector2 p)
        {
            return (Mathf.Cos(Mathf.Sin(1234 + p.x * 123 + p.y * 353) * 12334) + 1) / 2;
        }
        #endregion
    }
    #endregion
}
