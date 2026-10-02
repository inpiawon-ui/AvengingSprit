# -*- coding: utf-8 -*-
"""상성 체계 후보를 한 장으로 견주는 문서를 만든다 (AVSR_AffinitySchemes.html).

그림은 새로 그리지 않는다 — 게임에 이미 있는 캐릭터 스프라이트를 그대로 넣어
「이 몸이 어느 쪽에 들어가는가」가 눈으로 보이게 한다.
"""
import base64, math, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
UNIT = os.path.join(ROOT, 'Assets', 'BaseResource', 'Unit')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', 'AVSR_AffinitySchemes.html')

NAME = {
    'gangster': '갱스터', 'thug': '폭력배', 'amazon': '아마존', 'amazon_elite': '아마존 정예',
    'hopper': '호퍼', 'hopper_smg': '호퍼(기관단총)', 'commando_mg': '코만도(기관총)',
    'commando_laser': '코만도(레이저)', 'commando_grenade': '코만도(수류탄)',
    'commando_missile': '코만도(미사일)', 'salamander': '샐러맨더', 'dragoon': '드라군',
    'dragon_blue': '청룡', 'guru': '구루', 'white_wizard': '화이트 위저드', 'medium': '영매',
    'ninja': '닌자', 'ninja_chain': '닌자(사슬)', 'robot': '로봇', 'baseball': '슬러거',
    'snowwoman': '설녀', 'vampire': '흡혈귀', 'death': '사신',
    'skeleton': '해골', 'bat': '박쥐', 'scrapgunner': '폐품 사수', 'actor_enforcer': '집행자',
    'roadwarden': '순찰기', 'coilwalker': '코일 보행기',
    'robot_snakes': '로봇 스네이크', 'crusher': '크러셔', 'python': '파이썬',
    'sludge': '슬러지', 'guardian': '가디언', 'kingpin': '킹핀',
}


def sprite(key):
    for suffix in ('_s', ''):
        p = os.path.join(UNIT, key, f'unit_{key}{suffix}.png')
        if os.path.exists(p):
            with open(p, 'rb') as f:
                return 'data:image/png;base64,' + base64.b64encode(f.read()).decode()
    return ''


# (키, 이름, 색, 기호)
def scheme(sid, title, sub, kinds, beats, mutual, hosts, foes, why, good, bad, verdict):
    return dict(id=sid, title=title, sub=sub, kinds=kinds, beats=beats, mutual=mutual,
                hosts=hosts, foes=foes, why=why, good=good, bad=bad, verdict=verdict)


