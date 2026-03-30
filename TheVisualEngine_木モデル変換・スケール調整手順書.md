# The Visual Engine 木モデル変換・スケール調整手順書

## 概要

既存の木の3DモデルをThe Visual Engine (TVE)用に変換し、木の高さや幹の太さをリアルタイムで変更可能にするための手順書。

**変換対象**: Assets/Model/BC_PM_P02_japanese_cedar.fbx（杉モデル）
**ツール**: Tools/BOXOPHOBIC/The Visual Engine/Asset Converter
**目的**: TVE対応シェーダー適用、頂点データ変換、動的スケール制御の実現

---

## 前提条件

### 必須コンポーネント
- The Visual Engine アセット導入済み (Assets/BOXOPHOBIC/)
- TVE Manager シーン配置済み (BOXOPHOBIC/The Visual Engine/TVE Manager)
- 変換対象Prefab作成済み (Assets/Prefabs/JapaneseCedar.prefab など)

### 確認事項
```bash
# TVEアセット確認
ls Assets/BOXOPHOBIC/The Visual Engine/

# TVE Managerシーン配置確認
# シーン内にTVE Managerが1つだけ存在すること
```

---

## Part 0: Unity Tree Creatorで木を生成する手順

### 0-1. Tree Creator概要

**Unity Tree Creator**は、Unityエディタ内で直接木を設計できるツールです。

**重要な制限事項:**
- **Built-In Render Pipelineのみ対応**
- URP/HDRPでは使用不可（SpeedTreeのインポート推奨）
- 本プロジェクト（WoodSimulator）はBuilt-Inパイプラインを使用想定

**Tree Creatorの利点:**
- 外部3Dソフト不要
- リアルタイムプレビュー
- 幹・枝・葉のパラメトリック調整
- LOD（Level of Detail）自動生成
- TVE Asset Converterと完全互換

### 0-2. 新規Treeアセット作成

**手順:**
1. Unity Editorメニュー: `GameObject` → `3D Object` → `Tree`
2. シーン内に新規TreeオブジェクトとPrefabが自動作成
3. Projectウィンドウに `New Tree.prefab` が生成される

**または、Project内で直接作成:**
1. Projectウィンドウで右クリック
2. `Create` → `Tree`
3. `New Tree.prefab` が作成される

**作成後の確認:**
- Hierarchyに "New Tree" オブジェクト表示
- Inspectorに `Tree (Script)` コンポーネント表示
- デフォルトで簡単な木が表示される

### 0-3. Tree Editorウィンドウを開く

**手順:**
1. Hierarchyで作成したTreeオブジェクトを選択
2. Inspector下部の `Edit Tree` ボタンをクリック
3. Tree Editorウィンドウが開く

**Tree Editorレイアウト:**
```
┌─────────────────────────────────────┐
│ Tree Editor                         │
├─────────────────────────────────────┤
│ Hierarchy (左側)                    │
│  ├─ Tree Root (根)                 │
│  ├─ Branch Group (枝グループ)       │
│  └─ Leaf Group (葉グループ)         │
├─────────────────────────────────────┤
│ Properties (右側)                   │
│  - Distribution (分布)              │
│  - Geometry (形状)                  │
│  - Material (マテリアル)            │
│  - Shape (シェイプ)                 │
└─────────────────────────────────────┘
```

### 0-4. 幹（Trunk）の設定

**基本パラメータ:**

1. **Tree Root** をHierarchyで選択
2. Propertiesで以下を調整:

```
Distribution (分布設定):
- Group Seed: ランダムシード値（0-9999999）
- Frequency: 幹の基部からの分岐頻度（1-5推奨）
- Distribution: 幹に沿った分布カーブ
- Twirl: 幹のねじれ具合（0-1）
- Whorled Step: 輪生ステップ（枝の環状配置）

Geometry (形状設定):
- Length: 幹の長さ（1-10m）
- Relative Length: 相対長さ（0.5-1.0）
- Radius: 幹の半径（0.1-1.0m）
- Radius Curve: 根元から先端への太さ変化カーブ
- Cap Smoothing: 先端の滑らかさ（0-1）

Shape (シェイプ設定):
- Crinkliness: しわの量（0-1）
- Seek Sun: 太陽方向への成長（0-1）
- Noise: ノイズ強度（0-1）
- Noise Scale: ノイズスケール（0.1-2.0）

Material (マテリアル設定):
- Bark Material: 幹のマテリアル
- Break Material: 折れた部分のマテリアル
```

