# -*- coding: utf-8 -*-
"""소환 「영혼 배양실」 공개 순간 부품 2차 — PD 2026-10-22 「깨지고 그 뒤에 불꽃 연출도 이상하고 · 디자인 퀄리티가 별로」.
지금 게임은 예전 레벨업 주황 빛살을 빌려 써서 탁했다 → 고른 시안 공개 순간 크롭을 앵커로 전용 부품을 받는다.
납품: Projects/AVSR/_exchange/in/lab_tank_broken · lab_reveal_light · lab_glass_shards .png
"""
import io
import os

from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
REF = os.path.join(EX, 'ref', 'summon_lab')

HEAD = '''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형 · 필터를 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — 소환(가챠) 「영혼 배양실」 공개 순간 **부품**.
먼저 읽는다: Projects/AVSR/_exchange/codex_brief_fx.md · Projects/AVSR/_exchange/guide_summon_lab.md.
**정답 그림(이 수준 · 화풍 그대로)**: Projects/AVSR/_exchange/ref/summon_lab/anchor_peak_crop.png (PD 가 고른 시안의 공개 순간)
같이 쓰는 그림: Projects/AVSR/_exchange/in/lab_tank.png (배양조) · Projects/AVSR/_exchange/in/lab_stage.png (무대)
⚠ PD: 「깨지고 그 뒤에 불꽃 연출도 이상하고 · 전체적으로 연출은 괜찮은데 디자인 퀄리티가 별로」 — 지금은 예전 레벨업용 주황 가시 빛살을 빌려 써서 탁했다. 대충 그리지 마라.

[부품] {what}
{spec}
- 캐릭터 · 유령 · 글자는 그리지 마라.
※ 바탕 색이 지정과 조금 달라도 · 크기가 조금 달라도 **고치려 하지 말고 그대로 저장하라**. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

ORDERS = [
    ('lab_tank_broken', '깨진 배양조 — 공개 뒤 남는 배양조 틀(유리창이 깨져 뻥 뚫림)',
     '- 형태 그림: **마젠타 #FF00FF 단색 바탕**.\n'
     '- **lab_tank.png 와 정확히 같은 크기 · 같은 모양 · 같은 자리**의 배양조. 위 뚜껑 · 양옆 관 · 아래 받침은 그대로, **유리창만 깨져서**\n'
     '  가장자리에 들쭉날쭉한 유리 조각이 붙어 있고 가운데는 뚫려 비어 있다(뒤가 보이게 그 자리는 마젠타).\n'
     '- 액체는 빠지고 아래 받침에 청록 물기 · 물웅덩이만 조금. 몸 · 빛은 그리지 마라.'),
    ('lab_reveal_light', '공개 빛 — 몸 뒤에서 터지는 금빛 빛줄기 · 빛 알갱이, 머무는 루프 6장',
     '- 빛 그림: **검은 바탕(#000000)**(게임이 가산으로 겹친다).\n'
     '- 가로 6칸 한 줄, 정사각 칸. 정답 그림처럼 **가운데 아래(몸 발 자리)에서 위 · 양옆으로 퍼지는 금빛 빛줄기 다발** + 네모 도트 빛 알갱이 · 반짝 별.\n'
     '  가운데는 몸이 서므로 너무 하얗게 덮지 않는다(빛 뿌리만 밝게).\n'
     '- 1→6→1 끊김 없이 빛줄기가 천천히 일렁이고 알갱이가 위로 오른다. 주황 · 빨강 불꽃이 아니라 **금 · 노랑 · 흰 빛**.'),
    ('lab_glass_shards', '유리 조각 6개 — 공개 순간 사방으로 튀는 낱개 유리 파편(게임이 하나씩 날린다)',
     '- 형태 그림: **마젠타 #FF00FF 단색 바탕**.\n'
     '- 가로 6칸 한 줄, 정사각 칸마다 **유리 조각 한 개**(크기 · 모양 다르게: 긴 삼각 · 넓은 판 · 작은 쐐기 등).\n'
     '  정답 그림의 유리 조각처럼 밝은 하늘 · 흰 유리, 또렷한 도트 외곽선, 한쪽 면에 하이라이트.\n'
     '- 번짐 · 빛 · 물방울은 그리지 마라 — 조각 하나만 또렷하게, 칸 가운데.'),
]


def main():
    m = Image.open(os.path.join(EX, 'in', 'mock_summon_lab_free_peak.png')).convert('RGB')
    w, h = m.size
    m.crop((0, int(h * 0.08), w, int(h * 0.78))).save(os.path.join(REF, 'anchor_peak_crop.png'))
    keys = []
    for name, what, spec in ORDERS:
        io.open(os.path.join(EX, f'order_{name}.ps1'), 'w', encoding='utf-8-sig').write(HEAD.format(what=what, spec=spec, name=name))
        keys.append(name)
    run = ['Set-Location "C:\\won\\UnityProject\\AvengingSprit"',
           'foreach ($k in @(' + ','.join(f'"{k}"' for k in keys) + ')) {',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }',
           '    for ($try = 0; $try -lt 4; $try++) {',
           '        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"',
           '        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }',
           '        Start-Sleep 300',
           '    }',
           '    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }',
           '}']
    io.open(os.path.join(EX, 'run_lab_parts2.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('orders', keys)


if __name__ == '__main__':
    main()
