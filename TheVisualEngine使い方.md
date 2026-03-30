# The Visual Engine 使い方ガイド

## 概要

**The Visual Engine (TVE)** は、BOXOPHOBIC社が提供するUnity向けの高度な植生レンダリングシステムです。リアルタイムで植生や環境に動的なエフェクトを適用し、風、雨、雪、インタラクション、色の変化などを実現できます。

---

## 主要コンポーネント

### 1. TVE Manager（コアシステム）

TVE Managerは、The Visual Engineの中心的なコンポーネントで、すべてのエレメントとエフェクトを統括管理します。

#### 配置方法
1. ヒエラルキーで右クリック → `BOXOPHOBIC/The Visual Engine/TVE Manager` を選択
2. シーン内に1つだけ配置してください（複数配置すると競合します）

#### 主要設定

##### Quick Settings（クイック設定）
- **Motion Control** (0～1): グローバルな風の強度を制御
  - 0: 風なし
  - 0.5: 通常の風
  - 1: 強風

- **Season Control** (0～4): 季節の変化を制御
  - 0: 冬（Winter）
  - 1: 春（Spring）
  - 2: 夏（Summer）
  - 3: 秋（Autumn）
  - 4: 冬に戻る

##### Main Settings（メイン設定）
- **Main Camera**: シーンのメインカメラを指定（自動検出可能）
- **Main Light**: シーンのメインライト（太陽光）を指定
- **Main Wind**: 風の方向を決定するGameObjectを指定
- **Auto Assign Main Objects**: 上記を自動で割り当てる

##### Player Settings（プレイヤー設定）
- **Player Object**: プレイヤーのGameObjectを指定
- **Player Radius**: プレイヤー周辺の影響範囲

##### Global Settings（グローバル設定）
各エフェクトのグローバルな強度を制御します：
- **Coat Data**: コーティング効果（霜、雪など）の強度
- **Paint Data**: 色付け効果の強度
- **Atmo Data**: 大気効果（乾燥、湿気、雨など）の強度
- **Glow Data**: 発光・サブサーフェス効果の強度
- **Form Data**: 形状変形効果の強度

##### Element Settings（エレメント設定）
- **Element Visibility**: エレメントの表示/非表示設定
  - `Always Hidden`: 常に非表示
  - `Always Visible`: 常に表示
  - `Hidden At Runtime`: 実行時に非表示（デフォルト）

- **Element Ordering**: エレメントのソート方法
  - `Sort In Edit Mode`: エディタモードのみソート
  - `Sort At Runtime`: 実行時も常にソート

---

### 2. TVE Element（エフェクト要素）

TVE Elementは、特定のエフェクトをシーン内に配置するためのコンポーネントです。パーティクルシステムやメッシュに取り付けて使用します。

#### エレメントの種類

##### Atmo（大気系）
- **Atmo Dryness**: 乾燥効果
- **Atmo Overlay**: オーバーレイ効果
- **Atmo Rainfall**: 降雨効果
- **Atmo Wetness**: 濡れ効果

##### Coat（コーティング系）
- **Coat Detail**: 詳細コーティング（霜など）
- **Coat Layer**: レイヤーコーティング（雪の層）
- **Coat Stack**: スタックコーティング（積雪）

##### Flow（動き系）
- **Flow Interaction Simple**: シンプルなインタラクション
- **Flow Interaction**: 高度なインタラクション効果
- **Flow Wind Intensity**: 風の強度制御
- **Flow Wind Intensity Animated**: アニメーション付き風

##### Form（形状系）
- **Form Size Fade**: サイズフェード効果

##### Glow（発光系）
- **Glow Emissive**: 発光効果
- **Glow Subsurface**: サブサーフェス散乱効果

##### Paint（色彩系）
- **Paint Cutout**: カットアウト効果
- **Paint Disco**: ディスコ効果（色変化）
- **Paint Map**: マップベースの色付け
- **Paint Map Seasons**: 季節変化する色付け

#### エレメントの配置方法

1. シーンに空のGameObjectを作成
2. `Add Component` → `BOXOPHOBIC/The Visual Engine/TVE Element` を追加
3. Inspectorで適切なマテリアルを `Custom Material` に設定
4. または、デモプレハブ（`Assets/BOXOPHOBIC/The Visual Engine/Demo/Elements/`）を直接配置

#### エレメント設定

- **Element Refresh**: エレメントの更新モード
  - `Realtime`: リアルタイム更新
  - `On Demand`: 必要時のみ更新

- **Element Visibility**: 個別の表示設定
  - `Use Global Settings`: グローバル設定に従う
  - `Always Hidden`: 常に非表示
  - `Always Visible`: 常に表示

- **Custom Material**: エレメント専用のマテリアル

- **Terrain Data**: Terrainデータを使用する場合に指定
- **Terrain Mask**: Terrainのマスクタイプを選択

---

### 3. シェーダーとマテリアル

The Visual Engineには、植生専用のシェーダーが多数含まれています。

#### シェーダーの場所
- `Assets/BOXOPHOBIC/The Visual Engine/Core/Shaders/`

#### 主要シェーダーカテゴリ

##### Elements（エレメント用）
- エフェクトを描画するためのシェーダー
- 上記のAtmo、Coat、Flow、Form、Glow、Paint系

##### Effects（エフェクト用）
- **CustomRT Drips**: 雫エフェクト
- **CustomRT Drops**: 水滴エフェクト
- **CustomRT Flipbook**: フリップブックアニメーション
- **CustomRT Flutter**: 揺れエフェクト
- **CustomRT Glitter**: 輝きエフェクト
- **CustomRT Motion**: モーションエフェクト