**杉の木の推奨値（参考）:**
```
Length: 5.0-8.0
Radius: 0.3-0.5
Radius Curve: 下部1.0 → 上部0.3（細くなる）
Crinkliness: 0.2
Noise: 0.1
```

### 0-5. 枝（Branch）の追加

**手順:**
1. Tree Editorウィンドウ下部の `Add Branch Group` ボタンをクリック
2. Hierarchyに "Branch Group 0" が追加される
3. Propertiesで枝のパラメータを調整

**枝の主要パラメータ:**

```
Distribution:
- Group Seed: 枝の配置ランダムシード
- Frequency: 枝の数（5-20）
- Distribution: 幹に沿った枝の配置（カーブ調整）
- Growth Scale: 成長スケール（0.5-1.0）
- Growth Angle: 成長角度（30-90度）

Geometry:
- Length: 枝の長さ（0.5-3.0m）
- Relative Length: 幹に対する相対長さ（0.5-0.8）
- Radius: 枝の太さ（0.05-0.2m）

Shape:
- Cap Smoothing: 先端の滑らかさ
- Crinkliness: しわの量
- Seek Sun: 太陽方向への曲がり具合（0.2-0.5）
```

**杉の木の推奨値:**
```
Frequency: 8-12
Growth Angle: 45度
Length: 1.5-2.5
Radius: 0.1-0.15
Seek Sun: 0.3
```

**複数段階の枝追加:**
1. 既存の "Branch Group 0" を選択
2. `Add Branch Group` をクリック → "Branch Group 1" が子として追加
3. より細かい枝を作成（Length/Radius減少）

### 0-6. 葉（Leaf）の追加

**手順:**
1. 葉を配置したいBranch Groupを選択
2. Tree Editorウィンドウ下部の `Add Leaf Group` ボタンをクリック
3. Hierarchy に "Leaf Group 0" が追加される

**葉の主要パラメータ:**

```
Distribution:
- Group Seed: 葉の配置ランダムシード
- Frequency: 葉の数（10-100）
- Distribution: 枝に沿った葉の配置
- Twirl: 葉の回転（0-1）

Geometry:
- Size: 葉のサイズ（0.1-1.0）
- Perpendicular Align: 垂直方向の整列（0-1）
- Horizontal Align: 水平方向の整列（0-1）

Shape:
- Geometry Mode:
  - Plane: 平面（パフォーマンス重視）
  - Cross: 十字交差（よりリアル）
  - Tri Cross: 三角交差（最もリアル）
  - Billboard: ビルボード（常にカメラ向き）

Material:
- Leaf Material: 葉のマテリアル
- Shadow Offset: 影のオフセット（0-1）
```

**杉の木の推奨値（針葉樹用）:**
```
Frequency: 50-80
Size: 0.2-0.4
Geometry Mode: Cross または Tri Cross
Perpendicular Align: 0.8
Horizontal Align: 0.2
```

**葉のテクスチャ設定:**
1. 葉用テクスチャを準備（PNG、Alpha透過あり）
2. Materialを作成: `Assets/Materials/CedarLeaf.mat`
3. Shader: `Nature/Tree Creator Leaves`
4. Main Color: 緑系（RGB: 0.3, 0.6, 0.3）
5. Base (RGB) Trans (A): 葉テクスチャを割り当て
6. Tree EditorのLeaf Materialに設定

### 0-7. 幹のマテリアル設定

**幹用マテリアル作成:**
1. `Assets/Materials/CedarBark.mat` を作成
2. Shader: `Nature/Tree Creator Bark`
3. Base (RGB): 幹のテクスチャ（茶色系）
4. Normalmap: ノーマルマップ（凹凸）
5. Tree EditorのBark Materialに設定

