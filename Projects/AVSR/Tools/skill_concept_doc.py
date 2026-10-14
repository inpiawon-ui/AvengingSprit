"""스킬 컨셉 기획 문서(HTML) — skill_concepts.py 를 읽어 한 페이지로 만든다. 시안 그림은 img/ 에 JPG 로 줄여 둔다.
쓰기: python skill_concept_doc.py   → Projects/AVSR/_exchange/doc_skill_concept/index.html + img/*.jpg
"""
import html
import os
import sys
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from skill_concepts import HOSTS, GROUPS  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
IN = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'doc_skill_concept')
IMG = os.path.join(OUT, 'img')

GROUP_ORDER = ['single', 'summon', 'rush', 'control', 'trap', 'barrage', 'guard']
GROUP_NOTE = {
    'single': '보스 · 엘리트처럼 한 놈을 빨리 지울 때 고르는 몸.',
    'summon': '내 편을 세워 같이 싸운다. 소환물은 시전자와 같이 움직이고 같이 친다.',
    'rush': '몸이 적 사이를 날아다니며 친다. 이동은 잔상을 끌며 부드럽게.',
    'control': '적을 한곳에 모아 붙잡았다가 한 방에 정리한다.',
    'trap': '깔아 두면 시간이 지나 터진다. 설치하는 순간과 터지는 순간이 둘 다 보인다.',
    'barrage': '탄 · 빔 · 번개로 화면을 채운다. 몸마다 탄의 모양(부채 · 추적 · 관통 · 축적 · 위성)이 다르다.',
    'guard': '맞으면서 버티고, 버틴 만큼 돌려준다.',
}


def jpg(name, out):
    src = os.path.join(IN, name + '.png')
    if not os.path.exists(src):
        return None
    m = Image.open(src).convert('RGB')
    m = m.crop((0, 0, m.width, int(m.height * 0.94)))   # 아래 메모 칸(코덱스 글씨)은 문서 글로 대신한다
    w = 1440
    m = m.resize((w, int(m.height * w / m.width)), Image.LANCZOS)
    os.makedirs(IMG, exist_ok=True)
    m.save(os.path.join(IMG, out + '.jpg'), quality=80, optimize=True)
    return f'img/{out}.jpg'


def e(s):
    return html.escape(s, quote=True)


