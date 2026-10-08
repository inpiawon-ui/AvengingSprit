"""팝업 · 클리어 연출 — 단일 출처 표 + 게임 합성 미리보기 (2026-10-08).

PD 「시안(mock_fxstory_*)대로 가자」. 부품은 시안에서 잘라 발주한 것(out/fxs/{화면}/fx_{화면}_{부품}_n.png).
층(layer) 값은 이 파일 한 곳 — `python fx_story.py export` 가 popup_fx.json 으로 내보내고
유니티 `InGamePopupCLayout` 이 창마다 PopupFxSpec 으로 굽는다.

시간: open 층은 창이 열린 뒤, accept 층은 고른 순간 뒤. t1 <= 0 은 창이 닫힐 때까지.
좌표: 720x1280 화면(패널 왼쪽 위 기준). 미리보기 GIF = 등장~대기 3.8초 + 고른 뒤 1.0초.
python fx_story.py {화면,..|all} → _exchange/ref/fx_story/{화면}.gif · {화면}_vs_mock.png
python fx_story.py export        → Tools/popup_fx.json
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import fx_preview as P  # noqa: E402

OUT = os.path.join(P.EX, 'ref', 'fx_story')
FXS = os.path.join(P.EX, 'out', 'fxs')
FPS = 25
T_ACC, T_END = 3.8, 5.0
T_CLOSE = T_ACC + 0.35          # 게임: 고른 뒤 0.35초 연출을 보여 주고 창을 닫는다(PickFxHoldSeconds)
PREVIEW_AVATAR = (112, 292)      # 미리보기에서 플레이어 몸 자리(게임은 실제 몸 위치)


def L(frames, x, y, w, h, step=0.12, t0=0.0, t1=0.0, loop=True, add=True, tint=(255, 255, 255, 255), alpha=1.0,
      flip=False, fade=0.15, phase='open', back=False, beam=False, x2=0.0, y2=0.0, fromAvatar=False, atNode='',
      untilAccept=False, inNode=False):
    # inNode — atNode 칸 **안**, 칸 그림 바로 위 · 칸 안 글자 아래에 그린다. 칸에 붙는 빛이 글자를 못 덮게(PD 10-08 「게임 정보는 가리면 안 돼」)
    tint = list(tint) if len(tint) == 4 else list(tint) + [255]
    return dict(frames=frames, x=x, y=y, w=w, h=h, step=step, t0=t0, t1=t1, loop=loop, additive=add, tint=tint,
                alpha=alpha, flip=flip, fade=fade, phase=phase, back=back, beam=beam, x2=x2, y2=y2,
                fromAvatar=fromAvatar, atNode=atNode, untilAccept=untilAccept, inNode=inNode)


# PD 2026-10-08 「팝업이 닫힐 때 캐릭터에 능력치가 적용된 듯한 이펙트 — 이벤트마다 색이 다르게」
POWER_COLOR = {'levelup': (255, 200, 61), 'altar': (66, 234, 242), 'devil': (255, 75, 75), 'shop': (255, 174, 32)}


RARITY_TINT = {'rare': (92, 190, 255), 'common': (232, 238, 230)}   # 코덱스 levelup2 — 카드 희귀도 순환광 색


def powerup(key):
    """창이 닫힌 뒤(고른 뒤 0.35초) 플레이어 몸에 깃드는 빛 — 공통 그림 하나에 창 대표색을 곱한다.
    크기는 네 창이 같다(코덱스 altar1 · shop1: 몸보다 크면 소환 이펙트로 읽힌다). 덮기는 0.4초 뒤 거의 안 보여서 가산."""
    return L('ui_powerup', 0, -10, 180, 220, 0.07, 0.35, 0.95, loop=False, tint=POWER_COLOR[key], alpha=0.95,
             fade=0.06, phase='accept', fromAvatar=True)


# ── 세로로 늘린 창(popup_c_layout.TALL · make_tall_frames.INSERTS) ─────────────
# 아래 층 값은 **늘리기 전 틀**을 기준으로 쓴다(시안을 그 틀 위에서 쟀다). 표를 만든 뒤 글자 배치와 같은 규칙으로 옮긴다.
# 칸에 붙는 층(atNode) · 몸에 붙는 층(fromAvatar)은 그 자리를 따라가므로 손대지 않는다.
# 키 60 이상(관 · 칸 테두리 · 훑기)은 걸친 칸만큼 같이 늘고, 작은 것(보석 · 불씨)은 자리만 옮긴다.
TALL_SCREENS = {'devil': ('EventBox', 'eventframe'), 'altar': ('ShrineBox', 'shrineframe'), 'shop': ('ShopBox', 'shopframe')}


def apply_tall(key, spec):
    if key not in TALL_SCREENS:
        return spec
    from make_tall_frames import remap
    box, frame = TALL_SCREENS[key]
    _, top0, _, h0 = P.rect(box, True)
    top1 = P.rect(box)[1]

    def mp(y):
        # 틀 위 · 아래로 삐져나간 장식(물방울 · 등불 · 연기)도 창과 같이 움직인다 — 위는 창이 올라간 만큼, 아래는 늘어난 만큼
        r = y - top0
        if r < 0:
            return y + top1 - top0
        if r > h0:
            return top1 + remap(frame, h0) + (r - h0)
        return top1 + remap(frame, r)

    for l in spec['layers']:
        if l['atNode'] or l['fromAvatar']:
            continue
        if l['beam']:
            # 빛줄기 끝이 틀 밖이면 HUD(골드 · 체력) 자리다 — 화면에 고정
            l['y'] = mp(l['y'])
            if 0 <= l['y2'] - top0 <= h0:
                l['y2'] = mp(l['y2'])
            continue
        if l['frames'] in KEEP_SIZE:
            l['y'] = mp(l['y'])   # 알약 · 버튼 가운데를 그대로 따라간다
            continue
        a, b = mp(l['y'] - l['h'] / 2), mp(l['y'] + l['h'] / 2)
        l['y'] = (a + b) / 2   # 걸친 칸이 늘었으면 늘어난 칸의 가운데로
        if l['h'] >= 60 and l['frames'] not in KEEP_SIZE:
            l['h'] = b - a
    return spec


# 알약 · 버튼에 붙는 장식 — 위 본문 칸에 걸쳐 있어도 늘리지 않는다(알약 · 버튼 크기는 그대로다)
KEEP_SIZE = {'devil_reward_burst', 'devil_pill_sparks', 'devil_button_glow'}


def cen(r):
    return r[0] + r[2] / 2, r[1] + r[3] / 2


# ── 화면별 층 ─────────────────────────────────────────────
def layers_devil():
    rw, cs, acc = P.rect('EventRewardPill', True), P.rect('EventCostPill', True), P.rect('EventAcceptButton', True)
    return {'panel': 'EventPanel', 'layers': [
        # 1 등장 — 창 아래 바닥에서 진홍 연기(검수 review_fx_story_devil1 · 2)
        L('devil_smoke', 360, 930, 620, 300, 0.12, 0.0, 0.6, loop=False, add=False, alpha=0.48, fade=0.10, back=True),
        # 2 떠 있는 동안
        L('devil_heat', 360, 890, 610, 210, 0.12, 0.3, alpha=0.24, fade=0.18, back=True),
        L('devil_chain_glow', 52, 732, 132, 300, 0.12, 0.15, alpha=0.58, fade=0.12),
        L('devil_chain_glow', 668, 732, 132, 300, 0.12, 0.15, alpha=0.58, fade=0.12, flip=True),
        L('devil_flame', 360, 405, 38, 76, 0.10, 0.25, alpha=0.82, fade=0.12),
        # 알약 반짝이 · 수락 버튼 빛 · 수락 반응(혼 줄기 · 사슬 · 보상 판 터짐 · 몸 빛)은 뺐다 — PD 10-08 「싼마이틱하게 하지 마」.
        # 수락하면 창을 닫고 캐릭터에 저주 연출(InGameMainUI.Rewards AcceptDevilAsync)
    ]}


def layers_levelup():
    # 검수 review_fx_story_levelup1.md 반영. 대기 장식은 고르면 거둔다(untilAccept) — 미리보기의 t1=3.8 과 같은 뜻.
    fr = P.rect('BuffFrame')
    em = (fr[0] + 345, fr[1] + 75)
    cards = [P.rect('BuffCard%d' % i) for i in range(3)]
    lay = [
        L('levelup_emblem_strike', 360, 226, 110, 250, 0.07, 0.0, 0.35, loop=False, alpha=0.90, fade=0.04),
        L('levelup_emblem_rays', *em, 360, 360, 0.10, 0.40, alpha=0.72, back=True, untilAccept=True),
        L('levelup_tube_glow', 61, 435, 54, 220, 0.10, 0.08, alpha=0.62, untilAccept=True),
        L('levelup_tube_glow', 659, 435, 54, 220, 0.10, 0.12, alpha=0.62, flip=True, untilAccept=True),
        L('levelup_pipe_flow', 205, 310, 230, 34, 0.08, 0.12, 0.90, loop=False, alpha=0.78),
        L('levelup_pipe_flow', 515, 310, 230, 34, 0.08, 0.16, 0.94, loop=False, alpha=0.78, flip=True),
    ]
    # 카드 테두리 · 상승광은 카드 희귀도 색(시안: RARE 청 · COMMON 백) — 게임은 InGameMainUI 가 카드마다
    # PopupFxPlayer.Tint(패널, 'BuffCard{i}', 희귀도색) 로 다시 칠한다. 표의 색은 미리보기 카드(RARE · COMMON · RARE)용.
    # 카드 테두리 — 아주 가는 빛 토막이 천천히 한 바퀴(PD 10-08 「은은하게 · 얇게 · 천천히」). 상승광 · 고른 순간 빛줄기 · 몸 빛은 뺐다 —
    # 고르면 그 카드가 커지며 가운데로 오고(포커스) 창을 닫은 뒤 캐릭터에 카드 힘이 깃든다(InGameMainUI.Rewards PickCardAsync)
    for i, c in enumerate(cards):
        tint = RARITY_TINT['common' if i == 1 else 'rare']
        lay.append(L('present_card_rim_thin', 0, 0, 176, 336, 0.32, 0.5 + 0.1 * i, alpha=0.55, tint=tint,
                     atNode='BuffCard%d' % i, untilAccept=True, inNode=True))
    return {'panel': 'BuffChoicePanel', 'layers': lay, 'preview_slot': '0'}


def layers_altar():
    # 코덱스 1회차(review_fx_story_altar1.md) 반영 — 글자를 덮던 섬광은 덮기 · 낮은 알파로, 대기 하강 소울 · 기포 추가
    fr = P.rect('ShrineBox', True)
    sym = (fr[0] + 295, fr[1] + 38)
    slots = [P.rect('ShrineChoice%d' % i, True) for i in range(3)]
    lay = [
        # 등장 때 가운데로 떨어지던 물방울(altar_drop_ripple) · 칸 훑기(altar_sweep)는 뺐다 — PD 10-08 「쌩뚱맞게 위치도 안 맞는다, 없애」
        L('altar_tube_fill', fr[0] + 43, fr[1] + 275, 38, 300, 0.12, 0.16, alpha=0.5),
        L('altar_tube_fill', fr[0] + 547, fr[1] + 275, 38, 300, 0.12, 0.22, alpha=0.5, flip=True),
        # 가운데 낙하 물줄기(altar_idle_fall)는 뺐다 — 세 칸의 이름 · 설명 위를 지나가 글자를 가렸다(PD 10-08 「게임 정보는 가리면 안 돼」)
        L('altar_idle_bubbles', fr[0] + 43, fr[1] + 290, 56, 300, 0.16, 0.6, alpha=0.7),
        L('altar_idle_bubbles', fr[0] + 547, fr[1] + 290, 56, 300, 0.16, 0.78, alpha=0.7, flip=True),
    ]
    for i, s in enumerate(slots):
        # 칸 오른쪽 보석만 은은히 숨쉰다(칸 안 · 글자 아래). 보석 = 칸 그림 440 폭 중 x 385 → 407 폭 칸 가운데에서 +152
        lay.append(L('altar_gem_pulse', 152, 0, 52, 52, 0.28, 0.7 + 0.22 * i,
                     alpha=0.45, fade=0.3, atNode='ShrineChoice%d' % i, inNode=True))
    # 고른 순간 빛 · 치유 줄기 · 몸 빛은 뺐다 — 고른 칸에서 회복 구슬이 실제로 고스트 몸까지 날아가고(InGameMainUI.Rewards PickShrineAsync)
    # 닿는 순간 HP 바가 차며 몸에 회복 연출이 뜬다
    return {'panel': 'ShrinePanel', 'layers': lay, 'preview_slot': '1'}


def layers_shop():
    # 코덱스 1회차(review_fx_story_shop1.md) 반영 — 등불 → 관 → 배관 순서, 칸 테두리 호흡, 구매는 금화 · 아이콘 두 갈래
    fr = P.rect('ShopBox', True)
    sym = (fr[0] + 345, fr[1] + 40)
    cells = [P.rect('ShopItem%d' % i, True) for i in range(6)]
    lay = [
        # 열 때 번쩍이던 등불(shop_lamp_glow) · 칸마다 훑는 빛 · 모든 칸 금테는 뺐다 — PD 10-08 「다 빛나면 뭐가 좋은 건데」
        L('shop_tube_ghost', fr[0] + 31, fr[1] + 300, 44, 300, 0.14, 0.16, alpha=0.68, untilAccept=True),
        L('shop_tube_ghost', fr[0] + 659, fr[1] + 300, 44, 300, 0.14, 0.22, alpha=0.68, flip=True, untilAccept=True),
        L('shop_pipe_flow', fr[0] + 190, fr[1] + 68, 200, 36, 0.10, 0.22, 0.82, loop=False, alpha=0.5, back=True),
        L('shop_pipe_flow', fr[0] + 500, fr[1] + 68, 200, 36, 0.10, 0.28, 0.88, loop=False, alpha=0.5, back=True,
          flip=True),
    ]
    # 구매 반응(칸 금화 튐 · 금화 줄 · 원호 아이콘 · 몸 빛)은 뺐다 — HUD 골드 → 산 칸 → 몸 순서로 InGameMainUI.Rewards BuyAsync 가 보여 준다
    return {'panel': 'ShopPanel', 'layers': lay, 'preview_slot': '0'}


# 결과창 좌표 — ChapterScreensBuilder 가 화면 가운데 기준으로 세운 자리를 화면 좌표로
CLEAR = dict(frame=(30, 220, 660, 800), title=(130, 339, 460, 58), gold=(130, 508, 460, 140),
             chest=(130, 656, 460, 140), warn=(140, 809, 440, 46), ok=(224, 897, 272, 76), emblem=(360, 270))


def layers_clear():
    # 코덱스 1회차(review_fx_story_clear1.md) 반영. 단 제목색은 글자 검수 3회차(흰색 유지)를 따르고,
    # 「능력치 적용」 빛은 능력치를 얻는 창(레벨업 · 제단 · 악마 · 상점)에만 — 클리어는 보상 창이라 넣지 않는다.
    return {'panel': 'ChapterResultPopup', 'layers': [
        # 창 뒤 금빛 폭발(clear_title_burst)은 뺐다 — PD 10-08 「과하다」. 등장은 창이 OutBack 곡선으로 커지는 것 하나로 충분
        # 창 가장자리 전기 · 양옆 원혼 입자는 뺐다 — PD 10-08 「아웃라인에 이펙트를 과하게, 그냥 이미지 같다」
        # 금화는 금화 더미 칸에 붙는다(줄이 내려가도 따라감) — 위에서 쏟아져 더미 위에 쌓인다(PD 10-08 「따로 논다」).
        # 숫자 0 → 금액 · 더미 튐은 ChapterResultPopup.CountGoldAsync 가 같은 1.55 ~ 2.35 초에 맞춘다
        # 금화 더미 칸 안(같은 줄 「골드 획득」 · 금액 글자보다 아래 층). 폭은 더미 칸(150) 안으로
        L('clear_coin_rain', 0, -30, 160, 160, 0.2, 1.55, 2.4, loop=False, alpha=1.0, fade=0.1, add=False,
          atNode='ResultGoldIcon', inNode=True),
        # 상자가 위에서 떨어져 바닥에 닿는 순간(ChapterResultPopup.ChestDropAsync 의 착지 2.95초)에 먼지 고리
        L('clear_chest_land', -8, 31, 430, 120, 0.12, 2.95, 3.6, loop=False, alpha=0.72, fade=0.08,
          atNode='ResultChestArt', inNode=True),
        # 시안 3컷 「끝」= 누르기를 기다리는 대기 — 테두리만 은은히, 누르면 짧게 한 번 더
        L('clear_ok_glow', 0, 0, 304, 96, 0.16, 2.4, alpha=0.22, fade=0.3, atNode='ResultOkButton', inNode=True),
        L('clear_ok_glow', 0, 0, 304, 96, 0.09, 0.0, 0.35, loop=False, alpha=0.32, fade=0.06, phase='accept',
          atNode='ResultOkButton', inNode=True),
    ]}


# ── 방 오브젝트 — 좌표는 물건 가운데 기준(아래로 +). 물건 상자 236x300(RoomProp.cs), 발밑 = +150 ──
# 단계: open = 대기(방에 들어오면) · near = 플레이어가 다가옴 · accept = 작동. fromAvatar(빔 아님) = 플레이어 몸 자리.
def layers_room_heal():
    # 코덱스 1회차(review_fx_story_room_heal1.md) 반영. 기둥 t1 은 0 — 게임은 닿는 순간 near 를 끄므로 미리보기와 같다.
    return {'panel': '', 'room': 'heal', 'layers': [
        L('room_heal_ring_ripple', 0, 140, 250, 96, 0.18, 0.0, add=False, alpha=0.42, fade=0.18, back=True),
        L('room_heal_capsule_glow', 0, -8, 138, 238, 0.16, 0.0, add=False, alpha=0.38, fade=0.18),
        L('room_heal_steam', -12, -174, 126, 150, 0.16, 0.2, add=False, alpha=0.42, fade=0.18),
        L('room_heal_pillar', 0, -158, 124, 300, 0.12, 0.0, alpha=0.70, fade=0.08, phase='near'),
        # 닿았을 때 아래로 떨어지던 빛줄기(room_heal_absorb)는 뺐다 — PD 10-08 「생뚱맞게 밑에 한 줄기, 없애」. 닿으면 창이 열린다
    ]}


def layers_room_devil():
    # 코덱스 1회차(review_fx_story_room_devil1.md) 반영 — 대기는 뿔 불씨 · 좁은 일렁임, 다가오면 눈 점멸, 닿으면 사슬 해방.
    # 눈 점멸 t1 은 0 — 게임은 닿는 순간 near 를 끈다.
    return {'panel': '', 'room': 'devil', 'layers': [
        L('room_devil_fire_smoke', 0, -92, 270, 270, 0.18, 0.0, add=False, alpha=0.30, fade=0.18, back=True),
        L('room_devil_core_glow', 0, -2, 104, 208, 0.16, 0.0, alpha=0.48, fade=0.18),
        L('room_devil_horn_fire', -80, -146, 34, 68, 0.14, 0.10, alpha=0.66, fade=0.14),
        L('room_devil_horn_fire', 80, -146, 34, 68, 0.14, 0.17, alpha=0.66, fade=0.14, flip=True),
        L('room_devil_eye_flash', 0, -48, 54, 54, 0.12, 0.0, alpha=0.62, fade=0.08, phase='near'),
        L('room_devil_smoke_ring', 0, 52, 360, 150, 0.09, 0.0, 0.54, loop=False, add=False, alpha=0.48, fade=0.07,
          phase='accept', back=True),
        L('room_devil_chain_release', 0, 6, 410, 205, 0.07, 0.0, 0.42, loop=False, add=False, alpha=0.9, fade=0.04,
          phase='accept'),
    ]}


def layers_room_shop():
    # 코덱스 1회차(review_fx_story_room_shop1.md) 반영 — 혼불 · 금화는 뒤로 은은히, 등불은 차례로, 눈은 다가올 때 한 번
    lamps = [(-91, -49), (91, -49), (-91, 23), (91, 23)]
    lay = [
        # 코덱스 값(덮기 .28 · 뒤)은 물건 뒤로 숨어 대기 장식이 0 으로 보였다 — 앞 · 가산으로 두되 낮게
        L('room_shop_soul_flame', 0, 4, 184, 294, 0.16, 0.0, alpha=0.30),
        L('room_shop_coin_float', 0, -18, 390, 220, 0.18, 0.1, alpha=0.62),
    ]
    for i, (dx, dy) in enumerate(lamps):
        lay.append(L('room_shop_lantern', dx, dy, 34, 34, 0.18 + 0.02 * i, 0.18 * i, alpha=0.58))
    lay += [
        L('room_shop_eye_glow', 0, -71, 48, 24, 0.10, 0.04, 0.46, loop=False, alpha=0.78, fade=0.05, phase='near'),
        L('room_shop_coin_ring', 0, 82, 500, 210, 0.08, 0.0, 0.48, loop=False, alpha=0.58, fade=0.06,
          phase='accept'),
    ]
    return {'panel': '', 'room': 'shop', 'layers': lay}


ROOMS = {'room_heal': layers_room_heal, 'room_devil': layers_room_devil, 'room_shop': layers_room_shop}
ROOM_ART = {'room_heal': 'obj_heal_shrine', 'room_devil': 'obj_devil_altar', 'room_shop': 'obj_shop_stall'}
T_NEAR, T_USE, T_ROOM_END = 1.6, 2.8, 3.8


def render_room(key):
    spec = ROOMS[key]()
    kind = spec['room']
    spr, (px, py) = P.prop_place(kind, ROOM_ART[key])
    c = (px + spr.width / 2, py + spr.height / 2)
    ring = Image.open(os.path.join(P.ART, 'obj_ring_%s.png' % kind)).convert('RGBA').resize((230, 86), Image.LANCZOS)
    mark = Image.open(os.path.join(P.ART, 'obj_mark_%s.png' % kind)).convert('RGBA').resize((52, 52), Image.LANCZOS)
    # 닿으면 열리는 창 — 게임처럼 방 연출 위에 덮는다(코덱스 room_*1: 창이 없으면 글자 가림을 판정할 수 없다)
    box = {'heal': ('ShrinePanel', 'ShrineBox'), 'devil': ('EventPanel', 'EventBox'), 'shop': ('ShopPanel', 'ShopBox')}[kind]
    pr = P.rect(box[1])
    popup = Image.open(os.path.join(P.EX, 'ref', 'popup_c_type_ko_%s.png' % box[0])).convert('RGBA')
    popup = popup.crop((pr[0], pr[1], pr[0] + pr[2], pr[1] + pr[3]))
    spr_used = spr.copy()
    spr_used.paste(Image.eval(spr.convert('RGB'), lambda v: int(v * 0.45)), (0, 0), spr.getchannel('A'))   # 게임: color (.45,.45,.5,1)
    g_r = (c[0] - 45, py + spr.height + 70, 90, 100)
    base = P.empty_room_bg(kind, (px, py, spr.width, spr.height), g_r)
    ghost = P.sub(P.cap('room_' + kind), g_r)
    avatar0 = (c[0], g_r[1] + 50)
    bg = np.asarray(base.convert('RGB')).astype(np.float32)
    out = []

    def shifted(l, origin, avatar):
        m = dict(l)
        m['x2'], m['y2'] = origin[0] + l['x2'], origin[1] + l['y2']
        if l['fromAvatar']:
            # 방에서는 플레이어 몸이 움직인다 — 팝업 미리보기 자리(PREVIEW_AVATAR)를 쓰지 않는다
            m['x'], m['y'] = (avatar if l['beam'] else (avatar[0] + l['x'], avatar[1] + l['y']))
            m['fromAvatar'] = False
        else:
            m['x'], m['y'] = origin[0] + l['x'], origin[1] + l['y']
        return m

    for i in range(int(T_ROOM_END * FPS)):
        t = i / FPS
        k = min(1, max(0, (t - (T_NEAR - 0.5)) / 0.5))
        avatar = (avatar0[0], avatar0[1] - 130 * k)
        used = t >= T_USE
        b = bg.copy()
        for l in spec['layers']:
            if l['back'] and l['phase'] == 'open' and not used:
                draw_layer(b, shifted(l, c, avatar), t, None)
            if l['back'] and l['phase'] == 'accept' and used:
                draw_layer(b, shifted(l, c, avatar), t - T_USE, None)
        P.over(b, ring, (c[0] - 115, py + spr.height - 57), 0.3 if used else 1.0)
        P.over(b, spr_used if used else spr, (px, py))   # 다 쓴 물건은 어둡게(투명 아님 — 코덱스 room_heal1)
        if not used:
            P.over(b, mark, (c[0] - 26, py - 64 + 3 * math.sin(t / 1.4 * 2 * math.pi)))
        for l in spec['layers']:
            if l['back']:
                continue
            if l['phase'] == 'open' and not used:
                draw_layer(b, shifted(l, c, avatar), t, None)
            elif l['phase'] == 'near' and T_NEAR <= t < T_USE:
                draw_layer(b, shifted(l, c, avatar), t - T_NEAR, None)
            elif l['phase'] == 'accept' and used:
                draw_layer(b, shifted(l, c, avatar), t - T_USE, None)
        P.over(b, ghost, (avatar[0] - 45, avatar[1] - 50))
        if used:
            # 게임: 닿은 뒤 0.35초 작동 연출을 보여 주고 창을 연다(RoomPropUseHoldSeconds) — 바로 열면 창에 다 가려진다
            sc, al = P.ease_pop(t - T_USE, 0.35, 0.16, 0.94)
            if al > 0:
                pw, ph = max(1, int(pr[2] * sc)), max(1, int(pr[3] * sc))
                P.over(b, popup.resize((pw, ph), Image.LANCZOS),
                       (int(pr[0] + (pr[2] - pw) / 2), int(pr[1] + (pr[3] - ph) / 2)), al)
        out.append(Image.fromarray(b.clip(0, 255).astype(np.uint8), 'RGB'))
    os.makedirs(OUT, exist_ok=True)
    small = [f.resize((360, 640), Image.LANCZOS).quantize(colors=220, method=Image.Quantize.MEDIANCUT,
                                                          dither=Image.Dither.NONE) for f in out]
    path = os.path.join(OUT, key + '.gif')
    small[0].save(path, save_all=True, append_images=small[1:], duration=int(1000 / FPS), loop=0, optimize=True)
    mock = Image.open(os.path.join(P.EX, 'in', 'mock_fxstory_%s_v1.png' % key)).convert('RGB').resize((2160, 1280))
    # 4컷째 = 창이 열린 뒤(닿고 0.7초) — 방 연출이 창 글자 · 버튼을 가리지 않는지
    picks = [int(1.0 * FPS), int((T_NEAR + 0.4) * FPS), int((T_USE + 0.25) * FPS), int((T_USE + 0.7) * FPS)]
    sheet = Image.new('RGB', (2880, 2560), (20, 20, 24))
    sheet.paste(mock, (0, 0))
    for j, kk in enumerate(picks):
        sheet.paste(out[kk], (j * 720, 1280))
    sheet.resize((1440, 1280)).save(os.path.join(OUT, key + '_vs_mock.png'))
    print(key, len(out), 'frames', os.path.getsize(path) // 1024, 'KB')


ROOM_CS = os.path.join(P.ROOT, 'Assets', 'Scripts', 'Module', 'InGame', 'RoomPropFxTable.cs')


def gen_room_table(rooms, path=None):
    """방 오브젝트 층을 C# 표로 — 방 물건은 프리팹 밖(런타임 생성)이라 굽지 못한다. 생성 파일은 손으로 고치지 않는다."""
    def f(v):
        return ('%.3ff' % v).replace('.000f', 'f')

    def b(v):
        return 'true' if v else 'false'

    lines = ['// ⚠ 자동 생성 — Projects/AVSR/Tools/fx_story.py export 가 만든다. 손으로 고치지 말 것(값은 fx_story.py 에서).',
             'using System.Collections.Generic;', 'using UnityEngine;', '', 'namespace Game.Module.InGame', '{',
             '    /// <summary>방 오브젝트(회복 · 악마 · 상점) 연출 층 — 연출 시안 mock_fxstory_room_*_v1 (2026-10-08).</summary>',
             '    public static class RoomPropFxTable', '    {',
             '        public static IReadOnlyList<PopupFxLayer> Get(string kind) => kind switch', '        {']
    names = {k: ''.join(w.title() for w in k.split('_')) for k in rooms}
    for key, spec in rooms.items():
        lines.append('            "%s" => %s,' % (spec['room'], names[key]))
    lines += ['            _ => null,', '        };', '']
    for key, spec in rooms.items():
        lines.append('        private static readonly PopupFxLayer[] %s =' % names[key])
        lines.append('        {')
        for l in spec['layers']:
            t = l['tint']
            lines.append(
                '            new() { Frames = "%s", Phase = "%s", Back = %s, X = %s, Y = %s, W = %s, H = %s, Step = %s,'
                % (l['frames'], l['phase'], b(l['back']), f(l['x']), f(l['y']), f(l['w']), f(l['h']), f(l['step'])))
            lines.append(
                '                    T0 = %s, T1 = %s, Loop = %s, Additive = %s, Tint = new Color(%s, %s, %s, %s), Alpha = %s,'
                % (f(l['t0']), f(l['t1']), b(l['loop']), b(l['additive']), f(t[0] / 255), f(t[1] / 255), f(t[2] / 255),
                   f(t[3] / 255), f(l['alpha'])))
            lines.append('                    Flip = %s, Fade = %s, FromAvatar = %s },' % (b(l['flip']), f(l['fade']),
                                                                                          b(l['fromAvatar'])))
        lines += ['        };', '']
    lines = lines[:-1] + ['    }', '}', '']
    # 유니티가 빌 때 Assets/Scripts/Module/InGame/RoomPropFxTable.cs 로 복사해 설치한다(작업 중 컴파일을 일으키지 않게)
    path = path or os.path.join(HERE, 'RoomPropFxTable.cs.gen')
    with open(path, 'w', encoding='utf-8-sig') as fp:
        fp.write('\n'.join(lines))
    print('room table', path)


