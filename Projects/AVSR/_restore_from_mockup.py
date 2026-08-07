"""목업에서 UI 에셋을 복원한다 — 재생성이 아니라 원본 크롭 기반이므로 충실도 100%.

목업은 정수 도트 격자가 없는 자유 해상도 일러스트다(검증 완료: 양자화 후에도 1px 런 60%).
따라서 '격자 복원'이 아니라 **JPEG 노이즈 제거 + 팔레트 정리**가 핵심이다.

  크롭 → 색 양자화(팔레트 상한) → 알파 처리 → 목표 규격

프레임(9-slice)은 목업에 내용물이 들어 있으므로 **테두리만 취하고 중앙을 재구성**한다.
그래야 늘려도 안 깨지고 그 안에 콘텐츠를 얹을 수 있다.
"""
import io, os, json
from PIL import Image
import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
MOCK = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Reference', 'Mockups')


def quantize(im, colors=16):
    """JPEG 노이즈 제거. 디더링 없이 — 디더는 도트를 지저분하게 만든다."""
    return im.convert('RGB').quantize(colors=colors, method=Image.MEDIANCUT,
                                      dither=Image.NONE).convert('RGB')


def crop(mockup, box):
    im = Image.open(os.path.join(MOCK, mockup)).convert('RGB')
    return im.crop((box[0], box[1], box[0] + box[2], box[1] + box[3]))


def make_frame(src, border, target, colors=16):
    """9-slice 프레임 복원 — 목업 테두리를 살리고 중앙은 내부 배경색으로 채운다.

    border = (L, R, T, B). 중앙은 테두리 안쪽에서 가장 흔한 색으로 균일 채움.
    (질감을 남기면 늘렸을 때 반복 무늬가 드러난다)
    """
    q = quantize(src, colors)
    a = np.array(q)
    L, R, T, B = border
    inner = a[T + 4:-(B + 4) or None, L + 4:-(R + 4) or None].reshape(-1, 3)
    if len(inner) == 0:
        inner = a.reshape(-1, 3)
    cols, cnt = np.unique(inner, axis=0, return_counts=True)
    fill = cols[cnt.argmax()]

    tw, th = target
    out = np.tile(fill, (th, tw, 1)).astype(np.uint8)
    sh, sw = a.shape[:2]
    out[:T, :L] = a[:T, :L]                                   # 좌상
    out[:T, tw - R:] = a[:T, sw - R:]                         # 우상
    out[th - B:, :L] = a[sh - B:, :L]                         # 좌하
    out[th - B:, tw - R:] = a[sh - B:, sw - R:]               # 우하
    mid_src_w = sw - L - R
    mid_dst_w = tw - L - R
    if mid_dst_w > 0 and mid_src_w > 0:
        top = Image.fromarray(a[:T, L:sw - R]).resize((mid_dst_w, T), Image.NEAREST)
        bot = Image.fromarray(a[sh - B:, L:sw - R]).resize((mid_dst_w, B), Image.NEAREST)
        out[:T, L:tw - R] = np.array(top)
        out[th - B:, L:tw - R] = np.array(bot)
    mid_src_h = sh - T - B
    mid_dst_h = th - T - B
    if mid_dst_h > 0 and mid_src_h > 0:
        lef = Image.fromarray(a[T:sh - B, :L]).resize((L, mid_dst_h), Image.NEAREST)
        rig = Image.fromarray(a[T:sh - B, sw - R:]).resize((R, mid_dst_h), Image.NEAREST)
        out[T:th - B, :L] = np.array(lef)
        out[T:th - B, tw - R:] = np.array(rig)
    return Image.fromarray(out)


def make_art(src, target, colors=16, bg_tol=40):
    """일러스트 복원 — 어두운 배경을 알파로 빼고 목표 규격에 맞춘다."""
    q = quantize(src, colors)
    if q.size != target:
        q = q.resize(target, Image.NEAREST)
    a = np.array(q).astype(int)
    # 모서리 4곳 평균색을 배경으로 간주
    h, w = a.shape[:2]
    corners = np.vstack([a[:4, :4].reshape(-1, 3), a[:4, -4:].reshape(-1, 3),
                         a[-4:, :4].reshape(-1, 3), a[-4:, -4:].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    dist = np.abs(a - bg).sum(axis=2)
    alpha = np.where(dist < bg_tol, 0, 255).astype(np.uint8)
    rgba = np.dstack([a.astype(np.uint8), alpha])
    return Image.fromarray(rgba, 'RGBA')
