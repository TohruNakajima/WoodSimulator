#!/usr/bin/env python3
"""OneTree.unityシーンでAge25-100_TreeのSetActiveをTrueに変更"""

from pathlib import Path
import re

scene_path = Path("D:/Tozawa_Unity/WoodSimulator/Assets/Scenes/OneTree.unity")

if not scene_path.exists():
    print(f"[ERROR] Scene not found: {scene_path}")
    exit(1)

content = scene_path.read_text(encoding='utf-8')
original_content = content

# Age25/40/55/75/100_Treeの m_IsActive: 0 を m_IsActive: 1 に変更
# 対象のGameObject名パターンに続く m_IsActive を探す

# Age25_Tree, Age40_Tree, Age55_Tree, Age75_Tree, Age100_Tree の各GameObjectブロックを探して
# m_IsActive: 0 を m_IsActive: 1 に変更

# パターン:
# GameObject:
#   ...
#   m_Name: Age25_Tree (など)
#   ...
#   m_IsActive: 0
# を
#   m_IsActive: 1
# に変更

tree_names = ["Age25_Tree", "Age40_Tree", "Age55_Tree", "Age75_Tree", "Age100_Tree"]

for tree_name in tree_names:
    # GameObjectブロックを探す（m_Name: が含まれる GameObject から次の --- まで）
    pattern = r'(--- !u!1 &-?\d+\nGameObject:(?:[^\n]*\n)*?  m_Name: ' + tree_name + r'(?:[^\n]*\n)*?)(  m_IsActive: )0'

    matches = re.findall(pattern, content)
    if matches:
        print(f"[FOUND] {tree_name}: m_IsActive: 0")
        content = re.sub(pattern, r'\1\g<2>1', content)
        print(f"[CHANGED] {tree_name}: m_IsActive: 0 → 1")
    else:
        print(f"[NOT FOUND or ALREADY ACTIVE] {tree_name}")

if content == original_content:
    print("\n[NO CHANGE] シーンファイルに変更なし")
else:
    scene_path.write_text(content, encoding='utf-8')
    print("\n[SUCCESS] シーンファイル更新完了")

print("\n=== 完了 ===")