SCREENS = {'devil': layers_devil, 'levelup': layers_levelup, 'altar': layers_altar, 'shop': layers_shop,
           'clear': layers_clear}
MOCKS = {'clear': 'mock_fxstory_clear_v2.png'}
CAPS = {'devil': ('devil', 'eventframe', 'EventBox'), 'levelup': ('levelup', 'levelupframe', 'BuffFrame'),
        'altar': ('audit_altar_Korean', 'shrineframe', 'ShrineBox'), 'shop': ('audit_shop_Korean', 'shopframe', 'ShopBox')}

# ── 미리보기 렌더 ──────────────────────────────────────────
_cache = {}
GHOST = Image.open(os.path.join(P.ROOT, 'Assets', 'BaseResource', 'Unit', 'ghost', 'unit_ghost_e.png')).convert('RGBA').resize(
    (96, 96), Image.NEAREST)


def frames(name):
    if name not in _cache:
        screen = '_'.join(name.split('_')[:2]) if name.startswith('room_') else name.split('_')[0]
        folder = os.path.join(P.EX, 'out', 'fxui') if screen == 'ui' else os.path.join(FXS, screen)
        fs, i = [], 1
        while True:
            p = os.path.join(folder, 'fx_%s_%d.png' % (name, i))
            if not os.path.exists(p):
                break
            fs.append(Image.open(p).convert('RGBA'))
            i += 1
        _cache[name] = fs
    return _cache[name]