**テクスチャがない場合:**
- Unity標準アセット使用: `Standard Assets/Environment/TerrainAssets/BillboardTextures/`
- またはProceduralマテリアル使用（単色+ノイズ）

### 0-8. 風エフェクト設定

**Wind Zone設定:**
```
Distribution → Wind:
- Main Wind: 主要な風の強さ（0-1）
- Main Turbulence: 乱流（0-1）
- Edge Turbulence: 枝先の揺れ（0-1）
```

**推奨値:**
```
Main Wind: 0.3
Main Turbulence: 0.2
Edge Turbulence: 0.5
```

**Scene内にWind Zoneを配置:**
1. `GameObject` → `3D Object` → `Wind Zone`
2. Inspector:
   - Mode: `Directional`
   - Main: 0.5
   - Turbulence: 0.3

### 0-9. LOD（Level of Detail）設定

**LODグループ設定:**
1. Tree Editorウィンドウ上部の `LOD` タブをクリック
2. LOD数を設定（デフォルト: 3段階）
3. 各LODの品質を調整:

```
LOD 0 (最高品質、カメラ近距離):
- Mesh Quality: 100%
- Branch Count: 100%
- Leaf Count: 100%

LOD 1 (中品質、中距離):
- Mesh Quality: 70%
- Branch Count: 70%
- Leaf Count: 80%

LOD 2 (低品質、遠距離):
- Mesh Quality: 40%
- Branch Count: 40%
- Leaf Count: 50%

Billboard (最低品質、最遠距離):
- 2Dビルボード画像に切り替え
```

**Billboard設定:**
- Billboard Atlasが自動生成される
- 4方向または8方向の画像を生成

### 0-10. Treeアセットの保存とPrefab化

**保存手順:**
1. Tree Editorウィンドウで編集完了
2. 下部の `Apply` ボタンをクリック（変更を確定）
3. Projectウィンドウで `New Tree.prefab` を `CedarTree.prefab` にリネーム
4. 保存先を整理: `Assets/Prefabs/Trees/CedarTree.prefab` に移動

**生成されるアセット:**
```
Assets/Prefabs/Trees/
├─ CedarTree.prefab (メインPrefab)
├─ CedarTree_Textures/ (自動生成テクスチャ)
│  ├─ diffuse.png (拡散色)
│  ├─ normal_specular.png (法線+スペキュラ)
│  ├─ shadow.png (影)
│  └─ translucency_gloss.png (透過+光沢)
└─ CedarTree_Billboard/ (Billboard用)
   └─ atlas.png
```

### 0-11. Tree Creatorで作成した木の確認

**シーン配置テスト:**
1. HierarchyからTree Creatorツリーを削除（編集用オブジェクト）
2. ProjectからPrefabをシーンにドラッグ&ドロップ
3. Play Mode実行

**確認項目:**
- 幹・枝・葉が正しく表示される
- LODが距離に応じて切り替わる
- Wind Zoneで風エフェクトが動作する

**次のステップ:**
このTree Creator製Prefabを **Part 1: Asset Converter** でTVE用に変換します。

---

## Part 1: Asset Converterの起動と基本設定

### 1-1. Asset Converterウィンドウを開く

**Unity Editor操作:**
1. メニュー: `Window` → `BOXOPHOBIC` → `The Visual Engine` → `Asset Converter`
2. ウィンドウが開き、"Asset Converter" タイトルが表示される

**確認項目:**
- TVE Managerがシーンに存在しない場合、エラーメッセージ表示
- 初回起動時、最小サイズ 400x280px で表示

### 1-2. 変換対象Prefabの選択

**手順:**
1. Projectウィンドウで変換対象Prefabを選択
   - 例: `Assets/Prefabs/JapaneseCedar.prefab`
   - 複数選択可能（Ctrl+クリック）
2. Asset Converterウィンドウに選択Prefabが自動表示される

**表示内容:**
- `Supported Prefabs`: サポート対象の数
- `Converted Prefabs`: すでに変換済みの数
- `Unsupported Prefabs`: 非対応の数（0であること）

**注意:**
- FBXファイル自体は選択不可（Prefab化必須）
- SpeedTree、Tree Creator形式もサポート対象

---

