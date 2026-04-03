#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using SmartCreator.ProceduralTrees;

namespace WoodSimulator
{
    /// <summary>
    /// Terrain試行錯誤用シーンを作成するエディタスクリプト。
    /// </summary>
    public static class TerrainTestSceneBuilder
    {
        [MenuItem("WoodSimulator/Create Terrain Test Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // カメラ設定
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(75f, 40f, -20f);
                mainCam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                mainCam.farClipPlane = 500f;
                mainCam.gameObject.AddComponent<SimpleFlyCamera>();
            }

            // Directional Light
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    light.intensity = 1.2f;
                }
            }

            // Terrain作成（150x150m、高さ40m）
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(150f, 40f, 150f);

            var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "Terrain";
            var terrainComp = terrainGO.GetComponent<Terrain>();

            // TerrainDataをアセットとして保存
            string terrainFolder = "Assets/Terrain";
            if (!AssetDatabase.IsValidFolder(terrainFolder))
                AssetDatabase.CreateFolder("Assets", "Terrain");
            AssetDatabase.CreateAsset(terrainData, terrainFolder + "/TerrainTestData.asset");

            // シーン保存
            string scenePath = "Assets/Scenes/TerrainTestScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TerrainTestSceneBuilder] Scene created at {scenePath}");
            Selection.activeGameObject = terrainGO;
        }

        [MenuItem("WoodSimulator/Add Forest To Current Scene")]
        public static void AddForestToCurrentScene()
        {
            // --- GrowthDatabase ---
            var growthDB = AssetDatabase.LoadAssetAtPath<GrowthDatabase>("Assets/Model/GrowthDatabase.asset");
            if (growthDB == null)
            {
                Debug.LogError("[TerrainTestSceneBuilder] GrowthDatabase.asset not found.");
                return;
            }

            // --- マテリアル ---
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineTrunk.mat");
            var leafMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineLeaf.mat");
            if (leafMat == null || barkMat == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CedarTreeTest/Prefabs/CedarTreeTreeGeneratorOptimized.prefab");
                if (prefab != null)
                {
                    var gen = prefab.GetComponent<PineTreeGenerator>();
                    if (gen != null)
                    {
                        if (barkMat == null) barkMat = gen.barkMaterial;
                        if (leafMat == null) leafMat = gen.leafMaterial;
                    }
                }
            }

            // --- Terrain取得 ---
            var terrain = Terrain.activeTerrain;
            Vector3 center;
            Vector2 forestSize;

            if (terrain != null)
            {
                var td = terrain.terrainData;
                center = terrain.transform.position + new Vector3(td.size.x / 2f, 0f, td.size.z / 2f);
                forestSize = new Vector2(td.size.x * 0.8f, td.size.z * 0.8f);
            }
            else
            {
                center = Vector3.zero;
                forestSize = new Vector2(100f, 100f);
            }

            // --- ForestManager ---
            var managerGO = new GameObject("ProceduralForestManager");
            var manager = managerGO.AddComponent<ProceduralForestManager>();
            manager.growthDatabase = growthDB;
            manager.barkMaterial = barkMat;
            manager.leafMaterial = leafMat;
            manager.terrain = terrain;
            manager.forestCenter = center;
            manager.forestSize = forestSize;
            manager.maxTreeCount = 1000;
            manager.frameBudgetMs = 16f;
            manager.displayScale = 0.1f;
            manager.minTerrainHeight = 5f;

            // --- Canvas + UI ---
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var ui = canvasGO.AddComponent<ProceduralForestUI>();
            ui.growthDatabase = growthDB;
            ui.forestManager = manager;

            // Info Panel（左上）
            var infoPanel = CreatePanel(canvasGO.transform, "Panel_Info",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(250f, 160f));

            ui.ageText = CreateText(infoPanel.transform, "Txt_Age", "林齢: 10年", new Vector2(10, -10), new Vector2(230, 30));
            ui.heightText = CreateText(infoPanel.transform, "Txt_Height", "樹高: 5.4m", new Vector2(10, -40), new Vector2(230, 30));
            ui.diameterText = CreateText(infoPanel.transform, "Txt_Diameter", "直径: 6.3cm", new Vector2(10, -70), new Vector2(230, 30));
            ui.treeCountText = CreateText(infoPanel.transform, "Txt_TreeCount", "本数: 3000本/ha", new Vector2(10, -100), new Vector2(230, 30));
            ui.progressText = CreateText(infoPanel.transform, "Txt_Progress", "", new Vector2(10, -130), new Vector2(230, 30));

            // Control Panel（左下）
            var ctrlPanel = CreatePanel(canvasGO.transform, "Panel_Controls",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(250f, 60f));

            ui.prevButton = CreateButton(ctrlPanel.transform, "Btn_Prev", "◀", new Vector2(10, 15), new Vector2(50, 30));
            ui.resetButton = CreateButton(ctrlPanel.transform, "Btn_Reset", "Reset", new Vector2(65, 15), new Vector2(55, 30));
            ui.nextButton = CreateButton(ctrlPanel.transform, "Btn_Next", "▶", new Vector2(125, 15), new Vector2(50, 30));
            ui.autoPlayButton = CreateButton(ctrlPanel.transform, "Btn_AutoPlay", "自動進行", new Vector2(180, 15), new Vector2(60, 30));
            ui.autoPlayButtonText = ui.autoPlayButton.GetComponentInChildren<Text>();

            // FPS Counter
            var fpsGO = new GameObject("FPS_Text");
            fpsGO.transform.SetParent(canvasGO.transform, false);
            var fpsRect = fpsGO.AddComponent<RectTransform>();
            fpsRect.anchorMin = new Vector2(1f, 1f);
            fpsRect.anchorMax = new Vector2(1f, 1f);
            fpsRect.pivot = new Vector2(1f, 1f);
            fpsRect.anchoredPosition = new Vector2(-10f, -10f);
            fpsRect.sizeDelta = new Vector2(200f, 30f);
            var fpsText = fpsGO.AddComponent<Text>();
            fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fpsText.fontSize = 18;
            fpsText.color = Color.yellow;
            fpsText.alignment = TextAnchor.UpperRight;
            var fpsCounter = canvasGO.AddComponent<FPSCounter>();
            fpsCounter.fpsText = fpsText;

            // --- カメラ操作UI（右下） ---
            var camPanel = CreatePanel(canvasGO.transform, "Panel_Camera",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 10f), new Vector2(170f, 170f));

            var camUI = canvasGO.AddComponent<CameraControlUI>();
            var mainCam = Camera.main;
            if (mainCam != null)
                camUI.flyCamera = mainCam.GetComponent<SimpleFlyCamera>();

            // 移動ボタン（十字配置）
            camUI.forwardButton = CreateButton(camPanel.transform, "Btn_Fwd", "▲", new Vector2(60, 120), new Vector2(40, 35));
            camUI.backButton    = CreateButton(camPanel.transform, "Btn_Back", "▼", new Vector2(60, 45), new Vector2(40, 35));
            camUI.leftButton    = CreateButton(camPanel.transform, "Btn_Left", "◀", new Vector2(15, 82), new Vector2(40, 35));
            camUI.rightButton   = CreateButton(camPanel.transform, "Btn_Right", "▶", new Vector2(105, 82), new Vector2(40, 35));
            camUI.upButton      = CreateButton(camPanel.transform, "Btn_Up", "↑", new Vector2(60, 82), new Vector2(40, 35));

            // 上下ボタン（左端）
            camUI.downButton    = CreateButton(camPanel.transform, "Btn_Down", "↓", new Vector2(15, 45), new Vector2(40, 35));

            // 回転ボタン（右端）
            camUI.rotLeftButton  = CreateButton(camPanel.transform, "Btn_RotL", "↶", new Vector2(15, 120), new Vector2(40, 35));
            camUI.rotRightButton = CreateButton(camPanel.transform, "Btn_RotR", "↷", new Vector2(105, 120), new Vector2(40, 35));
            camUI.rotUpButton    = CreateButton(camPanel.transform, "Btn_RotU", "⤒", new Vector2(105, 45), new Vector2(40, 35));
            camUI.rotDownButton  = CreateButton(camPanel.transform, "Btn_RotD", "⤓", new Vector2(15, 10), new Vector2(40, 35));

            // ラベル
            CreateText(camPanel.transform, "Txt_CamLabel", "Camera", new Vector2(55, -5), new Vector2(60, 20));

            // --- EventSystem ---
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // --- シーン保存 ---
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"[TerrainTestSceneBuilder] Forest added: {manager.maxTreeCount} trees on terrain");
            Selection.activeGameObject = managerGO;
        }

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent, false);
            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = panelGO.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f);
            return panelGO;
        }

        private static Text CreateText(Transform parent, string name, string defaultText,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.text = defaultText;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            button.colors = colors;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }
    }
}
#endif
