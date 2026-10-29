"""10-29 밤 PD 고른 시안 셋의 부품 발주 — 1-A 거치 기관포 · 3-가 호스트 해금 · 2 성장 화면 인연 판.

정답 시안: _exchange/in/mock1029_ult_mg_a.png · mock1029_host_unlock.png · mock1029_growth_inyeon.png
앵커(2배 크롭): _exchange/ref/party_ui_1029/anchor/p5_*.png
→ _exchange/order_parts1029_{ult,unlock,inyeon}.ps1 · 납품 _exchange/in/parts1029_*.png
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — PD 가 고른 시안을 **게임에 넣을 부품 그림**으로 나눠 그린다. 새로 디자인하지 말고 시안 · 앵커의 화풍 · 색 · 도트 크기를 그대로 따른다.
[공통] 크기 1024 x 1024 시트 한 장. 부품끼리 사방 40px 이상 띄운다. **글자 · 숫자를 넣지 마라.** 그림자 판 · 부품 옆 이름 메모 금지.
'''
TAIL = '''
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

ORDERS = {
    'ult': '''[정답 시안] Projects/AVSR/_exchange/in/mock1029_ult_mg_a.png (코만도 기관총 필살기 「거치 기관포」). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_ult_*.png
[바탕] **완전한 검정(#000000)** — 빛이라 더하기로 그린다(분홍 바탕 금지). 단 6) 탄피만 분홍(#FF00FF) 바탕 칸을 따로 둔다(시트 오른쪽 아래 200x200 분홍 칸 안에).
위에서 아래, 왼쪽에서 오른쪽 순서로:
1) 굵은 탄줄 한 줄 — 앵커 p5_ult_tracer 의 주황 · 노랑 빛줄기 하나(가운데 흰 심, 끝이 가늘게). 세로로 길게 약 48 x 300
2) 총구 불꽃 4장 — 앵커 p5_ult_muzzle, 위를 향해 터지는 큰 불꽃의 4단계(작게 → 크게 → 갈라짐 → 사그라짐). 각 약 150 x 150, 가로 한 줄
3) 맞은 자리 푸른 반짝 4장 — 앵커 p5_ult_hit 의 푸른 · 흰 빛과 작은 네모 알갱이(둔해진 적), 4단계. 각 약 100 x 100, 가로 한 줄
4) 바닥 흙먼지 2장 — 앵커 p5_ult_ground 의 발밑 흙먼지 덩이(밝은 갈색), 약 220 x 90
6) 탄피 한 개 — 분홍 칸 안에 놋쇠 탄피 하나(약 14 x 24)
[저장] Projects/AVSR/_exchange/in/parts1029_ult.png''',
    'unlock': '''[정답 시안] Projects/AVSR/_exchange/in/mock1029_host_unlock.png (호스트 해금 순간). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_unlock_*.png
시트를 위아래 둘로 나눈다. **위 절반 = 완전한 검정(#000000) 바탕**(빛 — 더하기로 그림), **아래 절반 = 완전한 분홍(#FF00FF) 바탕**(형태 — 뚫어 씀). 경계에 칸 선을 그리지 마라.
[위 절반 · 검정]
1) 금빛 빛살 터짐 — 앵커 p5_unlock_rays 의 가운데서 사방으로 뻗는 금빛 빛살(가운데는 비우고 둘레만). 약 440 x 440
2) 금빛 고리 — 몸 뒤 금빛 원 고리(얇은 빛 고리 + 작은 반짝). 약 300 x 300
[아래 절반 · 분홍]
3) 조각 알갱이 세 개 — 앵커 p5_unlock_shards 의 금빛 · 붉은 · 푸른 작은 결정 조각(도트 네모 결정). 각 약 44 x 44, 가로 한 줄
4) 등급 딱지 판 — 앵커 p5_unlock_badge 의 금 장식 테 + 보라 안 판(글자 「SSR」 빼고 빈 판). 약 200 x 80
5) 이름 판 — 앵커 p5_unlock_name 의 어두운 갈색 안 + 금 장식 테 판(글자 빼고). 약 320 x 80
6) 단추 두 개 — 앵커 p5_unlock_buttons 의 파란 단추 · 금빛 단추(글자 빼고). 각 약 270 x 90
[저장] Projects/AVSR/_exchange/in/parts1029_unlock.png''',
    'inyeon': '''[정답 시안] Projects/AVSR/_exchange/in/mock1029_growth_inyeon.png (호스트 성장 화면 오른쪽 인연 판). 앵커: Projects/AVSR/_exchange/ref/party_ui_1029/anchor/p5_iy_*.png
[바탕] 완전한 분홍(#FF00FF). 지금 화면(Projects/AVSR/_exchange/ref/ui_reborn_1023/play/growth_16x9.png)의 남색 판 · 청록 테 화풍 그대로.
위에서 아래, 왼쪽에서 오른쪽 순서로:
1) 인연 큰 판 — 앵커 p5_iy_panel 의 바깥 판(남색 안 · 밝은 청록 두꺼운 테 · 모서리 장식). 9분할로 늘려 쓰게 가운데 고르게. 약 380 x 440
2) 안쪽 칸 — 앵커 p5_iy_pair · p5_iy_effect 의 판 안 작은 칸(더 어두운 남색 · 가는 회청 테). 9분할. 약 330 x 170
3) 단계 딱지 — 앵커 p5_iy_header 오른쪽 금빛 둥근 딱지(글자 「1단」 빼고). 약 70 x 36
4) 사슬 고리 아이콘 — 앵커 p5_iy_header 왼쪽 금빛 사슬 고리. 약 40 x 40
5) 얼굴 칸 두 개 — 앵커 p5_iy_pair 의 왼쪽 청록 테 얼굴 칸 · 오른쪽 금 테 얼굴 칸(얼굴 빼고 칸만). 각 약 100 x 100
6) 잇는 사슬 화살 — 앵커 p5_iy_pair 가운데 청록 사슬 + 오른쪽 화살촉. 약 110 x 40
7) 켜짐 스위치 — 앵커 p5_iy_pair 아래 청록 토글(켜진 모양). 약 50 x 28
8) 작은 화살 — 앵커 p5_iy_effect 의 청록 화살. 약 36 x 24
[저장] Projects/AVSR/_exchange/in/parts1029_inyeon.png''',
}


def main():
    for k, body in ORDERS.items():
        open(os.path.join(EX, f'order_parts1029_{k}.ps1'), 'w', encoding='utf-8-sig').write(HEAD + body + TAIL)
    print('발주서', len(ORDERS))


if __name__ == '__main__':
    main()
