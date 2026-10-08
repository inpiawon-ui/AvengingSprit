"""레벨업 풀세트 부품 — PD 가 고른 시안(2026-10-09 「추천대로」) 앵커 (스킬 fx-art-pipeline 5단계).

  카드 고르는 순간 = 코덱스 자유 시안 mock_lvpopup_free_peak.png (카드 둘레로 터지는 등급색 빛살 + 반짝임)
  고르기 전 머묾   = 가이드 시안 mock_lvpopup_guide_idle.png (카드 뒤 은은한 등급색 광원) + 자유 시안의 금빛 알갱이 → 셰이더 · 파티클
  몸에 깃드는 순간 = 코덱스 자유 시안 mock_lvgain_free_peak.png (카드 문양이 몸 둘레를 돌고 광원 · 빛기둥) → 카드 그림 · 셰이더 · 파티클

요소 · 무엇으로:
  카드 둘레 빛살(고르는 순간)  그림 1장(흰, 검은 바탕) — 곡선으로 펼쳐졌다 사라짐 · 등급색 곱하기   ← 여기서 받는다
  카드 뒤 광원 · 몸 광원 · 빛기둥 · 바닥 고리   셰이더(glight)
  반짝 별 · 알갱이                         파티클(결과창과 같은 입자 그림 fx_result_mote_1 · _2)
  몸 둘레를 도는 문양                       고른 카드 그림(아이콘) 그대로 — 카드마다 새로 그리지 않는다
자르기: cut_chest2_parts.py 와 같은 방식(흑백 → 투명, 2배 도트 반으로)
python order_levelup_parts.py
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

PARTS = {
    'card_rays': (520, 760, '카드를 고르는 순간 **카드 둘레에서 바깥으로 확 터지는 빛살** — 정답 시안 Projects/AVSR/_exchange/in/mock_lvpopup_free_peak.png 의 왼쪽 카드 둘레 파란 빛살 그대로. '
                  '칸 가운데에 세로로 긴 카드(324x640, 2배 크기)가 놓였다고 치고 그 테두리 바깥으로 뾰족한 빛줄기들이 사방으로 뻗는다(위아래 · 모서리 쪽이 길다). '
                  '카드 자리 안쪽은 비워도 되고 테두리 바로 바깥이 가장 밝다. 빛줄기마다 흰 심 + 부드럽게 퍼지는 광원, 작은 반짝 별 몇 개. 흰색 ~ 밝은 회색(게임에서 카드 등급색을 곱한다)'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다**(도트 모양 + 셰이더 광원 · 파티클을 섞는 게임 — 이 그림은 그중 도트 모양 층).
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) 레벨업 카드 연출 부품.
[부품] fx_levelup_{name} — {desc}
[크기] {w} x {h} 한 장. 2x2 픽셀을 한 도트로(2배 도트).
- 바탕은 **순수 검정(#000000)** — 게임에서 더하기(가산)로 그린다. 카드 그림은 그리지 않는다, 빛만.
- 빛 가장자리는 검정으로 바로 사그라든다 — 바깥에 어두운 색 판 · 윤곽선 금지. 글자 · 숫자 금지. 빛이 칸 밖으로 잘리지 않게.
[저장] Projects/AVSR/_exchange/in/lv_{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

if __name__ == '__main__':
    for name, (w, h, desc) in PARTS.items():
        with open(os.path.join(EX, 'order_lv_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(TEMPLATE.format(name=name, desc=desc, w=w, h=h))
        print('order_lv_%s.ps1' % name)


# 자르기(2026-10-09): 흑백 밝기 → 흰 그림 + 알파(밝기 하한 18), 크기 그대로 Assets/BaseResource/PopupFx/fx_levelup_card_rays_1.png
# 안쪽 구멍 실측 176x421(520x760 중) — 게임에서 카드 162x320 에 맞게 458x562 로 그린다(LevelUpFx.RaysSize)
