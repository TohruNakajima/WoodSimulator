#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace WoodSimulator
{
    /// <summary>
    /// LODプレハブを使った大量配置テストシーンを自動構築するエディタスクリプト。
    /// </summary>
    public static class LODForestSceneBuilder
    {
        private const int TreeCount = 1000;
        private const float ForestSizeX = 200f;
        private const float ForestSizeZ = 200f;
        private const string PrefabFolder = "Assets/BakedTrees";

        private static readonly string[] PrefabNames =
        {
            "CedarTree_Age010",
            "CedarTree_Age025",
            "CedarTree_Age040",
            "CedarTree_Age055",
            "CedarTree_Age075",
            "CedarTree_Age100"
        };

        [MenuItem("WoodSimulator/Create LOD Forest Test Scene")]
        public static void CreateScene()
        {
            // プレハブ読み込み
            var prefabs = new GameObject[PrefabNames.Length];
            for (int i = 0; i < PrefabNames.Length; i++)
            {
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{PrefabNames[i]}.prefab");
                if (prefabs[i] == null)
                {
                    Debug.LogError($"[LODForestSceneBuilder] Prefab not found: {PrefabNames[i]}. Run 'Bake LOD Tree Prefabs' first.");
                    return;
                }
            }

            // 新しいシーンを作成
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // --- カメラ設定 ---
            var mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0f, 20f, -40f);
                mainCam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
                mainCam.farClipPlane = 500f;
            }

            // --- Directional Light ---
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
            ground.transform.localScale = new Vector3(ForestSizeX / 10f, 1f, ForestSizeZ / 10f);

            // --- コンテナ ---
            var container = new GameObject("ForestContainer");

            // --- Poisson配置で木を配置 ---
            var points = PoissonDiskSampling.GeneratePointsWithCount(
                Vector3.zero, new Vector2(ForestSizeX, ForestSizeZ), TreeCount);

            int count = Mathf.Min(points.Count, TreeCount);
            Random.InitState(42);

            for (int i = 0; i < count; i++)
            {
                // 年齢パターンをランダム選択（老齢寄りの分布）
                float r = Random.Range(0f, 1f);
                int prefabIndex;
                if (r < 0.05f) prefabIndex = 0;       // 10年: 5%
                else if (r < 0.15f) prefabIndex = 1;   // 25年: 10%
                else if (r < 0.30f) prefabIndex = 2;   // 40年: 15%
                else if (r < 0.55f) prefabIndex = 3;   // 55年: 25%
                else if (r < 0.80f) prefabIndex = 4;   // 75年: 25%
                else prefabIndex = 5;                   // 100年: 20%

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[prefabIndex]);
                instance.transform.SetParent(container.transform, false);
                instance.transform.position = points[i];
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // 個体差のスケール（0.85〜1.15）
                float scale = Random.Range(0.85f, 1.15f);
                instance.transform.localScale = Vector3.one * scale;
            }

            // --- FPSカウンター用UI ---
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var fpsGO = new GameObject("FPS_Text");
            fpsGO.transform.SetParent(canvasGO.transform, false);
            var fpsRect = fpsGO.AddComponent<RectTransform>();
            fpsRect.anchorMin = new Vector2(1f, 1f);
            fpsRect.anchorMax = new Vector2(1f, 1f);
            fpsRect.pivot = new Vector2(1f, 1f);
            fpsRect.anchoredPosition = new Vector2(-10f, -10f);
            fpsRect.sizeDelta = new Vector2(200f, 30f);

            var fpsText = fpsGO.AddComponent<UnityEngine.UI.Text>();
            fpsText.text = "FPS: --";
            fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fpsText.fontSize = 18;
            fpsText.color = Color.yellow;
            fpsText.alignment = TextAnchor.UpperRight;

            // FPSカウンターコンポーネント
            var fpsCounter = canvasGO.AddComponent<FPSCounter>();
            fpsCounter.fpsText = fpsText;

            // --- 情報テキスト ---
            var infoGO = new GameObject("Info_Text");
            infoGO.transform.SetParent(canvasGO.transform, false);
            var infoRect = infoGO.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(0f, 1f);
            infoRect.pivot = new Vector2(0f, 1f);
            infoRect.anchoredPosition = new Vector2(10f, -10f);
            infoRect.sizeDelta = new Vector2(300f, 30f);

            var infoText = infoGO.AddComponent<UnityEngine.UI.Text>();
            infoText.text = $"LOD Forest Test: {count} trees | WASD+Mouse to move";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 16;
            infoText.color = Color.white;
            infoText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // --- EventSystem ---
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // --- シンプルカメラ移動コンポーネント ---
            if (mainCam != null)
                mainCam.gameObject.AddComponent<SimpleFlyCamera>();

            // --- シーン保存 ---
            string scenePath = "Assets/Scenes/LODForestTestScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[LODForestSceneBuilder] Scene created with {count} LOD trees at {scenePath}");
            Selection.activeGameObject = container;
        }
    }
}
#endif