## Part 2: 変換プリセットの選択

### 2-1. プリセット選択（木モデル用）

**推奨プリセット:**
- **Unity Tree Creator**: Unity標準木
- **SpeedTree**: SpeedTree形式
- **Universal / Default**: 汎用FBXモデル（今回の杉モデル）

**設定方法:**
1. Asset Converterウィンドウ上部の `Preset` ドロップダウン
2. `Universal / Default` を選択

**プリセット効果:**
- シェーダー自動割り当て (TVE/Vegetation シリーズ)
- 頂点カラー・UV座標のマッピング設定
- モーション・風エフェクト設定

### 2-2. オプション選択（自動検出推奨）

**Option設定:**
- `Auto Detect`: 自動検出（推奨）
- Prefab構造から最適オプションを判定

**カスタム設定（必要に応じて）:**
- `Leaves`: 葉メッシュのみ
- `Bark / Trunk`: 幹メッシュのみ
- `Leaves + Bark`: 両方含む（杉モデルはこれ）

---

## Part 3: 変換詳細設定（オプション）

### 3-1. Conversion Settings展開

**設定項目（デフォルトで問題なし）:**

#### Materials設定
- `Output Materials`: マテリアル変換有効化（チェックON）
- `Share Common Materials`: 共通マテリアル共有（チェックON推奨）

#### Meshes設定
- `Output Meshes`: メッシュ変換有効化（チェックON）
- `Transform Meshes to World Space`: ワールド座標変換（チェックOFF推奨）

#### 頂点データマッピング（重要）
- `Vertex R/G/B/A`: 頂点カラー用途設定
  - **Vertex R**: Motion Variation（揺れのバリエーション）
  - **Vertex G**: Motion Intensity（揺れの強度）
  - **Vertex B**: Phase Variation（位相バリエーション）
  - **Vertex A**: Mask（高さマスク、スケール制御用）

**高さ制御用の設定:**
```
Vertex A (高さマスク):
- Source: Procedural
- Option: Height
- Action: Multiply By Height

→ これにより、メッシュの高さ情報が頂点Aチャンネルに格納され、
  後からシェーダーでスケール変更が可能になる
```

### 3-2. 詳細設定の保存

**Override設定:**
1. `Add Override` ボタン (+) をクリック
2. カスタム設定を追加（必要時のみ）
3. グローバル設定として保存: `Save Overrides` ボタン

**保存場所:**
```
Assets/Editor/User/The Visual Engine/Converter Overrides.asset
```

---

## Part 4: 変換実行

### 4-1. 変換前チェック

**確認項目:**
1. Prefab選択数: 1個以上
2. プリセット: `Universal / Default` 選択済み
3. Output Materials / Meshes: 両方ON
4. Unsupported Prefabs: 0

### 4-2. 変換実行

**手順:**
1. `Convert` ボタンをクリック
2. 進行状況ウィンドウ表示:
   - Prepare Prefab (0%)
   - Create Backup (10%)
   - Prepare Assets (20%)
   - Convert Materials (40%)
   - Transform Meshes (50%)
   - Convert Meshes (60%)
   - Convert Colliders (80%)
   - Save Prefab (90%)
   - Finish Conversion (100%)

**処理内容:**
1. バックアップPrefab作成 (GUID保存)
2. マテリアル変換:
   - 既存マテリアル → TVEシェーダー (TVE/Vegetation/Leaves、TVE/Vegetation/Bark)
   - テクスチャ再割り当て (Albedo, Normal, Parallax)
3. メッシュ変換:
   - 頂点カラー追加/変更 (R: Variation, G: Intensity, B: Phase, A: Height Mask)
   - UV座標最適化
   - 法線再計算（オプション）
4. コライダー変換
5. 変換済みPrefab保存

**保存先:**
```
変換前: Assets/Prefabs/JapaneseCedar.prefab
変換後: Assets/Prefabs/JapaneseCedar.prefab（上書き）
バックアップ: Assets/Prefabs/JapaneseCedar [Original].prefab（自動作成）
```

### 4-3. 変換完了確認