CSS = r'''
<title>AVSR 호스트 스킬 컨셉</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Black+Han+Sans&family=Noto+Sans+KR:wght@400;500;700&family=JetBrains+Mono:wght@500&display=swap">
<style>
/* 레이아웃: 한 줄 읽기 폭의 문서 + 캐릭터마다 시안 두 줄(액티브 · 필살기) — 게임 HUD 의 남색 · 금색 · 하늘색을 그대로 빌린다 */
:root {
  --bg: #f3f4f8; --surface: #ffffff; --ink: #1b1f2e; --muted: #5d6478; --line: #d9dce6;
  --gold: #a8740a; --sky: #1f6fb8; --red: #b8323a; --chip: #e8ebf3; --badge-bg: #fbe8e8; --badge-fg: #9b2a31;
  --display: "Black Han Sans", "Noto Sans KR", system-ui, sans-serif;
  --body: "Noto Sans KR", system-ui, -apple-system, "Apple SD Gothic Neo", sans-serif;
  --mono: "JetBrains Mono", ui-monospace, Consolas, monospace;
}
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) {
  --bg: #11131c; --surface: #1a1d2a; --ink: #e9ebf2; --muted: #9aa1b5; --line: #2b3042;
  --gold: #f2c45a; --sky: #7cc0ff; --red: #ff8a8f; --chip: #242838; --badge-bg: #3a1e22; --badge-fg: #ffb3b6; color-scheme: dark } }
:root[data-theme="dark"] {
  --bg: #11131c; --surface: #1a1d2a; --ink: #e9ebf2; --muted: #9aa1b5; --line: #2b3042;
  --gold: #f2c45a; --sky: #7cc0ff; --red: #ff8a8f; --chip: #242838; --badge-bg: #3a1e22; --badge-fg: #ffb3b6; color-scheme: dark }
* { box-sizing: border-box }
body { background: var(--bg); color: var(--ink); font-family: var(--body); font-size: 15px; line-height: 1.65; margin: 0 }
.wrap { max-width: 1120px; margin: 0 auto; padding-inline: 20px; padding-block: 40px 80px; display: grid; gap: 40px }
h1, h2, h3 { font-family: var(--display); font-weight: 400; letter-spacing: .01em; text-wrap: balance; margin: 0 }
h1 { font-size: clamp(30px, 5vw, 44px); line-height: 1.15 }
h2 { font-size: 26px }
h3 { font-size: 22px }
p { margin: 0 }
.lede { color: var(--muted); max-width: 68ch; margin-top: 10px }
.meta { font-family: var(--mono); font-size: 12px; color: var(--muted); letter-spacing: .04em; text-transform: uppercase }
.rules { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px }
.rule { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 18px 20px; display: grid; gap: 8px; min-width: 0 }
.rule h3 { font-size: 19px }
.rule.act h3 { color: var(--sky) }
.rule.ult h3 { color: var(--red) }
.rule.pas h3 { color: var(--gold) }
.rule ul { margin: 0; padding-left: 18px; display: grid; gap: 4px }
.map { display: grid; gap: 10px }
.maprow { display: grid; grid-template-columns: 240px 1fr; gap: 12px; align-items: baseline; padding: 10px 0; border-top: 1px solid var(--line) }
.maprow b { font-weight: 700 }
.chips { display: flex; flex-wrap: wrap; gap: 6px }
.chips a { background: var(--chip); color: var(--ink); text-decoration: none; padding: 2px 10px; border-radius: 999px; font-size: 14px }
.chips a:hover, .chips a:focus-visible { outline: 2px solid var(--sky); outline-offset: 1px }
.group { display: grid; gap: 22px }
.group > header { display: grid; gap: 4px; border-bottom: 2px solid var(--ink); padding-bottom: 10px }
.host { background: var(--surface); border: 1px solid var(--line); border-radius: 14px; padding: 22px; display: grid; gap: 18px; scroll-margin-top: 16px }
.hosthead { display: flex; flex-wrap: wrap; align-items: baseline; gap: 6px 14px }
.hosthead .num { font-family: var(--mono); color: var(--muted); font-size: 14px }
.hosthead .concept { color: var(--gold); font-weight: 700 }
.badge { margin-left: auto; background: var(--badge-bg); color: var(--badge-fg); font-size: 13px; font-weight: 700; padding: 2px 10px; border-radius: 6px }
.pas { display: grid; grid-template-columns: 90px 1fr; gap: 8px; align-items: baseline }
.label { font-size: 12px; font-weight: 700; letter-spacing: .06em }
.label.act { color: var(--sky) } .label.ult { color: var(--red) } .label.pas { color: var(--gold) }
.skill { display: grid; grid-template-columns: minmax(0, 3fr) minmax(0, 2fr); gap: 18px; align-items: start }
.skill figure { margin: 0 }
.skill img { width: 100%; height: auto; border-radius: 8px; display: block; background: var(--chip) }
.skill .txt { display: grid; gap: 6px; min-width: 0 }
.skill .name { font-size: 18px; font-weight: 700 }
.skill .cuts { color: var(--muted); font-size: 13px }
.missing { aspect-ratio: 16/9; display: grid; place-items: center; color: var(--muted); border: 1px dashed var(--line); border-radius: 8px }
.ask { background: var(--surface); border: 1px solid var(--line); border-radius: 12px; padding: 20px 22px; display: grid; gap: 10px }
.ask ol { margin: 0; padding-left: 20px; display: grid; gap: 6px }
@media (max-width: 760px) {
  .skill { grid-template-columns: 1fr }
  .maprow { grid-template-columns: 1fr; gap: 6px }
  .pas { grid-template-columns: 1fr; gap: 2px }
  .badge { margin-left: 0 }
}
</style>
'''


