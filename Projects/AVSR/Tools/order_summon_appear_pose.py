# -*- coding: utf-8 -*-
"""소환 공개 — 캐릭터 「등장 자세」 한 장 발주(PD 2026-10-23 「캐릭터도 애니가 있어야지 · 나오면서 포즈 같은 거 없으면 추가」).
원작에는 승리 · 등장 자세가 없다(서기 · 걷기 · 공격 · 맞기 · 죽기뿐). 게임은 그동안 공격 자세 atk1 → atk2 로 대신한다.
샘플 하나(블루 드래곤) 먼저 → 통과하면 나머지 23명.
입력: 원작 시트 3배(몸 · 화풍의 정답) · 지금 큰 그림 서기 · 공격 2장(크기 · 발 자리 · 외곽선 기준)
납품: Projects/AVSR/_exchange/in/appear_{키}.png  (384×384, 분홍 바탕)
쓰는 법: python order_summon_appear_pose.py <키> [<키> ...]
"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from charv2_notes import NOTES, SHEET   # noqa: E402

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')


def order(key):
    n = NOTES[key]
    big = f'Assets/BaseResource/UnitLarge/{key}/unitL_{key}_se'
    return f'''$env:OPENAI_API_KEY = $null
Set-Location "C:\\won\\UnityProject\\AvengingSprit"
$prompt = @'
[작업] 게임 캐릭터 픽셀아트 — {key}({n["name"]}) 「등장 자세」 한 장
API 키를 쓰는 CLI 폴백은 쓰지 마라. 내장 이미지 생성 · 편집 기능만 써라. 스크립트로 그려 때우지 마라.

소환(가챠)에서 배양조 유리가 깨지고 이 캐릭터가 튀어나와 **「내가 왔다」 하고 자세를 잡는 순간**의 한 장.
힘 있게, 캐릭터 성격이 드러나게(무기 · 주먹 · 날개 등 그 캐릭터의 특징을 앞세운 결정 자세).

[정답 · 기준]
1. 원작 시트(몸 생김 · 색 · 화풍의 정답): Projects/AVSR/_exchange/ref/char_v2/orig_{key}_x3.png
2. 지금 게임 큰 그림 — 크기 · 발 자리 · 외곽선 · 색 그대로 맞출 것:
   서기 {big}.png · 공격1 {big}_atk1.png · 공격2 {big}_atk2.png
3. 원작 메모: {n.get("body", "")}

[규격]
- **384×384, 마젠타 #FF00FF 단색 바탕**, 오른쪽 아래 비스듬히(지금 서기 그림과 같은 방향).
- 발 자리 · 몸 크기는 서기 그림과 같게(칸 아래에서 같은 높이에 발이 닿게). 몸이 칸 밖으로 나가지 않게.
- 원작에 없는 장신구 · 무기 · 빛 · 이펙트는 그리지 마라. 원작 캐릭터 그대로, 자세만 새로.
- 외곽선 · 도트 크기 · 명암 단계는 지금 큰 그림과 같게.
※ 크기가 조금 달라도 고치려 하지 말고 그대로 저장하라. 저장을 건너뛰지 마라.
[저장] Projects/AVSR/_exchange/in/appear_{key}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''


def main(keys):
    for k in keys:
        if k not in NOTES:
            raise SystemExit(f'원작 메모 없음: {k}')
        io.open(os.path.join(EX, f'order_appear_{k}.ps1'), 'w', encoding='utf-8-sig').write(order(k))
    run = ['Set-Location "C:\\won\\UnityProject\\AvengingSprit"',
           'foreach ($k in @(' + ','.join(f'"{k}"' for k in keys) + ')) {',
           '    if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { "skip $k"; continue }',
           '    for ($try = 0; $try -lt 4; $try++) {',
           '        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_appear_$k.ps1" *> "Projects/AVSR/_exchange/log_appear_$k.txt"',
           '        if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { break }',
           '        Start-Sleep 300',
           '    }',
           '    if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { "ok $k" } else { "MISSING $k" }',
           '}']
    io.open(os.path.join(EX, 'run_appear.ps1'), 'w', encoding='utf-8-sig').write('\n'.join(run) + '\n')
    print('orders', keys)


if __name__ == '__main__':
    main(sys.argv[1:])
