#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

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
                mainCam.transform.position = new Vector3(250f, 80f, -50f);
                mainCam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                mainCam.farClipPlane = 1000f;
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

            // Terrain作成（500x500m）
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(500f, 100f, 500f);

            var terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            terrainGO.name = "Terrain";

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
    }
}
#endif