**確認項目:**
1. Prefabに `TVEPrefab` コンポーネント追加
2. マテリアルシェーダー変更:
   - `TVE/Vegetation/Leaves/Standard`
   - `TVE/Vegetation/Bark/Standard`
3. メッシュファイル作成:
   - `Assets/Prefabs/Meshes/JapaneseCedar_01.mesh`
   - `Assets/Prefabs/Meshes/JapaneseCedar_02.mesh`（サブメッシュ数に応じて）

**Inspector確認:**
```
JapaneseCedar Prefab
├─ TVEPrefab (Component)
│  ├─ Stored Preset: Universal / Default; Auto
│  ├─ Stored Prefab Backup GUID: (自動記録)
│  └─ Lock In Asset Converter: (チェック可能)
├─ Age10_SmallTree
│  ├─ MeshFilter → Mesh: JapaneseCedar_01.mesh
│  └─ MeshRenderer → Material: JapaneseCedar_Leaves (TVE Shader)
├─ Age25_YoungTree
...
```

---

## Part 5: スケール調整（高さ・太さ変更）

### 5-1. TVEシェーダーによる高さ制御

**方法1: シェーダープロパティ直接変更**

1. マテリアルを開く: `JapaneseCedar_Leaves.mat`
2. Inspectorで以下を調整:
   ```
   Main (Albedo, Normal, Mask):
   - Size Fade: 高さフェード範囲（0-1）

   Motion (Motion, Perspective, Squash):
   - Motion Intensity: 揺れの強さ（0-1）
   - Motion Amplitude: 揺れの振幅

   Custom:
   - Vertex A Multiplier: 高さマスク乗算（0.5-2.0）
     → 0.5: 半分の高さ
     → 2.0: 2倍の高さ
   ```

**方法2: スクリプトから動的変更**

```csharp
// TreeScaler.cs に追加（既存実装を拡張）
public class TreeScalerWithTVE : MonoBehaviour
{
    public float targetHeight = 10.0f;
    public float targetDiameter = 0.3f;
    private Renderer treeRenderer;

    void Start()
    {
        treeRenderer = GetComponent<Renderer>();
    }

    public void ApplyScale(float height, float diameter)
    {
        // 従来のlocalScale変更
        Vector3 original = transform.localScale;
        float heightRatio = height / targetHeight;
        float diameterRatio = diameter / targetDiameter;

        transform.localScale = new Vector3(
            original.x * diameterRatio,
            original.y * heightRatio,
            original.z * diameterRatio
        );

        // TVEシェーダープロパティ変更（追加）
        if (treeRenderer != null && treeRenderer.sharedMaterial != null)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            treeRenderer.GetPropertyBlock(props);

            // 高さマスク乗算
            props.SetFloat("_VertexAMultiplier", heightRatio);

            // テクスチャTiling調整（既存実装）
            props.SetVector("_MainTex_ST", new Vector4(1, heightRatio, 0, 0));

            treeRenderer.SetPropertyBlock(props);
        }
    }
}
```

**使用例:**
```csharp
// ForestGenerator.cs での適用
void UpdateTree(GameObject tree, float age)
{
    var growthData = growthDatabase.GetDataByAge(age);
    var scaler = tree.GetComponent<TreeScalerWithTVE>();

    if (scaler != null)
    {
        scaler.ApplyScale(growthData.height, growthData.diameter);
    }
}
```

### 5-2. 幹の太さ変更（Diameter制御）

**方法1: X/Zスケール変更**

```csharp
// 直径に応じたスケール調整
float diameterRatio = targetDiameter / baseDiameter;
transform.localScale = new Vector3(
    originalScale.x * diameterRatio,
    originalScale.y, // 高さは別途調整
    originalScale.z * diameterRatio
);
```

**方法2: Bark ShaderのDetail Mask使用**

1. マテリアル: `JapaneseCedar_Trunk.mat`
2. Inspector設定:
   ```
   Detail Mask:
   - Detail Mode: Overlay
   - Detail Intensity: 太さ強調度（0-1）

   Gradient Mask:
   - Gradient Offset: 幹の膨らみ位置調整（-1 to 1）
   - Gradient Contrast: 幹の太さコントラスト（0-5）
   ```