SCHEMES = [
    scheme('five', '안 1 · 다섯 속성', '불 · 물 · 나무 삼각 + 빛 · 어둠',
        [('fire', '불', '#E5482E'), ('wood', '나무', '#3FA34D'), ('water', '물', '#2F8FE0'),
         ('light', '빛', '#E3B420'), ('dark', '어둠', '#8A4FD0')],
        [('fire', 'wood'), ('wood', 'water'), ('water', 'fire')], [('light', 'dark')],
        {'fire': ['dragoon', 'commando_grenade', 'commando_missile', 'commando_mg', 'gangster', 'thug'],
         'water': ['snowwoman', 'dragon_blue', 'salamander', 'commando_laser'],
         'wood': ['amazon', 'amazon_elite', 'ninja', 'ninja_chain', 'hopper', 'hopper_smg', 'baseball'],
         'light': ['white_wizard', 'guru', 'robot'],
         'dark': ['death', 'vampire', 'medium']},
        {'fire': ['scrapgunner', 'crusher', 'kingpin'], 'water': ['sludge', 'coilwalker'],
         'wood': ['bat', 'python', 'actor_enforcer'], 'light': ['roadwarden', 'guardian'],
         'dark': ['skeleton', 'robot_snakes']},
        ['불은 나무를 태운다', '나무는 물을 빨아들인다', '물은 불을 끈다', '빛과 어둠은 서로를 크게 벤다'],
        ['일본 · 한국 모바일 게임에서 가장 널리 쓰는 틀이라 설명이 필요 없다',
         '빨강 · 초록 · 파랑 · 노랑 · 보라 — 색만으로 다섯이 한눈에 갈린다',
         '챕터마다 「이번 챕터는 불 적이 많다」처럼 색을 줄 수 있다'],
        ['총을 쏘는 몸(갱스터 · 코만도)에 속성을 붙이는 이유가 약하다 — 「화약 = 불」 정도',
         '다섯이라 방 하나에 다 섞이면 복잡하다'],
        '추천'),
    scheme('four', '안 2 · 네 원소', '불 · 물 · 바람 · 땅 (말씀하신 예시)',
        [('fire', '불', '#E5482E'), ('earth', '땅', '#B07A3C'), ('wind', '바람', '#35B5A0'),
         ('water', '물', '#2F8FE0')],
        [('fire', 'earth'), ('earth', 'wind'), ('wind', 'water'), ('water', 'fire')], [],
        {'fire': ['dragoon', 'commando_grenade', 'commando_missile', 'commando_mg', 'gangster', 'thug'],
         'water': ['snowwoman', 'salamander', 'vampire', 'medium', 'white_wizard'],
         'wind': ['ninja', 'ninja_chain', 'hopper', 'hopper_smg', 'dragon_blue', 'commando_laser'],
         'earth': ['amazon', 'amazon_elite', 'guru', 'baseball', 'robot', 'death']},
        {'fire': ['scrapgunner', 'crusher', 'kingpin'], 'water': ['sludge', 'python'],
         'wind': ['bat', 'coilwalker', 'roadwarden'],
         'earth': ['skeleton', 'actor_enforcer', 'robot_snakes', 'guardian']},
        ['불은 땅(숲)을 태운다', '땅은 바람을 막는다', '바람은 물을 흩는다', '물은 불을 끈다'],
        ['넷이라 외우기 쉽다', '원소라는 말 자체가 누구에게나 익숙하다'],
        ['「바람이 물을 이긴다 · 땅이 바람을 이긴다」는 사람마다 떠올리는 방향이 다르다 — 물 → 불 말고는 직관이 약하다',
         '마주 보는 둘(불 ↔ 바람, 땅 ↔ 물)은 아무 사이도 아니라 설명이 한 번 필요하다'],
        ''),
    scheme('three', '안 3 · 세 갈래', '힘 · 속도 · 기술 (가위바위보)',
        [('power', '힘', '#E5482E'), ('speed', '속도', '#3FA34D'), ('tech', '기술', '#2F8FE0')],
        [('power', 'speed'), ('speed', 'tech'), ('tech', 'power')], [],
        {'power': ['amazon', 'amazon_elite', 'baseball', 'robot', 'death', 'commando_mg', 'dragoon', 'guru'],
         'speed': ['ninja', 'ninja_chain', 'hopper', 'hopper_smg', 'gangster', 'thug', 'dragon_blue'],
         'tech': ['commando_laser', 'commando_grenade', 'commando_missile', 'white_wizard', 'medium',
                  'snowwoman', 'vampire', 'salamander']},
        {'power': ['actor_enforcer', 'crusher', 'guardian', 'skeleton'],
         'speed': ['bat', 'coilwalker', 'python', 'kingpin'],
         'tech': ['scrapgunner', 'roadwarden', 'sludge', 'robot_snakes']},
        ['힘은 속도를 한 방에 부순다', '속도는 기술을 흔든다', '기술은 힘을 요리한다'],
        ['셋이라 가장 단순하다 — 가위바위보', '몸의 생김새와 맞는다: 덩치 큰 몸 · 날랜 몸 · 장비와 주술을 쓰는 몸',
         '속성을 억지로 붙이지 않아도 된다'],
        ['「힘이 속도를 이긴다」는 방향이 게임마다 달라 한 번은 알려 줘야 한다',
         '색 말고는 화면에서 보여 줄 것이 약하다(불 · 물 같은 기운이 없다)'],
        ''),
    scheme('soul', '안 4 · 혼의 색', '이 게임만의 틀 — 몸마다 깃든 혼의 색',
        [('rage', '분노', '#E5482E'), ('calm', '고요', '#2F8FE0'), ('greed', '탐욕', '#E3B420'),
         ('grudge', '원한', '#8A4FD0')],
        [('rage', 'greed'), ('greed', 'calm'), ('calm', 'rage')], [('grudge', 'grudge')],
        {'rage': ['amazon', 'amazon_elite', 'baseball', 'dragoon', 'commando_mg', 'commando_grenade',
                  'commando_missile'],
         'calm': ['guru', 'white_wizard', 'snowwoman', 'ninja', 'ninja_chain', 'commando_laser', 'robot'],
         'greed': ['gangster', 'thug', 'hopper', 'hopper_smg', 'salamander', 'dragon_blue'],
         'grudge': ['death', 'vampire', 'medium']},
        {'rage': ['actor_enforcer', 'crusher', 'kingpin'], 'calm': ['roadwarden', 'guardian', 'coilwalker'],
         'greed': ['scrapgunner', 'bat', 'python', 'sludge'], 'grudge': ['skeleton', 'robot_snakes']},
        ['분노는 탐욕을 짓밟는다', '탐욕은 고요를 흔든다', '고요는 분노를 가라앉힌다',
         '원한은 어느 쪽에도 안 밀리지만 유령 에너지를 더 먹는다'],
        ['유령이 몸을 빼앗는 이 게임의 이야기와 맞닿는다 — 다른 게임에 없는 틀이다',
         '몸의 성격으로 나누므로 총잡이에게도 자연스럽다(갱스터 = 탐욕)'],
        ['처음 보는 틀이라 배워야 한다', '어느 감정이 어느 감정을 이기는지 직관이 약하다'],
        ''),
]


