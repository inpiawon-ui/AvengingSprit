"""코덱스 시안 발주 — 어떤 화면이든 같은 틀로 (스킬 fx-art-pipeline 3단계, PD 2026-10-09).

결과창에서 「코덱스 자유 시안」이 PD 한 번에 통과했다 — 그 프롬프트 틀을 그대로 쓴다.
자유 시안(코덱스가 아트 디렉터) + 가이드 시안(우리 박자표대로)을 한 장씩 병렬로 받는다.

python order_codex_mock.py <주제> <바탕캡처.png> [--peak 힘주는순간설명] [--idle 머무는장면설명]
                           [--ref 기준그림경로 ...] [--pd "PD 원문"] [--guide 가이드.md] [--size 920x690]
예) python order_codex_mock.py result Projects/AVSR/_exchange/ref/result_mock/base_idle.png \\
      --ref Assets/BaseResource/ChapterScreens/reward_goldpile.png --pd "상자가 돋보이게" --guide Projects/AVSR/_exchange/guide_result_fx.md
→ _exchange/order_mock_<주제>_{free_peak,free_idle[,guide_peak,guide_idle]}.ps1 + run_mock_<주제>.ps1
   납품: _exchange/in/mock_<주제>_*.png
"""
import argparse
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')

HEAD = r'''$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$prompt = @'
이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트) — {topic} 연출 **시안**.
**먼저 Projects/AVSR/_exchange/codex_brief_fx.md 를 읽는다** — 이 게임의 빛은 「도트 모양 + 셰이더 광원 · 파티클」을 섞는다(툰 느낌만 그리지 말 것), 강약 · 연출 원칙.
[바탕] {base} — 지금 게임 화면. 창 · 칸 · 글자 · 캐릭터 · 물건 그림은 **그대로** 두고 그 위에 연출을 덧그린다.
{refs}PD 말: 「{pd}」 · 공통: 「약하게 할 때는 약하게, 힘줄 때는 힘을 줘야지 · 게임 정보(글자 · 숫자)는 가리지 마 · 되도 않는 그림자 넣지 마」.
'''

FREE = HEAD + r'''[장면] {name} — 네가 아트 디렉터라면 어떻게 꾸미겠나. {scene}
기준 그림은 참고일 뿐, 더 멋진 안이 있으면 그걸로. 도트 모양 + 부드럽게 퍼지는 광원(블룸 · 빛 받는 주변) + 떠오르는 빛 알갱이를 함께.
- 크기는 바탕과 같은 {size}. 그림 옆 여백에 한국어 메모 3줄 이내로 「무엇이 · 어떻게 움직이는지」를 적어도 된다.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

GUIDE = HEAD + r'''먼저 {guide} 를 읽는다(목표 모습 · 박자 · 강약).
[장면] {name} — 가이드의 박자표대로. {scene}
- 크기는 바탕과 같은 {size}.
[저장] Projects/AVSR/_exchange/in/{name}.png
'@
codex exec --sandbox workspace-write --skip-git-repo-check $prompt
'''

RUN = r'''Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @(KEYS)
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
'''


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('topic')
    ap.add_argument('base')
    ap.add_argument('--peak', default='**힘주는 순간**(완성 · 착지 · 획득의 정점) 한 장.')
    ap.add_argument('--idle', default='같은 안의 **머무는 장면**(계속 보이는 모습) 한 장 — 은은하지만 주인공이 돋보이게.')
    ap.add_argument('--ref', action='append', default=[])
    ap.add_argument('--pd', default='')
    ap.add_argument('--guide', default='')
    ap.add_argument('--size', default='920 x 690')
    a = ap.parse_args()

    refs = ''.join('[참고 그림] %s\n' % r for r in a.ref)
    common = dict(topic=a.topic, base=a.base, refs=refs, pd=a.pd, size=a.size, guide=a.guide)
    jobs = [('free_peak', FREE, a.peak), ('free_idle', FREE, a.idle)]
    if a.guide:
        jobs += [('guide_peak', GUIDE, a.peak), ('guide_idle', GUIDE, a.idle)]
    keys = []
    for suffix, tpl, scene in jobs:
        name = 'mock_%s_%s' % (a.topic, suffix)
        with open(os.path.join(EX, 'order_%s.ps1' % name), 'w', encoding='utf-8-sig') as f:
            f.write(tpl.format(name=name, scene=scene, **common))
        keys.append(name)
    run = os.path.join(EX, 'run_mock_%s.ps1' % a.topic)
    with open(run, 'w', encoding='utf-8-sig') as f:
        f.write(RUN.replace('KEYS', ','.join("'%s'" % k for k in keys)))
    print(len(keys), 'orders →', run)


if __name__ == '__main__':
    main()
