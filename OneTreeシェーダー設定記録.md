# OneTree.unity 杉モデルシェーダー設定詳細記録

## 概要
OneTree.unityシーンで正常に表示されている杉モデルのマテリアル・シェーダー設定を記録。
後ほどこの設定をLikeaDeemo.unityシーンに適用する。

## シーン構成

### OneTree.unity
- **パス**: `Assets/Scenes/OneTree.unity`
- **構造**:
  ```
  SingleTreeSimulator
    └─ TreeContainer
       ├─ Age10_Tree (Active: True)
       │  └─ BC_PM_P02_japanese_cedar_01
       ├─ Age25_Tree (Active: False)
       ├─ Age40_Tree (Active: False)
       ├─ Age55_Tree (Active: False)
       ├─ Age75_Tree (Active: False)
       └─ Age100_Tree (Active: False)
  ```

## Prefab構成

### Age10_Tree.prefab
- **パス**: `Assets/Prefabs/SingleTree/Age10_Tree.prefab`
- **使用マテリアル**:
  1. **幹マテリアル**: guid `4e012567b3c522144ae1b28c7cdb4331`
  2. **葉マテリアル**: guid `ccf8007166da5f145880e347c23b47df`

## マテリアル詳細

### 1. 幹マテリアル: BC_PM_P02_japanese_cedar_trunk_01_alb.mat

**パス**: `Assets/Model/Materials/BC_PM_P02_japanese_cedar_trunk_01_alb.mat`
**GUID**: `4e012567b3c522144ae1b28c7cdb4331`

#### シェーダー設定
```yaml
m_Shader: {fileID: 4800000, guid: a933075b367f9b24981408633f72ff34, type: 3}
# TVE General Subsurface Lit
```

#### キーワード
```yaml
m_ValidKeywords:
  - TVE_FILTER_DEFAULT
  - TVE_MAIN_SAMPLE_MAIN_UV
  - TVE_PIVOT_SINGLE
m_InvalidKeywords: []
```

#### レンダー設定
```yaml
m_LightmapFlags: 0
m_EnableInstancingVariants: 0
m_DoubleSidedGI: 0
m_CustomRenderQueue: 2000
stringTagMap:
  NatureRendererInstancing: true
  RenderType: TransparentCutout
disabledShaderPasses:
  - TransparentBackface
  - TransparentBackfaceDebugDisplay
  - TransparentDepthPrepass
  - TransparentDepthPostpass
```

#### テクスチャマッピング（幹）
```yaml
_AlbedoTex: guid 3d8c089a279942f408c19c4bad5fd89b  # trunk_01_alb.jpg
_BaseColorMap: guid 3d8c089a279942f408c19c4bad5fd89b
_MainTex: guid 3d8c089a279942f408c19c4bad5fd89b
_MainAlbedoTex: guid 3d8c089a279942f408c19c4bad5fd89b
_BumpMap: guid 77496d195623a0b45a79a6ed850cc579  # trunk_01_nrm.png
_MainNormalTex: guid 77496d195623a0b45a79a6ed850cc579
_NormalMap: guid 77496d195623a0b45a79a6ed850cc579
_MotionNoiseTex: guid 9881e099480439147ba51cc2d31303b6
_NoiseTex3D: guid 038de154ac2857945b4e07ea7b2c8629
_OverlayNormalTex: guid 918024ada31e4e043bd2d6e6a4242b4c
```

#### 重要パラメータ（幹）
```yaml
_Cull: 2
_CullMode: 2
_Cutoff: 0.5
_RenderMode: 0  # Opaque
_RenderCull: 2
_RenderZWrite: 1
_IsConverted: 1
_IsTVEShader: 1
_IsSubsurfaceShader: 1
_IsGeneralShader: 1
_IsVersion: 2150
```

---

### 2. 葉マテリアル: BC_PM_P02_japanese_cedar_leaves_01_alb.mat

**パス**: `Assets/Model/Materials/BC_PM_P02_japanese_cedar_leaves_01_alb.mat`
**GUID**: `ccf8007166da5f145880e347c23b47df`

#### シェーダー設定
```yaml
m_Shader: {fileID: 4800000, guid: a933075b367f9b24981408633f72ff34, type: 3}
# TVE General Subsurface Lit
```

#### キーワード
```yaml
m_ValidKeywords:
  - TVE_FILTER_DEFAULT
  - TVE_MAIN_SAMPLE_MAIN_UV
  - TVE_PIVOT_SINGLE
m_InvalidKeywords:
  - TVE_ALPHA_CLIP  # ← 重要: ALPHA_CLIPは無効化されている
```