def main():
    keys = list(HOSTS)
    idx = {k: i for i, k in enumerate(keys, 1)}
    parts = [CSS, '<div class="wrap">']
    parts.append('''<header>
<p class="meta">Avenging Spirit RE:BORN · 기획 · 2026-10-14 · PD 확인 전 안</p>
<h1>호스트 23명 스킬 컨셉</h1>
<p class="lede">몸마다 컨셉을 하나 정하고, 그 컨셉을 작게 쓰는 것이 액티브, 크게 터뜨리는 것이 필살기입니다. 패시브는 그 컨셉이 평타에 늘 묻어나는 자리입니다. 모든 시안은 PD 피드백 12개 기준(기승전결 · 캐릭터가 스킬을 만든다 · 소환물도 같이 공격 · 크기 절제 등)으로 검수했습니다.</p>
</header>''')
    parts.append('''<section class="rules" aria-label="규칙">
<div class="rule act"><h3>액티브 · 일반 스킬</h3><ul>
<li>평타가 맞을 때마다 충전(지금 30대)</li><li>컷인 없이 바로 나간다</li><li>예전 스킬을 살려 위력만 낮춘다</li><li>적 호스트도 이 스킬(약식판)만 쓴다</li></ul></div>
<div class="rule ult"><h3>필살기</h3><ul>
<li>빙의할 때 1회 — 쓰면 다음 빙의까지 잠김</li><li>컷인 + 전투 멈춤, 가장 강한 연출</li><li>이번 시안(던파 레퍼런스) 연출</li><li>적 호스트는 쓰지 않는다(대장은 결정 필요)</li></ul></div>
<div class="rule pas"><h3>패시브</h3><ul>
<li>몸에 늘 붙어 있는 효과(지금 값 그대로)</li><li>숙련도 0 이면 액티브와 같이 잠김</li><li>시안 세 번째 컷이 패시브가 터지는 순간</li></ul></div>
</section>''')

    parts.append('<section class="map" aria-label="컨셉군"><h2>컨셉군 7개</h2>')
    for g in GROUP_ORDER:
        members = [k for k in keys if HOSTS[k][1] == g]
        chips = ''.join(f'<a href="#h-{k}">{e(HOSTS[k][0])}</a>' for k in members)
        parts.append(f'<div class="maprow"><div><b>{e(GROUPS[g])}</b></div><div class="chips">{chips}</div></div>')
    parts.append('</section>')

    for g in GROUP_ORDER:
        parts.append(f'<section class="group" aria-label="{e(GROUPS[g])}"><header><h2>{e(GROUPS[g])}</h2><p class="lede">{e(GROUP_NOTE[g])}</p></header>')
        for k in [k for k in keys if HOSTS[k][1] == g]:
            who, grp, concept, (pn, pd), (an, ad, _cuts), (un, ud), ult, mark = HOSTS[k]
            act_src = 'mock_active_' + k + ('_v2' if os.path.exists(os.path.join(IN, f'mock_active_{k}_v2.png')) else '')
            a_img = jpg(act_src, f'active_{k}')
            u_img = jpg(f'mock_skill_{ult}', f'ult_{k}')
            badge = f'<span class="badge">{e(mark)}</span>' if mark else ''

            def fig(src, alt):
                if not src:
                    return '<div class="missing">시안 대기</div>'
                return f'<figure><a href="{src}" target="_blank"><img src="{src}" alt="{e(alt)}" loading="lazy"></a></figure>'

            parts.append(f'''<article class="host" id="h-{k}">
<div class="hosthead"><span class="num">{idx[k]:02d}</span><h3>{e(who)}</h3><span class="concept">{e(concept)}</span>{badge}</div>
<div class="pas"><span class="label pas">패시브</span><p><b>{e(pn)}</b> — {e(pd)}</p></div>
<div class="skill">{fig(a_img, f"{who} 액티브 {an} 시안")}<div class="txt"><span class="label act">액티브 · 평타로 충전</span><span class="name">{e(an)}</span><p>{e(ad)}</p><p class="cuts">세 컷: 액티브 발동 → 결과 → 패시브 순간</p></div></div>
<div class="skill">{fig(u_img, f"{who} 필살기 {un} 시안")}<div class="txt"><span class="label ult">필살기 · 빙의할 때 1회</span><span class="name">{e(un)}</span><p>{e(ud)}</p><p class="cuts">세 컷: 기 → 승 · 전 → 결</p></div></div>
</article>''')
        parts.append('</section>')

    parts.append('''<section class="ask"><h2>정해 주실 것</h2><ol>
<li>컨셉군 7개와 캐릭터 배정</li>
<li>★ 변경 6곳 — 필살기 4(갱스터 청부 처형 · 닌자(사슬) 사슬 처형 · 로봇 요새 모드 · 아마존 정예 역린) · 액티브 2(영매 해골 소환 · 호퍼(기관단총) 백스텝 사격)</li>
<li>액티브와 필살기 차이가 약한 몸 — 코만도(미사일)(둘 다 미사일 무리) · 샐러맨더(둘 다 독 장판)</li>
<li>버튼 — 두 개 / 액티브 자동 발동 + 필살기 버튼 하나</li>
<li>같은 몸에 다시 빙의하면 필살기를 다시 줄지</li>
<li>갱스터 청부 처형의 보스 피해 상한(제안 15~20%)</li>
</ol></section>''')
    parts.append('</div>')
    os.makedirs(OUT, exist_ok=True)
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf-8').write('\n'.join(parts))
    total = sum(os.path.getsize(os.path.join(IMG, f)) for f in os.listdir(IMG))
    print('ok', len(os.listdir(IMG)), 'imgs', round(total / 1e6, 1), 'MB')


if __name__ == '__main__':
    main()