def node_center(name):
    try:
        return cen(P.rect(name))
    except KeyError:
        return None


def draw_layer(base, l, t, slot):
    f = frames(l['frames'])
    if not f or t < l['t0'] or (l['t1'] > 0 and t >= l['t1']):
        return
    k = int((t - l['t0']) / max(0.01, l['step']))
    if k >= len(f):
        if not l['loop']:
            return
        k %= len(f)
    a = l['alpha']
    if l['fade'] > 0:
        a *= min(1, (t - l['t0']) / l['fade'])
        if l['t1'] > 0:
            a *= min(1, (l['t1'] - t) / l['fade'])
    if a <= 0:
        return
    im = f[k]
    tint = l['tint']
    x, y = l['x'], l['y']
    if l['atNode']:
        c = node_center(l['atNode'].replace('{slot}', slot or '0'))
        if c:
            x, y = c[0] + l['x'], c[1] + l['y']
    if l['fromAvatar'] and not l['beam']:
        x, y = PREVIEW_AVATAR[0] + l['x'], PREVIEW_AVATAR[1] + l['y']
    if l['beam']:
        a0 = PREVIEW_AVATAR if l['fromAvatar'] else (x, y)
        grow = min(1, (t - l['t0']) / 0.15)
        bx, by = a0[0] + (l['x2'] - a0[0]) * grow, a0[1] + (l['y2'] - a0[1]) * grow
        length = int(math.hypot(bx - a0[0], by - a0[1]))
        if length < 8:
            return
        im = P.tint(im.resize((length, int(l['h'])), Image.LANCZOS), tint[:3], a * tint[3] / 255)
        ang = math.degrees(math.atan2(-(by - a0[1]), bx - a0[0]))
        im = im.rotate(ang, expand=True, resample=Image.BICUBIC)
        at = ((a0[0] + bx) / 2 - im.width / 2, (a0[1] + by) / 2 - im.height / 2)
    else:
        if l['flip']:
            im = ImageOps.mirror(im)
        im = P.tint(im.resize((max(1, int(l['w'])), max(1, int(l['h']))), Image.LANCZOS), tint[:3], a * tint[3] / 255)
        at = (x - l['w'] / 2, y - l['h'] / 2)
    (P.add if l['additive'] else P.over)(base, im, at)