##### Geometry（ジオメトリ用）
- 植生のメッシュレンダリング用シェーダー

---

## 基本的な使い方

### ステップ1: TVE Managerの配置

1. 新しいシーンを開く
2. ヒエラルキーで右クリック → `BOXOPHOBIC/The Visual Engine/TVE Manager`
3. Inspector で `Motion Control` と `Season Control` を調整

### ステップ2: 植生アセットの準備

1. The Visual Engineに対応したシェーダーを使用したマテリアルを作成
2. または、デモアセット（`Assets/BOXOPHOBIC/The Visual Engine/Demo/Prefabs/`）を使用

### ステップ3: エレメントの配置

#### 風のエフェクト
1. `Demo/Elements/Direction Element (Rotate Me).prefab` を配置
2. Y軸回転で風向きを調整
3. TVE Managerの `Motion Control` で風の強度を調整

#### インタラクションエフェクト
1. `Demo/Elements/Particle (Motion Interaction Trail).prefab` をプレイヤーの子オブジェクトとして配置
2. プレイヤーが移動すると、植生が反応します

#### 色変化エフェクト
1. `Demo/Elements/Particle (Color Reveal).prefab` を配置
2. 配置したエリアの植生の色が変化します

### ステップ4: シーズン変化の設定

1. TVE Managerの `Season Control` スライダーを動かす
2. 0（冬）→ 1（春）→ 2（夏）→ 3（秋）→ 4（冬）
3. 植生マテリアルが季節変化に対応している場合、色や状態が変化します

---

## 高度な設定

### レイヤーシステム

The Visual Engineは、最大9つのレイヤーでエレメントを管理できます。

#### レイヤー名のカスタマイズ
1. `Assets/BOXOPHOBIC/The Visual Engine/Core/Resources/Internal Layers.txt` をコピー
2. ローカルの `Resources` フォルダに配置
3. レイヤー名を編集:
```
Default 0 Vegetation 1 Grass 2 Props 3 Character 4 Flowers 5 Mountains 6 Houses 7 My_Custom_Layer 8
```

#### エレメントレイヤーの指定
- エレメントマテリアルの `Element Layer Mask` でビットマスクを設定
- 複数レイヤーに同時に影響を与えることも可能

### レンダリング設定の最適化

#### Element Renderer設定（TVE Manager内）
- **Base Texture**: ベーステクスチャの解像度（デフォルト: 1024）
- **Near Texture**: ニアテクスチャの解像度（デフォルト: 512）
- **Base Radius**: ベースレンダリング範囲（デフォルト: 100m）
- **Near Radius**: ニアレンダリング範囲（デフォルト: 20m）
- **Base To Near Blend**: ベースとニアのブレンド比率

### パーティクルエレメントの使用

パーティクルシステムとTVE Elementを組み合わせることで、動的なエフェクトを実現できます。

#### 例: 火の玉エフェクト
1. パーティクルシステムを作成
2. TVE Elementコンポーネントを追加
3. `Demo/Elements/Materials/` から適切なマテリアルを選択
4. パーティクルの動きに合わせてエフェクトが描画されます

### Terrain連携

UnityのTerrainデータをエレメントとして使用できます。

1. TVE Elementコンポーネントを持つGameObjectを作成
2. `Terrain Data` フィールドにTerrainを割り当て
3. `Terrain Mask` で使用するデータを選択:
   - `Auto`: 自動選択
   - `Height`: 高さマップ
   - `Splat`: スプラットマップ

---

## トラブルシューティング

### エフェクトが表示されない
- TVE Managerがシーンに配置されているか確認
- Main Cameraが正しく設定されているか確認
- Element Visibilityが `Always Hidden` になっていないか確認

### パフォーマンスが低い
- Element Renderingの解像度を下げる（Base Texture / Near Textureを512以下に）
- Element Orderingを `Sort In Edit Mode` に設定
- 不要なエレメントを削除

### 植生が風に反応しない
- TVE Managerの `Motion Control` が0になっていないか確認
- 植生マテリアルがThe Visual Engine対応シェーダーを使用しているか確認
- 風のエレメント（Direction Element）が配置されているか確認

### 季節変化が機能しない
- 植生マテリアルが季節変化に対応しているか確認（Paint Map Seasonsなど）
- TVE Managerの `Season Control` が変化しているか確認

---

## デモシーンの活用

`Assets/BOXOPHOBIC/The Visual Engine/Demo/` には、様々なサンプルプレハブとエレメントが含まれています。

### 推奨される学習順序
1. `Demo/Elements/` のプレハブを1つずつシーンに配置して効果を確認
2. `Demo/Prefabs/` の植生プレハブを使ってテストシーンを作成
3. 独自の植生アセットにThe Visual Engineシェーダーを適用

---

## まとめ

The Visual Engineの基本的な使い方：
1. **TVE Manager** をシーンに1つ配置
2. **植生アセット** にTVE対応シェーダーを適用
3. **TVE Element** を配置してエフェクトを追加
4. **Motion Control** と **Season Control** で全体を調整

これらの基本を理解すれば、リアルタイムで動的に変化する美しい植生環境を構築できます。

---

## 参考リンク

- 公式マニュアル: [The Visual Engine PDF](Assets/BOXOPHOBIC/The Visual Engine/The Visual Engine.pdf)
- 公式ドキュメント: https://docs.google.com/document/d/145JOVlJ1tE-WODW45YoJ6Ixg23mFc56EnB_8Tbwloz8/
- Discord: BOXOPHOBICコミュニティ
- Contact: BOXOPHOBIC公式サイト

---

**作成日**: 2026-03-30
**対象バージョン**: The Visual Engine (Unity 2021.3以降対応)