def diagram(s):
    kinds = s['kinds']
    cyc = [k for k in kinds if any(k[0] in b for b in s['beats'])]
    rest = [k for k in kinds if k not in cyc]
    W, H, cx, cy, R, r = 420, 300, 150, 150, 96, 34
    pos = {}
    for i, k in enumerate(cyc):
        a = -math.pi / 2 + 2 * math.pi * i / len(cyc)
        pos[k[0]] = (cx + R * math.cos(a), cy + R * math.sin(a))
    for i, k in enumerate(rest):
        pos[k[0]] = (350, 90 + i * 120)
    out = [f'<svg viewBox="0 0 {W} {H}" role="img" aria-label="{s["title"]} 상성 그림">',
           '<defs><marker id="ah-%s" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" '
           'orient="auto-start-reverse"><path d="M0 0L10 5L0 10z" class="ah"/></marker></defs>' % s['id']]

    def arrow(a, b, both=False):
        (x1, y1), (x2, y2) = pos[a], pos[b]
        d = math.hypot(x2 - x1, y2 - y1)
        if d < 1:
            return
        ux, uy = (x2 - x1) / d, (y2 - y1) / d
        p = (x1 + ux * (r + 4), y1 + uy * (r + 4), x2 - ux * (r + 8), y2 - uy * (r + 8))
        start = f' marker-start="url(#ah-{s["id"]})"' if both else ''
        out.append(f'<line x1="{p[0]:.0f}" y1="{p[1]:.0f}" x2="{p[2]:.0f}" y2="{p[3]:.0f}" class="ln"'
                   f' marker-end="url(#ah-{s["id"]})"{start}/>')

    for a, b in s['beats']:
        arrow(a, b)
    for a, b in s['mutual']:
        if a != b:
            arrow(a, b, True)
    for k, name, color in kinds:
        x, y = pos[k]
        out.append(f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{r}" fill="{color}"/>'
                   f'<text x="{x:.0f}" y="{y + 6:.0f}" class="nm">{name}</text>')
    out.append('</svg>')
    return ''.join(out)


def chips(keys):
    cells = []
    for k in keys:
        src = sprite(k)
        img = f'<img src="{src}" alt="">' if src else '<span class="noimg"></span>'
        cells.append(f'<figure class="unit">{img}<figcaption>{NAME.get(k, k)}</figcaption></figure>')
    return ''.join(cells)


