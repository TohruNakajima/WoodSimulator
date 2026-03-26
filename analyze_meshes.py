import subprocess
import json
import re

meshes = []

# 各メッシュのTransform情報を取得してサイズを推定
for i in range(1, 7):
    mesh_name = f"BC_PM_P02_japanese_cedar_0{i}"
    path = f"JapaneseCedar/{mesh_name}"

    # Transformのローカルスケール取得
    cmd = f'curl -X POST http://localhost:56782/mcp/ -H "Content-Type: application/json" -d "{{\\\"jsonrpc\\\":\\\"2.0\\\",\\\"method\\\":\\\"tools/call\\\",\\\"params\\\":{{\\\"name\\\":\\\"Ins_GetComponentProperties\\\",\\\"arguments\\\":{{\\\"target\\\":\\\"{path}\\\",\\\"component\\\":\\\"Transform\\\"}}}},\\\"id\\\":1}}"'

    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)

    # m_LocalScaleを抽出
    match = re.search(r'm_LocalScale.*?\(Vector3\).*?=.*?\(([\d.]+),\s*([\d.]+),\s*([\d.]+)\)', result.stdout)

    if match:
        x, y, z = float(match.group(1)), float(match.group(2)), float(match.group(3))
        # Y軸（高さ）を基準にサイズ判定
        size = y
        meshes.append({
            'index': i,
            'name': mesh_name,
            'scale': (x, y, z),
            'size': size
        })
        print(f"Mesh {i}: scale=({x}, {y}, {z}), size={size}")

# サイズ順にソート（小さい順）
meshes.sort(key=lambda m: m['size'])

print("\n=== Sorted by size ===")
for idx, mesh in enumerate(meshes):
    print(f"{idx+1}. {mesh['name']} - size: {mesh['size']}")

# 結果をJSON出力
with open('D:/Tozawa_Unity/WoodSimulator/mesh_analysis.json', 'w') as f:
    json.dump(meshes, f, indent=2)

print("\nSaved to mesh_analysis.json")
