import requests
import json

# MCPエンドポイント
MCP_URL = "http://localhost:56782/mcp/"

def call_mcp(method, arguments):
    """MCPツールを呼び出す"""
    payload = {
        "jsonrpc": "2.0",
        "method": "tools/call",
        "params": {
            "name": method,
            "arguments": arguments
        },
        "id": 1
    }
    response = requests.post(MCP_URL, json=payload, headers={"Content-Type": "application/json"})
    result = response.json()
    if result.get("error"):
        raise Exception(f"MCP Error: {result['error']}")
    return result["result"]["content"][0]["text"]

def get_tree_height(tree_path):
    """木の高さを取得"""
    # 子メッシュのパスを取得
    info = call_mcp("Ins_GetGameObjectInfo", {"target": tree_path})

    # メッシュの高さを取得（Renderer bounds）
    # パースして子の名前を取得
    lines = info.split('\n')
    child_name = None
    for line in lines:
        if 'BC_PM_P02_japanese_cedar' in line:
            # "- BC_PM_P02_japanese_cedar_XX [ID:...]" から名前を抽出
            child_name = line.split('[')[0].strip('- ').strip()
            break

    if not child_name:
        return None

    mesh_path = f"{tree_path}/{child_name}"

    # Transformプロパティを取得してLocalScaleを確認
    transform = call_mcp("Ins_GetComponentProperties", {
        "target": mesh_path,
        "component": "Transform"
    })

    return mesh_path, transform

# 各木の測定
trees = [
    ("Age10_Tree", "SingleTreeSimulator/TreeContainer/Age10_Tree"),
    ("Age25_Tree", "SingleTreeSimulator/TreeContainer/Age25_Tree"),
    ("Age40_Tree", "SingleTreeSimulator/TreeContainer/Age40_Tree"),
    ("Age55_Tree", "SingleTreeSimulator/TreeContainer/Age55_Tree"),
    ("Age75_Tree", "SingleTreeSimulator/TreeContainer/Age75_Tree"),
    ("Age100_Tree", "SingleTreeSimulator/TreeContainer/Age100_Tree"),
]

results = {}

for name, path in trees:
    print(f"\n測定中: {name}")

    # 一時的にActive化
    call_mcp("Ins_SetActive", {"target": path, "active": True})

    # 高さ取得
    mesh_path, transform_info = get_tree_height(path)
    print(f"  メッシュパス: {mesh_path}")
    print(f"  Transform情報:\n{transform_info}")

    results[name] = {
        "mesh_path": mesh_path,
        "transform": transform_info
    }

    # 元に戻す（Age10以外は非表示）
    if name != "Age10_Tree":
        call_mcp("Ins_SetActive", {"target": path, "active": False})

# 結果を保存
with open('tree_heights_measurement.json', 'w', encoding='utf-8') as f:
    json.dump(results, f, indent=2, ensure_ascii=False)

print("\n測定完了: tree_heights_measurement.json に保存しました")
