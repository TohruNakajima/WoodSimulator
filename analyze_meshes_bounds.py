import subprocess
import json
import re

meshes = []

# 各メッシュのbounds情報を取得
for i in range(1, 7):
    mesh_name = f"BC_PM_P02_japanese_cedar_0{i}"
    path = f"JapaneseCedar/{mesh_name}"

    cmd = f'curl -X POST http://localhost:56782/mcp/ -H "Content-Type: application/json" -d "{{\\\"jsonrpc\\\":\\\"2.0\\\",\\\"method\\\":\\\"tools/call\\\",\\\"params\\\":{{\\\"name\\\":\\\"Ins_GetMeshBounds\\\",\\\"arguments\\\":{{\\\"target\\\":\\\"{path}\\\"}}}},\\\"id\\\":1}}"'

    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)

    # Bounds Sizeを抽出
    match = re.search(r'Bounds Size: \(([\d.]+), ([\d.]+), ([\d.]+)\)', result.stdout)

    if match:
        x, y, z = float(match.group(1)), float(match.group(2)), float(match.group(3))
        # Y軸（高さ）を基準にサイズ判定
        size = y
        meshes.append({
            'index': i,
            'name': mesh_name,
            'bounds_size': (x, y, z),
            'height': size
        })
        print(f"Mesh {i}: bounds_size=({x}, {y}, {z}), height={size}")

# 高さ順にソート（小さい順）
meshes.sort(key=lambda m: m['height'])

print("\n=== Sorted by height (smallest to largest) ===")
for idx, mesh in enumerate(meshes):
    print(f"{idx+1}. {mesh['name']} - height: {mesh['height']:.2f}m")

# 結果をJSON出力
with open('D:/Tozawa_Unity/WoodSimulator/mesh_bounds.json', 'w') as f:
    json.dump(meshes, f, indent=2)

print("\nSaved to mesh_bounds.json")
