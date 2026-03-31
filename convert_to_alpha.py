from PIL import Image
import numpy as np

# JPGファイルを読み込み
img = Image.open('Assets/Material/BC_PM_P02_japanese_cedar_leaves_01_alb.jpg')
img_array = np.array(img)

# 元のRGB値はそのまま保持
# アルファチャンネルのみを生成する
gray = np.dot(img_array[...,:3], [0.299, 0.587, 0.114])

# 閾値を上げて背景をより積極的に透明化
# また、グラデーションを使用して自然な透明化を実現
threshold_min = 30  # これ以下は完全に透明
threshold_max = 80  # これ以上は完全に不透明

# グラデーション透明化
alpha = np.zeros_like(gray, dtype=np.uint8)
alpha[gray <= threshold_min] = 0  # 完全に透明
alpha[gray >= threshold_max] = 255  # 完全に不透明

# 中間はグラデーション
mask = (gray > threshold_min) & (gray < threshold_max)
alpha[mask] = ((gray[mask] - threshold_min) / (threshold_max - threshold_min) * 255).astype(np.uint8)

# RGBAイメージを作成
rgba = np.dstack((img_array, alpha))

# PNG形式で保存
output = Image.fromarray(rgba, 'RGBA')
output.save('Assets/Material/BC_PM_P02_japanese_cedar_leaves_01_alb.png')

print("PNG with alpha channel created: Assets/Material/BC_PM_P02_japanese_cedar_leaves_01_alb.png")
print(f"Alpha range: min={alpha.min()}, max={alpha.max()}")