def section(s):
    rows = []
    for k, name, color in s['kinds']:
        rows.append(
            f'<div class="kind"><div class="tag" style="--c:{color}">{name}</div>'
            f'<div class="col"><div class="lab">몸 {len(s["hosts"].get(k, []))}</div>'
            f'<div class="units">{chips(s["hosts"].get(k, []))}</div></div>'
            f'<div class="col"><div class="lab">적 · 보스</div>'
            f'<div class="units">{chips(s["foes"].get(k, []))}</div></div></div>')
    badge = f'<span class="pick">{s["verdict"]}</span>' if s['verdict'] else ''
    li = lambda xs: ''.join(f'<li>{x}</li>' for x in xs)
    return f'''
<section id="{s['id']}">
  <h2>{s['title']} {badge}</h2>
  <p class="lede">{s['sub']}</p>
  <div class="top">
    <div class="dia">{diagram(s)}</div>
    <div class="rule"><h3>누가 누구를 이기나</h3><ul>{li(s['why'])}</ul>
      <div class="pc"><div><h3>좋은 점</h3><ul>{li(s['good'])}</ul></div>
      <div><h3>걸리는 점</h3><ul>{li(s['bad'])}</ul></div></div></div>
  </div>
  <h3>누가 어디에 들어가나</h3>
  <div class="kinds">{''.join(rows)}</div>
</section>'''


CSS = '''
:root{--ground:#F2F4F6;--surface:#FFFFFF;--surface2:#E6EAEE;--ink:#151B22;--muted:#5B6672;--line:#D2D9E0;--accent:#1F6FD0;--pick:#0B7F70;--pickbg:#D6EFEA}
@media (prefers-color-scheme: dark){:root:not([data-theme="light"]){--ground:#0E1318;--surface:#171E26;--surface2:#202A34;--ink:#E4EAF0;--muted:#93A0AD;--line:#2B3743;--accent:#6FB1FF;--pick:#56D3BF;--pickbg:#12352F}}
:root[data-theme="dark"]{--ground:#0E1318;--surface:#171E26;--surface2:#202A34;--ink:#E4EAF0;--muted:#93A0AD;--line:#2B3743;--accent:#6FB1FF;--pick:#56D3BF;--pickbg:#12352F}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--ink);font-family:"IBM Plex Sans KR","Malgun Gothic","Apple SD Gothic Neo",sans-serif;font-size:15px;line-height:1.6}
main{max-width:1080px;margin:0 auto;padding:24px 16px 80px}
h1{font-family:"Do Hyeon",sans-serif;font-weight:400;font-size:32px;line-height:1.2;margin:0 0 8px;text-wrap:balance}
h2{font-family:"Do Hyeon",sans-serif;font-weight:400;font-size:25px;margin:0 0 2px;display:flex;align-items:center;gap:10px;flex-wrap:wrap}
h3{font-size:14px;font-weight:600;margin:14px 0 6px;color:var(--muted);letter-spacing:.02em}
p{margin:0 0 10px;max-width:70ch}
.lede{color:var(--muted)}
ul{margin:0 0 8px;padding-left:20px}
li{margin:2px 0}
nav{display:flex;gap:6px;flex-wrap:wrap;margin:14px 0 26px}
nav a{padding:6px 12px;border:1px solid var(--line);border-radius:999px;background:var(--surface);color:var(--ink);text-decoration:none;font-size:13.5px}
nav a:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
section{background:var(--surface);border:1px solid var(--line);border-radius:14px;padding:20px;margin:0 0 22px;scroll-margin-top:12px}
.pick{font-family:"IBM Plex Sans KR",sans-serif;font-size:12.5px;font-weight:600;color:var(--pick);background:var(--pickbg);padding:2px 10px;border-radius:999px}
.top{display:grid;grid-template-columns:minmax(0,380px) minmax(0,1fr);gap:20px;align-items:start;margin-top:8px}
.dia svg{width:100%;height:auto;display:block}
.ln{stroke:var(--ink);stroke-width:3;stroke-linecap:round}
.ah{fill:var(--ink)}
.nm{fill:#fff;font-family:"Do Hyeon",sans-serif;font-size:20px;text-anchor:middle;paint-order:stroke;stroke:rgba(0,0,0,.45);stroke-width:3px}
.pc{display:grid;grid-template-columns:1fr 1fr;gap:16px}
.kinds{display:flex;flex-direction:column;gap:8px}
.kind{display:grid;grid-template-columns:72px minmax(0,1.6fr) minmax(0,1fr);gap:12px;align-items:start;background:var(--surface2);border-radius:10px;padding:10px}
.tag{background:var(--c);color:#fff;font-family:"Do Hyeon",sans-serif;font-size:18px;text-align:center;border-radius:8px;padding:10px 4px;text-shadow:0 1px 2px rgba(0,0,0,.45)}
.lab{font-size:12px;color:var(--muted);margin-bottom:4px}
.units{display:flex;flex-wrap:wrap;gap:6px}
.unit{margin:0;width:62px;text-align:center}
.unit img,.noimg{width:56px;height:56px;object-fit:contain;image-rendering:pixelated;background:var(--surface);border-radius:8px;display:block;margin:0 auto}
.unit figcaption{font-size:11px;line-height:1.25;color:var(--muted);margin-top:2px}
.cmp{overflow-x:auto;border:1px solid var(--line);border-radius:10px;background:var(--surface)}
table{border-collapse:collapse;width:100%;font-size:14px}
th,td{padding:8px 11px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}
th{background:var(--surface2);font-size:12.5px;white-space:nowrap}
tr:last-child td{border-bottom:0}
@media (max-width:760px){.top{grid-template-columns:minmax(0,1fr)}.pc{grid-template-columns:minmax(0,1fr)}.kind{grid-template-columns:minmax(0,1fr)}.tag{padding:6px}}
'''