**スクリプト制御:**
```csharp
public void ApplyTrunkDiameter(float diameter)
{
    var trunkRenderer = trunkObject.GetComponent<Renderer>();
    if (trunkRenderer != null)
    {
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        trunkRenderer.GetPropertyBlock(props);

        // 太さに応じたGradient調整
        float contrast = Mathf.Lerp(1.0f, 5.0f, diameter / maxDiameter);
        props.SetFloat("_GradientContrast", contrast);

        trunkRenderer.SetPropertyBlock(props);
    }
}
```

### 5-3. 季節変化との連動

**TVE Managerとの統合:**

```csharp
public class SeasonalTreeGrowth : MonoBehaviour
{
    public TVEGlobalControl tveManager;
    public TreeScalerWithTVE treeScaler;

    void Update()
    {
        // TVE Managerの季節データ取得
        float seasonValue = tveManager.seasonControl; // 0-4

        // 成長データと季節を連動
        float adjustedHeight = baseHeight * Mathf.Lerp(0.8f, 1.2f, seasonValue / 4.0f);
        treeScaler.ApplyScale(adjustedHeight, baseDiameter);
    }
}
```

---

## Part 6: 変換結果の検証

### 6-1. シーン配置テスト

1. 変換済みPrefabをシーンに配置
2. Play Mode実行
3. 確認項目:
   - 風エフェクト動作（Motion Control > 0）
   - 季節変化対応（Season Control 0-4）
   - インタラクション反応（Flow Element配置時）

### 6-2. パフォーマンス確認

**Stats確認:**
- Batches: 増加が最小限（GPU Instancing有効化推奨）
- SetPass Calls: マテリアル数に応じて増加
- Tris: メッシュ変換前後で同数

**最適化設定:**
```csharp
// GPU Instancing有効化
Material mat = treeRenderer.sharedMaterial;
mat.enableInstancing = true;
```

### 6-3. スケール動作テスト

**テストスクリプト:**
```csharp
[ExecuteInEditMode]
public class TreeScaleTest : MonoBehaviour
{
    [Range(0.5f, 3.0f)] public float heightMultiplier = 1.0f;
    [Range(0.5f, 2.0f)] public float diameterMultiplier = 1.0f;

    private TreeScalerWithTVE scaler;

    void Update()
    {
        if (scaler == null) scaler = GetComponent<TreeScalerWithTVE>();

        scaler.ApplyScale(
            scaler.targetHeight * heightMultiplier,
            scaler.targetDiameter * diameterMultiplier
        );
    }
}
```

**Inspector動作確認:**
1. TreeScaleTestコンポーネント追加
2. heightMultiplier / diameterMultiplier スライダー変更
3. リアルタイムでスケール変化を確認

---

## Part 7: トラブルシューティング

### 問題1: 変換ボタンが押せない

**原因:**
- TVE Managerがシーンに存在しない
- Unsupported Prefabsが1以上

**解決:**
```
1. ヒエラルキー右クリック → BOXOPHOBIC/The Visual Engine/TVE Manager
2. FBXを直接Prefab化（Instantiate → Save As Prefab）
```

### 問題2: 変換後にメッシュが表示されない

**原因:**
- マテリアルシェーダーエラー
- メッシュファイル参照切れ

**解決:**
```
1. Prefab Inspector → MeshRenderer → Material確認
2. シェーダーがTVE/Vegetation/*であることを確認
3. メッシュが存在するか確認: Assets/Prefabs/Meshes/
```

### 問題3: スケール変更が反映されない

**原因:**
- Vertex Aチャンネルに高さデータなし
- MaterialPropertyBlock未適用

**解決:**
```csharp
// 頂点データ確認
Mesh mesh = meshFilter.sharedMesh;
Color[] colors = mesh.colors;
Debug.Log("Vertex A (Height Mask): " + colors[0].a);

// 0の場合、再変換が必要:
// Asset Converter → Vertex A → Source: Procedural → Height
```

### 問題4: 風エフェクトが動作しない

**原因:**
- TVE Managerの Motion Control が 0
- 風エレメント未配置