def clear_panel():
    """결과창 미리보기 — 새 C 틀(납품되면) · 없으면 시안 컷3 를 잘라 쓴다."""
    fr = CLEAR['frame']
    src = os.path.join(P.EX, 'ref', 'popup_c_type_ko_ChapterResultPopup.png')
    if os.path.exists(src):
        full = Image.open(src).convert('RGBA')
        m = Image.open(os.path.join(P.EX, 'out', 'setc', 'result_frame.png')).convert('RGBA').getchannel('A')
        crop = full.crop((fr[0], fr[1], fr[0] + fr[2], fr[1] + fr[3]))
        crop.putalpha(m.resize((fr[2], fr[3])))
        return crop, fr[:2]
    mock = Image.open(os.path.join(P.EX, 'in', 'mock_fxstory_clear_v2.png')).convert('RGB').resize((2160, 1280))
    cut = mock.crop((1440, 0, 2160, 1280))
    return cut.crop((fr[0], fr[1], fr[0] + fr[2], fr[1] + fr[3])).convert('RGBA'), fr[:2]


def panel_image(key, spec):
    if key == 'clear':
        return clear_panel()
    cap_name, frame_sprite, box = CAPS[key]
    cap = P.cap(cap_name)
    r = P.rect(box)
    full = Image.new('RGBA', (720, 1280), (0, 0, 0, 0))
    full.alpha_composite(P.masked(cap, frame_sprite, r), r[:2])
    # 창 틀 밖으로 나온 칸(레벨업 카드 · 상점 칸 등)도 캡처에서 그대로
    for n in P.LAYOUT.values():
        if n['panel'] == spec['panel'] and n['parent'] is None and (n['sprite'] or n['sliced']) and n['name'] != box:
            rr = P.rect(n['name'])
            full.alpha_composite(P.sub(cap, rr), rr[:2])
    return full, (0, 0)


