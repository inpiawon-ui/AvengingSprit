"""방 특별 오브젝트 3종(회복 제단 · 악마 제단 · 유령 노점) 크기 · 퀄업 시안 발주 (2026-10-08).

PD: 「맵에 하나만 뜨는 오브젝트인데 너무 작아 티가 안 난다 — 스케일 크게, 퀄업」.
디자인은 지금 그림 그대로(새 양식 금지), 크게 · 정교하게. 완성된 게임 화면 한 장씩.
python order_obj_mocks.py → _exchange/order_obj_mock_{key}.ps1 · run_obj_mocks.ps1
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

OBJS = {
    'heal_shrine': ('회복의 제단', 'obj_heal_shrine.png',
                    '청록 빛 수정이 박힌 은빛 십자 제단 + 양옆 촛불 + 둥근 돌 단',
                    '높이 약 340 px · 폭 약 260 px',
                    '제단 둘레에 옅은 청록 빛이 은은히 감돌고, 받침 돌 단이 넓어져 방의 「성소」처럼 보이게'),
    'devil_altar': ('악마의 제단', 'obj_devil_altar.png',
                    '검은 바위 계단 위 뿔 달린 악마 상 + 붉은 보석 + 양쪽 매달린 화로 + 찢어진 붉은 깃발',
                    '폭 약 360 px · 높이 약 340 px',
                    '작은 건물(악마의 사당)처럼 무게감 있게 — 바위 계단이 넓고 갈라진 틈에 붉은 용암 빛, 화로 불이 살아 있게'),
    'shop_stall': ('유령 노점', 'obj_shop_stall.png',
                   '보라 · 금 줄무늬 천막 노점 + 물약 · 두루마리 · 금화가 놓인 진열대 + 등불',
                   '폭 약 380 px · 높이 약 320 px',
                   '진열대 물건이 더 많고 또렷하게(물약 · 두루마리 · 금화 더미 · 작은 상자), 등불 빛이 따뜻하게'),
}

TEMPLATE = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」 방 오브젝트 퀄업 **시안** — 완성된 게임 화면 한 장(720 x 1280 세로).

[무엇] 「{name}」 — 맵(방)에 딱 하나만 서는 특별 오브젝트. 지금은 너무 작아 티가 안 난다(지금 192 px 칸 — 유닛 한 명과 비슷).
[디자인 앵커 — 반드시 그대로] Projects/AVSR/_exchange/in/{src} — {desc}.
  **디자인 · 색 · 구성은 이 그림 그대로** 두고, 크기를 키우고 퀄을 올린다(새 디자인 · 다른 양식으로 바꾸지 말 것).
[바탕] Projects/AVSR/_exchange/ref/skill_qx_check/robot.png — 지금 게임 화면(위 HUD · 아래 조작 버튼 · 방 바닥 그대로).
  방 안의 적 · 이펙트 · 데미지 숫자는 지우고, 아래쪽에 플레이어 캐릭터 하나만 그대로 둔다(크기 비교용, 약 144 px).
[크기 · 위치] 오브젝트는 방 가로 가운데, 방 판(y 약 230~820)의 위쪽 절반에 선다. 크기 {size} —
  플레이어보다 훨씬 커서 방에 들어오면 한눈에 「저게 그거다」.
[퀄업] {qual}. 도트 밀도를 높여 디테일을 살리고, 바닥 그림자 · 은은한 빛으로 바닥에 붙어 서 있게. 위에서 비스듬히 내려다본 방 시점과 맞게.
- 게임과 같은 도트(픽셀 아트) 그림체. 별 · 표창 · 십자 반짝이 모양 금지. 화면 전체를 한 색으로 물들이지 않는다. 글자 · 이름표는 넣지 않는다.
[저장] Projects/AVSR/_exchange/in/mock_obj_{key}_v1.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('heal_shrine','devil_altar','shop_stall')
$ps = foreach ($k in $keys) {
    Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_obj_mock_$k.ps1' *> 'Projects/AVSR/_exchange/log_obj_mock_$k.txt'" -WindowStyle Hidden -PassThru
    Start-Sleep 3
}
$ps | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/mock_obj_$($k)_v1.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main():
    for key, (name, src, desc, size, qual) in OBJS.items():
        body = TEMPLATE.format(name=name, src=src, desc=desc, size=size, qual=qual, key=key)
        with open(os.path.join(EX, 'order_obj_mock_%s.ps1' % key), 'w', encoding='utf-8-sig') as f:
            f.write(body)
    with open(os.path.join(EX, 'run_obj_mocks.ps1'), 'w', encoding='utf-8-sig') as f:
        f.write(RUN)
    print(len(OBJS), 'orders')


if __name__ == '__main__':
    main()
