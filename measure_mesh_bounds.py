"""
各木モデルのメッシュBoundsを測定して実際の高さを取得
"""
import re

# 開発記録から既知の高さ情報
KNOWN_HEIGHTS = {
    "cedar_01": 3.72,  # Age10
    "cedar_03": 3.80,  # Age25
    "cedar_05": 3.90,  # Age40
    "cedar_04": 3.97,  # Age55
    "cedar_06": 4.36,  # Age75
    "cedar_02": 4.75,  # Age100
}

# 対応表
TREE_TO_CEDAR = {
    "Age10_Tree": "cedar_01",
    "Age25_Tree": "cedar_03",
    "Age40_Tree": "cedar_05",
    "Age55_Tree": "cedar_04",
    "Age75_Tree": "cedar_06",
    "Age100_Tree": "cedar_02",
}

# 現在のLocalScale
CURRENT_SCALE = 1.20

# 標準高さを決定（最小値を基準にする）
BASE_HEIGHT = min(KNOWN_HEIGHTS.values())  # 3.72m (cedar_01)

print("=== 木モデルの高さ情報 ===\n")
print(f"基準高さ（標準化後）: {BASE_HEIGHT}m\n")

results = []

for tree_name, cedar_name in TREE_TO_CEDAR.items():
    original_height = KNOWN_HEIGHTS[cedar_name]
    # 現在のScale適用後の高さ
    current_height = original_height * CURRENT_SCALE

    # 標準高さにするためのスケール係数
    normalization_scale = BASE_HEIGHT / original_height

    # 標準化後のスケール（現在のScaleと組み合わせ）
    final_scale = CURRENT_SCALE * normalization_scale

    results.append({
        "tree": tree_name,
        "cedar": cedar_name,
        "original_height": original_height,
        "current_scale": CURRENT_SCALE,
        "current_height": current_height,
        "normalization_scale": normalization_scale,
        "final_scale": final_scale,
        "normalized_height": BASE_HEIGHT * CURRENT_SCALE
    })

    print(f"{tree_name} ({cedar_name}):")
    print(f"  元の高さ: {original_height}m")
    print(f"  現在のScale: {CURRENT_SCALE}")
    print(f"  現在の高さ: {current_height:.3f}m")
    print(f"  標準化係数: {normalization_scale:.4f}")
    print(f"  適用すべきScale: {final_scale:.4f}")
    print(f"  標準化後の高さ: {BASE_HEIGHT * CURRENT_SCALE:.3f}m")
    print()

print("\n=== SingleTreeGrowth.csで使用する定数 ===\n")
print(f"private const float BASE_MODEL_HEIGHT = {BASE_HEIGHT}f; // 標準化前の基準高さ")
print(f"private const float MODEL_SCALE = {CURRENT_SCALE}f; // 現在適用されているScale")
print(f"private const float NORMALIZED_BASE_HEIGHT = {BASE_HEIGHT * CURRENT_SCALE}f; // 標準化後の基準高さ")
print()
print("// 各モデルの標準化スケール")
print("private readonly float[] modelNormalizationScales = new float[] {")
for r in results:
    print(f"    {r['normalization_scale']:.4f}f,  // {r['tree']} ({r['cedar']})")
print("};")

# JSON保存
import json
with open('tree_model_heights.json', 'w', encoding='utf-8') as f:
    json.dump({
        "base_height": BASE_HEIGHT,
        "current_scale": CURRENT_SCALE,
        "normalized_base_height": BASE_HEIGHT * CURRENT_SCALE,
        "models": results
    }, f, indent=2, ensure_ascii=False)

print("\n測定完了: tree_model_heights.json に保存しました")
