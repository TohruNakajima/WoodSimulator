#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SmartCreator.ProceduralTrees;

namespace WoodSimulator
{
    /// <summary>
    /// ProceduralForestSceneをメニューから自動構築するEditorスクリプト。
    /// </summary>
    public static class ProceduralForestSceneBuilder
    {
        [MenuItem("WoodSimulator/Create Procedural Forest Scene")]
        public static void CreateScene()
        {
            // 新しいシーンを作成
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // --- GrowthDatabase参照を取得 ---
            var growthDB = AssetDatabase.LoadAssetAtPath<GrowthDatabase>("Assets/Model/GrowthDatabase.asset");
            if (growthDB == null)
            {
                Debug.LogError("GrowthDatabase.asset not found at Assets/Model/GrowthDatabase.asset");
                return;
            }

            // --- マテリアル参照を取得 ---
            var barkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineTrunk.mat");
            var leafMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SmartCreatorProceduralTrees/Materials/PineLeaf.mat");
            if (leafMat == null)
            {
                // leafMatが見つからない場合、プレハブから取得を試みる
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CedarTreeTest/Prefabs/CedarTreeTreeGeneratorOptimized.prefab");
                if (prefab != null)
                {
                    var gen = prefab.GetComponent<PineTreeGenerator>();
                    if (gen != null)
                    {
                        if (barkMat == null) barkMat = gen.barkMaterial;
                        leafMat = gen.leafMaterial;
                    }
                }
            }

            // --- カメラ設定 ---
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 15f, -30f);
                mainCam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
                mainCam.farClipPlane = 300f;
            }

            // --- Directional Light調整 ---
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    light.intensity = 1.2f;
                }
            }

            // --- 地面 ---
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(5f, 1f, 5f);

            // --- ForestManager ---
            var managerGO = new GameObject("ProceduralForestManager");
            var manager = managerGO.AddComponent<ProceduralForestManager>();
            manager.growthDatabase = growthDB;
            manager.barkMaterial = barkMat;
            manager.leafMaterial = leafMat;
            manager.forestCenter = Vector3.zero;
            manager.forestSize = new Vector2(32f, 32f);
            manager.maxTreeCount = 300;
            manager.frameBudgetMs = 8f;
            manager.displayScale = 0.1f;

            // --- Canvas + UI ---
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // UIコンポーネント
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

            // --- EventSystem ---
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // --- シーン保存 ---
            string scenePath = "Assets/Scenes/ProceduralForestScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[ProceduralForestSceneBuilder] Scene created at {scenePath}");
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

            // ラベルテキスト
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
