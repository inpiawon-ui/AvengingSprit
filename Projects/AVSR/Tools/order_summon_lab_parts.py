# -*- coding: utf-8 -*-
"""소환 「영혼 배양실」 부품 발주(스킬 fx-art-pipeline 5단계) — 고른 시안을 앵커로 한 장씩, 순서대로.
가이드: Projects/AVSR/_exchange/guide_summon_lab.md
PD 2026-10-22 「그 조합으로 진행해줘」: 버팀 = 자유 시안(대박 · 픽업) / 가이드 시안(B · A) · 공개 = 자유 시안의 깨지는 유리 · 빛살
납품: Projects/AVSR/_exchange/in/lab_*.png — 금 · 파편은 배양조(lab_tank) 납품을 앵커로 쓰므로 그 뒤에 보낸다.
"""
import io
import os

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 연출 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md (컨셉 · 박자표).
PD 가 고른 시안(이 화풍이 정답): {anchors}
화풍 기준(실험실 소품 · 이미 게임에 있는 것): Assets/BaseResource/InGameMainUI/obj_lab_bulk.png · Assets/BundleResource/RoomFloor/roomfloor_env_lab_full.png
PD 말: 「motion-library 의 gacha_reveal_1 참고해서 가차 · 우리 컨셉에 맞게 실험실에서 유령이 나오는 컨셉」 · 「그 조합으로 진행해줘」.

[부품] {what}
{spec}
- 시안 속 캐릭터(용) · 유령 · 글자 · 이름 띠는 **그리지 마라**(게임이 따로 얹는다).
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

FREE_IDLE = 'Projects/AVSR/_exchange/in/mock_summon_lab_free_idle.png'
FREE_PEAK = 'Projects/AVSR/_exchange/in/mock_summon_lab_free_peak.png'
GUIDE_IDLE = 'Projects/AVSR/_exchange/in/mock_summon_lab_guide_idle.png'
TANK = 'Projects/AVSR/_exchange/in/lab_tank.png'
FORM = '- 형태 그림: **마젠타 #FF00FF 단색 바탕**(반투명 헤일로 금지 · 검은 바탕 금지).\n'
LIGHT = '- 빛 그림: **검은 바탕(#000000)** 위에(게임이 가산으로 겹친다 — 검은색 = 투명). 마젠타 금지.\n'

ORDERS = [
    ('lab_stage', [FREE_IDLE, FREE_PEAK],
     '배양실 무대 바탕 — 세로 720×1280 비율 전체 화면 한 장',
     '- 시안의 어두운 청록 실험실(벽 패널 · 바닥 관 · 아래 받침대 · 옆 작은 배양조들 · 경고등).\n'
     '- **화면 가운데(위에서 25%~72%)는 비워 둔다** — 큰 배양조를 따로 얹는다. 그 자리 바닥에 배양조가 설 둥근 받침만.\n'
     '- 왼쪽 아래 구석에 유령이 빠져나올 **관 끝(열린 배출구)** 을 그린다(유령은 그리지 마라).'),
    ('lab_tank', [FREE_IDLE, GUIDE_IDLE],
     '큰 배양조 한 개 — 빈 유리 원통(안에 청록 액체 · 기포 몇 개, **몸은 없음**)',
     FORM + '- 세로로 긴 한 장(가로:세로 약 3:5). 위 금속 뚜껑 · 양옆 관 · 아래 받침 · 가운데 넓은 유리창(시안의 배양조와 같은 모양).\n'
     '- 유리 안 액체는 **반쯤 비쳐 보이는 밝은 청록**(나중에 안에 짙은 몸 그림자를 겹친다). 유리창은 가운데가 넓고 곧게.'),
    ('lab_tank_glass', [FREE_IDLE],
     '배양조 유리 앞면 반사광 — 몸 그림자 위에 겹쳐 「유리 속에 있다」로 보이게 하는 빛',
     LIGHT + f'- **{TANK} 와 정확히 같은 크기 · 같은 자리**의 한 장. 유리창 자리에만 세로 반사 줄 2~3개 · 가장자리 은은한 하이라이트 · 작은 기포 점.\n'
     '- 금속 부분 · 액체 색은 그리지 마라(빛만).'),
    ('lab_crack', [FREE_IDLE, FREE_PEAK],
     '유리 금 — 버팀 동안 점점 번지는 금 3단계(가로 3칸 한 줄)',
     FORM + f'- 칸마다 **{TANK} 와 같은 크기**(가로로 3칸 이어 붙임). 각 칸에 그 배양조의 **유리창 자리에만** 흰 · 하늘색 금(도트 선).\n'
     '- 1 칸: 가운데 작은 금 · 2 칸: 사방으로 번짐 · 3 칸: 유리창 전체에 거미줄 금(깨지기 직전). 금속 · 액체 · 배양조 몸체는 그리지 마라 — 금 선만.'),
    ('lab_shatter', [FREE_PEAK],
     '유리 깨짐 — 배양조 유리가 산산이 깨지며 파편 · 액체가 튀는 6장(한 번 재생)',
     FORM + f'- 가로 6칸 한 줄, 칸마다 {TANK} 크기의 1.6배(파편이 밖으로 튄다). 배양조 몸체는 그리지 마라 — 유리 파편 · 액체 방울 · 물보라만.\n'
     '- 1 유리창이 부서지기 시작 → 2~3 파편이 사방으로 가장 크게 튐(자유 시안 공개 순간처럼) → 6 파편이 떨어져 바닥에 흩어짐 · 물보라 사라짐.\n'
     '- 파편은 밝은 하늘 · 흰 유리 조각(도트), 액체는 청록 방울.'),
]


def main():
    keys = []
    for name, anchors, what, spec in ORDERS:
        body = HEAD.format(anchors=' · '.join(anchors), what=what, spec=spec, name=name)
        io.open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig').write(body)
        keys.append(name)
    run = ['Set-Location "C:\\won\\UnityProject\\AvengingSprit"',
           'foreach ($k in @(' + ','.join(f'"{k}"' for k in keys) + ')) {',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }',
           '    for ($try = 0; $try -lt 4; $try++) {',
           '        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"',
           '        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }',
           '        Start-Sleep 300',
           '    }',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k"; if ($k -eq "lab_tank") { break } }',
           '}']
    io.open(os.path.join(EX, 'run_lab_parts.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('orders', len(keys))


if __name__ == '__main__':
    main()