HTML = f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>AVSR 상성 체계 후보</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Do+Hyeon&family=IBM+Plex+Sans+KR:wght@400;500;600&display=swap">
<style>{CSS}</style></head><body><main>
<h1>상성 체계 후보</h1>
<p class="lede">상성을 「무엇과 무엇의 관계」로 보여 줄 것인가. 네 가지 틀을 같은 호스트 23명과 같은 적으로 나눠 봤습니다. 그림은 게임에 지금 들어 있는 캐릭터입니다. 누가 어디에 들어가는지는 초안이고, 틀이 정해지면 하나씩 다시 맞춥니다.</p>
<nav aria-label="후보">{''.join(f'<a href="#{s["id"]}">{s["title"]}</a>' for s in SCHEMES)}<a href="#cmp">한눈에 견주기</a></nav>
{''.join(section(s) for s in SCHEMES)}
<section id="cmp"><h2>한눈에 견주기</h2>
<div class="cmp"><table><thead><tr><th>틀</th><th>갈래 수</th><th>설명 없이 통하나</th><th>화면에서 보기 좋나</th><th>지금 호스트에 맞나</th></tr></thead><tbody>
<tr><td>안 1 · 다섯 속성</td><td>5</td><td>가장 잘 통함</td><td>색 다섯 + 불 · 물 기운</td><td>총잡이는 억지가 조금 있음</td></tr>
<tr><td>안 2 · 네 원소</td><td>4</td><td>물 → 불만 통함</td><td>색 넷 + 원소 기운</td><td>총잡이는 억지가 조금 있음</td></tr>
<tr><td>안 3 · 세 갈래</td><td>3</td><td>한 번 알려 줘야 함</td><td>색 셋뿐</td><td>생김새와 잘 맞음</td></tr>
<tr><td>안 4 · 혼의 색</td><td>3 + 1</td><td>배워야 함</td><td>색 넷 + 혼의 기운</td><td>성격으로 나눠 자연스러움</td></tr>
</tbody></table></div></section>
</main></body></html>'''

with open(OUT, 'w', encoding='utf-8') as f:
    f.write(HTML)
print(OUT, len(HTML) // 1024, 'KB')
missing = [k for s in SCHEMES for grp in (s['hosts'], s['foes']) for ks in grp.values() for k in ks if not sprite(k)]
print('그림 없는 것', sorted(set(missing)))
for s in SCHEMES:
    hs = [k for ks in s['hosts'].values() for k in ks]
    print(s['id'], len(hs), len(set(hs)))
