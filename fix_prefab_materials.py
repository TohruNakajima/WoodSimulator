#!/usr/bin/env python3
"""Age25-100 Prefabのマテリアル参照を専用マテリアルに変更"""

import re
from pathlib import Path

# マテリアルGUID一覧
material_guids = {
    10: {
        "leaves": "e3172152dc975874ca935f1ed034876c",  # JapaneseCedar_Leaves_Age10
        "trunk": "c6a5e51cb8be8b0469a2ce5ed16de6ed"   # JapaneseCedar_Trunk_Age10
    },
    25: {
        "leaves": "791f1b8f429375049a3d1a89f5439a9c",  # JapaneseCedar_Leaves_Age25
        "trunk": "87e8f2469a5526249ae23c04d2edd9e9"   # JapaneseCedar_Trunk_Age25
    },
    40: {
        "leaves": "4ef4b23a97a61224c857a9a9e0651fc3",  # JapaneseCedar_Leaves_Age40
        "trunk": "5dd8d1ee08f73a44b8b5d35ca7adf4b3"   # JapaneseCedar_Trunk_Age40
    },
    55: {
        "leaves": "e8bb41cb5d6fd744abc8ca8b8f13a14f",  # JapaneseCedar_Leaves_Age55
        "trunk": "3b8cd3dcb13a7c54c8b176e8efd67b7d"   # JapaneseCedar_Trunk_Age55
    },
    75: {
        "leaves": "be1d36ddb86df9447b3b1e02ea47f09c",  # JapaneseCedar_Leaves_Age75
        "trunk": "3daabdaa6c83e8a498d2e6db4b27e0e8"   # JapaneseCedar_Trunk_Age75
    },
    100: {
        "leaves": "6eee59cc14a5d0c4f948d56d9b1ff5d2",  # JapaneseCedar_Leaves_Age100
        "trunk": "bb8ad952c30a3914ababa4b5eb9b4feb"   # JapaneseCedar_Trunk_Age100
    }
}

# 共有マテリアルGUID（置換対象）
SHARED_LEAVES_GUID = "791f1b8f429375049a3d1a89f5439a9c"  # JapaneseCedar_Leaves（元のファイル）
SHARED_TRUNK_GUID = "87e8f2469a5526249ae23c04d2edd9e9"   # JapaneseCedar_Trunk（元のファイル）

def fix_prefab(age: int):
    """指定された年齢のPrefabのマテリアル参照を修正"""
    prefab_path = Path(f"D:/Tozawa_Unity/WoodSimulator/Assets/Prefabs/SingleTree/Age{age}_Tree.prefab")

    if not prefab_path.exists():
        print(f"[ERROR] Prefab not found: {prefab_path}")
        return False

    content = prefab_path.read_text(encoding='utf-8')
    original_content = content

    # マテリアル参照のパターン: "- {fileID: 2100000, guid: XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX, type: 2}"
    # Age25は既に正しいGUIDを持っているので、スキップ
    if age == 25:
        print(f"[SKIP] Age{age}_Tree.prefab already has correct GUID")
        return True

    # Age25-100の場合、共有マテリアルGUIDを専用マテリアルGUIDに置換
    leaves_guid = material_guids[age]["leaves"]
    trunk_guid = material_guids[age]["trunk"]

    # 葉マテリアル置換（Age25のGUIDを各年齢の専用GUIDに）
    pattern_leaves = r'(- {fileID: 2100000, guid: )791f1b8f429375049a3d1a89f5439a9c(, type: 2})'
    content = re.sub(pattern_leaves, rf'\g<1>{leaves_guid}\g<2>', content)

    # 幹マテリアル置換（Age25のGUIDを各年齢の専用GUIDに）
    pattern_trunk = r'(- {fileID: 2100000, guid: )87e8f2469a5526249ae23c04d2edd9e9(, type: 2})'
    content = re.sub(pattern_trunk, rf'\g<1>{trunk_guid}\g<2>', content)

    if content == original_content:
        print(f"[NO CHANGE] Age{age}_Tree.prefab")
        return True

    prefab_path.write_text(content, encoding='utf-8')
    print(f"[FIXED] Age{age}_Tree.prefab")
    print(f"  Leaves: 791f1b8f -> {leaves_guid}")
    print(f"  Trunk:  87e8f246 -> {trunk_guid}")
    return True

def main():
    print("=== Prefabマテリアル参照修正 ===\n")

    # Age40, 55, 75, 100を修正
    for age in [40, 55, 75, 100]:
        fix_prefab(age)

    print("\n=== 修正完了 ===")

if __name__ == "__main__":
    main()