**解決:**
```
1. TVE Manager Inspector → Motion Control を 0.5 に設定
2. Demo/Elements/Direction Element (Rotate Me).prefab を配置
```

### 問題5: テクスチャが引き伸ばされる

**原因:**
- Tiling調整未実装

**解決:**
```csharp
// TreeScaler.cs に追加
void ApplyTextureTiling(float heightRatio)
{
    MaterialPropertyBlock props = new MaterialPropertyBlock();
    renderer.GetPropertyBlock(props);

    // Y方向のTilingを高さ比率で調整
    props.SetVector("_MainTex_ST", new Vector4(1, heightRatio, 0, 0));
    props.SetVector("_BumpMap_ST", new Vector4(1, heightRatio, 0, 0));

    renderer.SetPropertyBlock(props);
}
```

---

## Part 8: 実践例（WoodSimulatorプロジェクト統合）

### 8-1. 既存システムとの統合

**ForestGenerator.cs 拡張:**
```csharp
public class ForestGenerator : MonoBehaviour
{
    [Header("TVE Integration")]
    public bool useTVEScaling = true;

    void UpdateForest(int targetAge)
    {
        var growthData = growthDatabase.GetDataByAge(targetAge);

        foreach (var tree in activeTrees)
        {
            if (useTVEScaling)
            {
                var scaler = tree.GetComponent<TreeScalerWithTVE>();
                if (scaler != null)
                {
                    scaler.ApplyScale(growthData.height, growthData.diameter);
                }
            }
            else
            {
                // 従来のTreeScaler使用
                var legacyScaler = tree.GetComponent<TreeScaler>();
                legacyScaler.SetHeight(growthData.height);
            }
        }
    }
}
```

### 8-2. UI連動

**GrowthTimeline.cs 拡張:**
```csharp
public class GrowthTimeline : MonoBehaviour
{
    [Header("TVE Season Sync")]
    public TVEGlobalControl tveManager;
    public bool syncSeasonWithAge = true;

    void UpdateForest()
    {
        // 既存処理
        int currentAge = (int)ageSlider.value;
        forestGenerator.UpdateForest(currentAge);

        // TVE季節同期
        if (syncSeasonWithAge && tveManager != null)
        {
            // 10歳=冬(0), 100歳=夏(2)
            float seasonValue = Mathf.Lerp(0, 2, (currentAge - 10) / 90f);
            tveManager.seasonControl = seasonValue;
        }
    }
}
```

### 8-3. パフォーマンス最適化

**GPU Instancing一括設定:**
```csharp
void EnableGPUInstancingForAllTrees()
{
    var materials = new HashSet<Material>();

    foreach (var tree in activeTrees)
    {
        var renderers = tree.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            materials.Add(renderer.sharedMaterial);
        }
    }

    foreach (var mat in materials)
    {
        mat.enableInstancing = true;
    }

    Debug.Log($"GPU Instancing enabled for {materials.Count} materials");
}
```

---

## まとめ

### 変換手順の流れ

1. **準備**: TVE Manager配置、Prefab作成
2. **変換**: Asset Converter起動 → Preset選択 → Convert実行
3. **確認**: TVEPrefabコンポーネント、シェーダー、メッシュファイル確認
4. **スケール調整**: TreeScalerWithTVE実装、MaterialPropertyBlock使用
5. **統合**: ForestGenerator/GrowthTimeline連動

### 重要ポイント

- **頂点データ**: Vertex Aチャンネルに高さマスク格納
- **MaterialPropertyBlock**: 個別インスタンスのプロパティ変更
- **GPU Instancing**: パフォーマンス最適化必須
- **Tiling調整**: テクスチャ引き伸ばし防止

### 参考リンク

- TheVisualEngine使い方.md: 基本設定・エフェクト説明
- 開発記録.md: Phase 4実装詳細
- The Visual Engine.pdf: 公式マニュアル (Assets/BOXOPHOBIC/)
- Conversion Presets.pdf: プリセット詳細 (Assets/BOXOPHOBIC/The Visual Engine Presets/)

---

**作成日**: 2026-03-30
**対象プロジェクト**: WoodSimulator
**対象バージョン**: The Visual Engine (Unity 2021.3以降対応)