def render(key):
    spec = apply_tall(key, SCREENS[key]())
    slot = spec.get('preview_slot')
    panel, at = panel_image(key, spec)
    bg = np.asarray(P.empty_bg().convert('RGB')).astype(np.float32)
    out = []
    for i in range(int(T_END * FPS)):
        t = i / FPS
        b = bg * (1 - 0.5 * min(1, t / 0.2))
        for l in spec['layers']:
            if l['phase'] == 'open' and l['back']:
                if (l['untilAccept'] and t >= T_ACC + 0.3) or t >= T_CLOSE + 0.11:
                    continue
                draw_layer(b, l, t, slot)
            elif l['phase'] == 'accept' and l['back'] and t >= T_ACC:
                draw_layer(b, l, t - T_ACC, slot)
        s, a = P.ease_pop(t, 0, 0.16, 0.94)
        if t >= T_CLOSE:
            k = min(1, (t - T_CLOSE) / 0.11)
            s, a = 1 - 0.06 * k, 1 - k
        if a > 0:
            im = panel if s == 1 else panel.resize((int(panel.width * s), int(panel.height * s)), Image.BILINEAR)
            P.over(b, im, (at[0] + (panel.width - im.width) / 2, at[1] + (panel.height - im.height) / 2), a)
        for l in spec['layers']:
            if l['phase'] == 'open' and not l['back']:
                if (l['untilAccept'] and t >= T_ACC + 0.3) or t >= T_CLOSE + 0.11:
                    continue
                draw_layer(b, l, t, slot)
            elif l['phase'] == 'accept' and not l['back'] and t >= T_ACC:
                draw_layer(b, l, t - T_ACC, slot)
        if key != 'clear':
            P.over(b, GHOST, (PREVIEW_AVATAR[0] - 48, PREVIEW_AVATAR[1] - 48))   # 플레이어 몸(게임은 실제 몸)
        out.append(Image.fromarray(b.clip(0, 255).astype(np.uint8), 'RGB'))
    os.makedirs(OUT, exist_ok=True)
    small = [f.resize((360, 640), Image.LANCZOS).quantize(colors=220, method=Image.Quantize.MEDIANCUT,
                                                          dither=Image.Dither.NONE) for f in out]
    path = os.path.join(OUT, key + '.gif')
    small[0].save(path, save_all=True, append_images=small[1:], duration=int(1000 / FPS), loop=0, optimize=True)
    mock = Image.open(os.path.join(P.EX, 'in', MOCKS.get(key, 'mock_fxstory_%s_v1.png' % key))).convert('RGB')
    mock = mock.resize((2160, 1280))
    picks = [int(0.25 * FPS), int(2.0 * FPS), int((T_ACC + 0.25) * FPS), int((T_ACC + 0.75) * FPS)]
    sheet = Image.new('RGB', (2880, 2560), (20, 20, 24))
    sheet.paste(mock, (0, 0))
    for i, k in enumerate(picks):
        sheet.paste(out[k], (i * 720, 1280))
    sheet.resize((1440, 1280)).save(os.path.join(OUT, key + '_vs_mock.png'))
    print(key, len(out), 'frames', os.path.getsize(path) // 1024, 'KB')


def export():
    data = {k: apply_tall(k, fn()) for k, fn in SCREENS.items()}
    rooms = {k: fn() for k, fn in ROOMS.items()}
    data.update(rooms)
    gen_room_table(rooms)
    with open(os.path.join(HERE, 'popup_fx.json'), 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print('popup_fx.json', sum(len(v['layers']) for v in data.values()), 'layers')


if __name__ == '__main__':
    arg = sys.argv[1] if len(sys.argv) > 1 else 'all'
    if arg == 'export':
        export()
    else:
        keys = list(SCREENS) + list(ROOMS) if arg == 'all' else arg.split(',')
        for k in keys:
            (render_room if k in ROOMS else render)(k)
