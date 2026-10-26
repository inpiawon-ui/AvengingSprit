# 코덱스 납품 시트(분홍 #FF00FF 바탕)에서 판을 잘라 바탕을 뚫고 키트 크기(시안 1080 → 게임 720, 2/3)로 줄인다.
# 가장자리는 분홍과 섞여 있다 — 판 안쪽 색(F)을 알면 섞인 비율(알파)과 원래 색을 되돌릴 수 있다:
#   m(c) = (R + B) / 2 - G   (분홍은 255, 판 색은 작다) → 알파 = (255 - m(c)) / (255 - m(F))
# 쓰는 법: python cut_magenta_parts.py <시트.png> <내보낼 폴더> 이름=왼,위,오른,아래 [이름=…]
import sys
from PIL import Image


def magenta_score(c):
    return (c[0] + c[2]) / 2.0 - c[1]


def cut(sheet, box, out_path, scale=2.0 / 3.0):
    part = sheet.crop(box).convert("RGB")
    w, h = part.size
    px = part.load()
    # 판 안쪽 색 — 분홍이 아닌 칸들의 가운데 값
    inner = [px[x, y] for y in range(h) for x in range(w) if magenta_score(px[x, y]) < 120]
    inner.sort(key=lambda c: c[0] + c[1] + c[2])
    f = inner[len(inner) // 2] if inner else (0, 0, 0)
    mf = magenta_score(f)
    out = Image.new("RGBA", (w, h))
    op = out.load()
    for y in range(h):
        for x in range(w):
            c = px[x, y]
            a = (255.0 - magenta_score(c)) / max(1.0, 255.0 - mf)
            a = max(0.0, min(1.0, a))
            if a <= 0.02:
                op[x, y] = (0, 0, 0, 0)
                continue
            # 분홍을 덜어 낸 원래 색
            rgb = tuple(int(max(0, min(255, (c[i] - (1 - a) * (255, 0, 255)[i]) / a))) for i in range(3))
            op[x, y] = rgb + (int(round(a * 255)),)
    if scale != 1.0:
        out = out.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.LANCZOS)
    out.save(out_path)
    print(out_path, out.size, "판 색", "#%02X%02X%02X" % f)


def main():
    sheet = Image.open(sys.argv[1])
    folder = sys.argv[2]
    for arg in sys.argv[3:]:
        name, nums = arg.split("=")
        box = tuple(int(v) for v in nums.split(","))
        cut(sheet, box, f"{folder}/{name}.png")


if __name__ == "__main__":
    main()