#### レンダー設定（葉）
```yaml
m_LightmapFlags: 0
m_EnableInstancingVariants: 1
m_DoubleSidedGI: 0
m_CustomRenderQueue: 2000  # ← 重要: 2000 (Opaqueキュー)
stringTagMap:
  NatureRendererInstancing: true
  RenderType: TransparentCutout  # ← RenderTypeはTransparentCutout
disabledShaderPasses:
  - TransparentBackface
  - TransparentBackfaceDebugDisplay
  - TransparentDepthPrepass
  - TransparentDepthPostpass
```

#### テクスチャマッピング（葉）
```yaml
_AlbedoTex: guid 44bb79d354263964d8906c1fd13a6509  # leaves_01_alb.jpg
_BaseColorMap: guid 44bb79d354263964d8906c1fd13a6509
_MainTex: guid 44bb79d354263964d8906c1fd13a6509
_MainAlbedoTex: guid 44bb79d354263964d8906c1fd13a6509
_BumpMap: guid 5c24308cf9617e74cad2a32dedf9270b  # leaves_01_nrm.png
_MainNormalTex: guid 5c24308cf9617e74cad2a32dedf9270b
_NormalMap: guid 5c24308cf9617e74cad2a32dedf9270b
_MotionNoiseTex: guid 9881e099480439147ba51cc2d31303b6
_NoiseTex3D: guid 038de154ac2857945b4e07ea7b2c8629
_OverlayNormalTex: guid 918024ada31e4e043bd2d6e6a4242b4c
```

#### 重要パラメータ（葉）
```yaml
_AlphaClipValue: 0.3
_AlphaCutoff: 0.5
_AlphaCutoffEnable: 1
_AlphaCutoffPrepass: 0.5
_AlphaCutoffPostpass: 0.5
_AlphaCutoffValue: 0.4
_Cutoff: 0.5
_MainAlphaClipValue: 0.5

_Cull: 0  # 両面レンダリング
_CullMode: 2
_RenderMode: 0  # Opaque
_RenderCull: 2
_RenderZWrite: 1

_Glossiness: 0.3
_MainSmoothnessValue: 0
_SmoothnessTextureChannel: 1

_IsConverted: 1
_IsTVEShader: 1
_IsSubsurfaceShader: 1
_IsGeneralShader: 1
_IsIdentifier: 27  # 幹は43、葉は27
_IsVersion: 2150
```

## Alpha Clipping問題の核心

### 現在の設定（OneTree.unityで正常動作）
- **RenderQueue**: 2000 (Opaque)
- **RenderType**: TransparentCutout
- **TVE_ALPHA_CLIP**: InvalidKeywords（無効）
- **Alpha Cutoff値**: 複数設定あり（0.3, 0.4, 0.5）
- **_Cull**: 0 （両面レンダリング）

### TVEシェーダーのAlpha処理
TVEシェーダーは独自のAlpha処理システムを持っており、Unity標準のCutoutモード（_Mode: 1）とは異なる：
- TVE_ALPHA_CLIPキーワードを**使わない**
- RenderQueueは2000（Opaque）を使用
- RenderTypeタグでTransparentCutoutを指定
- _AlphaCutoffEnable: 1 で有効化
- 複数のCutoff値で段階的処理

## 再現手順（LikeaDeemo.unityへの適用）

### 必要な作業
1. LikeaDeemo.unityシーンの杉モデルマテリアルを確認
2. 上記の設定を完全に再現：
   - m_CustomRenderQueue: 2000
   - m_ValidKeywords: TVE_FILTER_DEFAULT, TVE_MAIN_SAMPLE_MAIN_UV, TVE_PIVOT_SINGLE
   - m_InvalidKeywords: TVE_ALPHA_CLIP
   - stringTagMap RenderType: TransparentCutout
   - _AlphaCutoffEnable: 1
   - _Cutoff: 0.5
   - _AlphaClipValue: 0.3
3. RefreshAssets実行
4. シーン保存
5. Unity Editorで視覚確認

## 注意事項
- **TVE_ALPHA_CLIPは使用しない**（InvalidKeywordsに入れる）
- **RenderQueueは2000固定**（2450ではない）
- **Standardシェーダーへの変更は絶対禁止**
- TVEシェーダー固定（guid: a933075b367f9b24981408633f72ff34）
