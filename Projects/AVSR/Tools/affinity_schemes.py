# -*- coding: utf-8 -*-
"""상성 체계 후보를 한 장으로 견주는 문서를 만든다 (AVSR_AffinitySchemes.html).

2026-10-02 다시 씀 — 불·물 같은 속성을 호스트에 억지로 붙이는 안은 반려됐다.
**지금 있는 호스트 23명이 이미 가진 것**(무기 · 능력치 · 사거리 · 정체)에서 출발하는 틀만 남긴다.
그림은 새로 그리지 않는다 — 게임에 들어 있는 스프라이트를 그대로 넣는다.
"""
import base64, os

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

# 이 몸이 실제로 무엇으로 싸우는가 — 틀을 고르는 근거(게임 데이터 그대로)
WEAPON = [
    ('gangster', '권총', '표식 · 처형'), ('thug', '권총', '난사'), ('hopper', '소총', '정조준(치명타)'),
    ('hopper_smg', '기관단총', '도약 강습'), ('commando_mg', '기관총', '방벽 · 중장갑'),
    ('commando_laser', '레이저 총', '연쇄 방전 · 지형 관통'), ('commando_grenade', '수류탄', '융단 폭격'),
    ('commando_missile', '유도 미사일', '다중 유도'), ('amazon', '창', '도약 강타'),
    ('amazon_elite', '창', '(스킬 미정)'), ('ninja', '표창', '분신'), ('ninja_chain', '사슬낫', '결박 · 회피'),
    ('baseball', '방망이', '탄 반사'), ('guru', '맨손 장법', '결계 · 지형 통과'),
    ('robot', '빔', '포탑 설치'), ('salamander', '독 뿜기', '중독'), ('dragoon', '불 뿜기', '불바다 · 화상'),
    ('dragon_blue', '번개', '낙뢰 · 둔화'), ('snowwoman', '냉기', '얼음 감옥 · 빙결'),
    ('vampire', '흡혈', '체력 회복'), ('white_wizard', '빛 구슬', '광휘 확산'),
    ('medium', '어둠 구슬', '골렘 · 해골 소환'), ('death', '큰 낫', '즉사 · 해골 소환'),
]


def sprite(key):
    for suffix in ('_s', ''):
        p = os.path.join(UNIT, key, f'unit_{key}{suffix}.png')
        if os.path.exists(p):
            with open(p, 'rb') as f:
                return 'data:image/png;base64,' + base64.b64encode(f.read()).decode()
    return ''


SCHEMES = [
    dict(id='material', verdict='추천',
         title='안 1 · 무엇으로 때리나 × 무엇으로 되어 있나',
         sub='몸은 무기의 성질 셋, 적은 몸의 재질 셋. 같은 색끼리 통한다.',
         mine=[('blade', '날', '#C99A1E', '총알 · 칼 · 창 · 표창 — 꿰뚫는다'),
               ('force', '힘', '#E0562E', '방망이 · 장법 · 폭발 · 중화기 — 부순다'),
               ('magic', '술', '#8A4FD0', '불 · 얼음 · 번개 · 독 · 빛 · 어둠 — 태운다')],
         theirs=[('flesh', '살', '#C99A1E', '사람 · 짐승'),
                 ('metal', '쇠', '#E0562E', '기계 · 갑옷'),
                 ('spirit', '혼', '#8A4FD0', '망자 · 괴이')],
         pairs=[('blade', 'flesh', '총칼은 살을 꿰뚫는다'), ('force', 'metal', '폭발과 망치는 쇠를 부순다'),
                ('magic', 'spirit', '불과 주술은 망령을 태운다')],
         hosts={'blade': ['gangster', 'thug', 'hopper', 'hopper_smg', 'commando_mg', 'ninja', 'amazon', 'amazon_elite'],
                'force': ['baseball', 'guru', 'ninja_chain', 'commando_grenade', 'commando_missile', 'commando_laser', 'robot'],
                'magic': ['dragoon', 'snowwoman', 'dragon_blue', 'salamander', 'vampire', 'white_wizard', 'medium', 'death']},
         foes={'flesh': ['bat', 'actor_enforcer', 'python', 'kingpin'],
               'metal': ['scrapgunner', 'roadwarden', 'coilwalker', 'robot_snakes', 'crusher', 'guardian'],
               'spirit': ['skeleton', 'sludge']},
         show=['맞을 때 — 살: 깊게 박히며 붉은 섬광 · 쇠: 찌그러지며 파편이 튐 · 혼: 타오르며 흩어짐',
               '안 맞을 때 — 총알이 쇠에 「팅」 튕김 · 칼이 망령을 스르륵 지나감 · 주술이 쇠 위에서 꺼짐',
               '적의 재질은 생김새에 이미 있다(해골은 뼈, 포탑은 쇠) — 아이콘 없이도 읽힌다'],
         good=['호스트가 **지금 들고 있는 무기 그대로** 나눈 것이라 억지가 없다',
               '세 문장이 다 상식이다 — 「총알은 쇠에 튕긴다」는 설명이 필요 없다',
               '같은 색끼리 통한다 — 돌고 도는 관계(누가 누구를 이기나)를 외울 필요가 없다',
               '맞는 느낌과 안 맞는 느낌을 재질마다 다른 이펙트로 줄 수 있다'],
         bad=['「혼」 쪽 적이 지금 해골 · 슬러지 둘뿐이다 — 새 잡몹을 넣을 때 이쪽을 채워야 한다',
              '경계에 선 몸이 있다: 사신(낫인데 주술 쪽에 둠) · 레이저 코만도(빛인데 화력 쪽에 둠)']),
    dict(id='weight', verdict='',
         title='안 2 · 한 방 · 연타 · 넓게',
         sub='표를 따로 두지 않는다. 몸의 능력치(한 발의 무게 · 빠르기 · 범위)가 곧 상성이다.',
         mine=[('heavy', '한 방', '#E0562E', '느리지만 한 발이 무겁다'),
               ('rapid', '연타', '#C99A1E', '빠르게 여러 번 때린다'),
               ('wide', '넓게', '#2F8FE0', '주위를 한꺼번에 친다')],
         theirs=[('armor', '갑옷', '#E0562E', '맞을 때마다 피해를 덜어 낸다'),
                 ('swift', '날쌘 것', '#C99A1E', '작고 빨라 잘 안 맞는다'),
                 ('swarm', '떼', '#2F8FE0', '약하지만 여럿이 몰려온다')],
         pairs=[('heavy', 'armor', '무거운 한 방은 갑옷을 뚫는다'), ('rapid', 'swift', '쏟아부으면 날쌘 것도 맞는다'),
                ('wide', 'swarm', '넓게 치면 떼가 쓸린다')],
         hosts={'heavy': ['commando_grenade', 'commando_missile', 'dragoon', 'death', 'commando_laser', 'white_wizard',
                          'medium', 'amazon_elite', 'robot', 'salamander'],
                'rapid': ['hopper_smg', 'commando_mg', 'thug', 'ninja', 'gangster', 'hopper', 'dragon_blue', 'vampire'],
                'wide': ['guru', 'ninja_chain', 'baseball', 'amazon', 'snowwoman']},
         foes={'armor': ['actor_enforcer', 'crusher', 'guardian', 'robot_snakes'],
               'swift': ['bat', 'coilwalker', 'kingpin'],
               'swarm': ['skeleton', 'scrapgunner', 'roadwarden', 'python', 'sludge']},
         show=['갑옷에 약한 연타 — 숫자가 1 · 1 · 1 로 뜨고 불똥만 튄다. 무거운 한 방 — 갑옷이 깨지며 큰 숫자',
               '떼에 한 방 — 한 마리만 죽고 나머지가 덮친다. 넓게 — 여럿이 한꺼번에 날아간다',
               '「갑옷은 맞을 때마다 일정량을 덜어 낸다」는 규칙 하나면 표 없이 저절로 생긴다'],
         good=['외울 표가 없다 — 능력치가 그대로 상성이 된다', '호스트 능력치 강화(공격 · 공속)와 바로 이어진다',
               '잘 키운 몸은 숫자가 커져 갑옷도 그냥 뚫는다 — 「키우면 상성 무시」가 저절로 된다'],
         bad=['중간인 몸이 많다 — 23명 중 절반쯤은 한 방도 연타도 아니다(한 발 피해 13 안팎)',
              '적 그림만 보고 갑옷 · 날쌘 것 · 떼가 읽혀야 한다 — 지금 잡몹 7종으로는 구별이 약하다',
              '색이나 기운 같은 「보는 맛」이 없다']),
    dict(id='range', verdict='',
         title='안 3 · 거리',
         sub='이미 화면에 있는 직업 셋(근거리 · 중거리 · 원거리)을 그대로 쓴다.',
         mine=[('melee', '근거리', '#E0562E', '붙어서 때린다'),
               ('mid', '중거리', '#C99A1E', '가까이서 쏟아붓는다'),
               ('far', '원거리', '#2F8FE0', '멀리서 쏜다')],
         theirs=[('shooter', '쏘는 적', '#E0562E', '멀리서 쏘고 물러난다'),
                 ('holder', '버티는 적', '#C99A1E', '제자리에서 버틴다'),
                 ('rusher', '달려드는 적', '#2F8FE0', '붙으려고 온다')],
         pairs=[('melee', 'shooter', '붙으면 쏘는 놈은 끝이다'), ('mid', 'holder', '버티는 놈은 가까이서 갈아 낸다'),
                ('far', 'rusher', '오기 전에 잡는다')],
         hosts={'melee': ['amazon', 'amazon_elite', 'guru', 'ninja_chain', 'baseball', 'death'],
                'mid': ['thug', 'hopper_smg', 'commando_mg'],
                'far': ['gangster', 'hopper', 'commando_laser', 'commando_grenade', 'commando_missile', 'salamander',
                        'dragoon', 'dragon_blue', 'white_wizard', 'medium', 'ninja', 'robot', 'snowwoman', 'vampire']},
         foes={'shooter': ['scrapgunner', 'roadwarden', 'coilwalker'],
               'holder': ['crusher', 'guardian', 'robot_snakes', 'sludge'],
               'rusher': ['skeleton', 'bat', 'actor_enforcer', 'python', 'kingpin']},
         show=['적의 **행동**이 곧 표시다 — 쏘는지, 달려드는지는 보면 안다',
               '지금도 원거리 몸이 근접 몹을 맞히면 경직이 걸린다 — 절반은 이미 들어 있다'],
         good=['새로 정할 것이 없다 — 직업 표시가 이미 호스트 카드에 있다', '행동으로 읽혀서 표시가 거의 필요 없다'],
         bad=['**치우침이 심하다** — 근거리 6 · 중거리 3 · 원거리 14. 대부분의 몸이 같은 편이다',
              '거리만으로는 「이 몸을 골라야 할 이유」가 얕다 — 원거리 14명끼리는 차이가 없다']),
    dict(id='kin', verdict='',
         title='안 4 · 정체',
         sub='누구인가로 나눈다 — 사람 · 괴수 · 요괴 · 기계.',
         mine=[('human', '사람', '#C99A1E', '갱 · 군인 · 무술가'), ('beast', '괴수', '#3FA34D', '용 · 도마뱀'),
               ('yokai', '요괴', '#8A4FD0', '술사 · 설녀 · 흡혈귀 · 사신'), ('machine', '기계', '#2F8FE0', '로봇')],
         theirs=[('human', '사람', '#3FA34D', ''), ('beast', '괴수', '#8A4FD0', ''),
                 ('yokai', '요괴', '#2F8FE0', ''), ('machine', '기계', '#C99A1E', '')],
         pairs=[('human', 'machine', '사람은 기계를 다룰 줄 안다'), ('beast', 'human', '괴수는 사람을 사냥한다'),
                ('yokai', 'beast', '요괴는 괴수를 홀린다'), ('machine', 'yokai', '기계는 홀리지 않는다')],
         hosts={'human': ['gangster', 'thug', 'hopper', 'hopper_smg', 'commando_mg', 'commando_laser',
                          'commando_grenade', 'commando_missile', 'amazon', 'amazon_elite', 'ninja',
                          'ninja_chain', 'baseball', 'guru'],
                'beast': ['salamander', 'dragoon', 'dragon_blue'],
                'yokai': ['white_wizard', 'medium', 'snowwoman', 'vampire', 'death'],
                'machine': ['robot']},
         foes={'human': ['actor_enforcer', 'kingpin', 'scrapgunner'], 'beast': ['bat', 'python', 'sludge'],
               'yokai': ['skeleton'], 'machine': ['roadwarden', 'coilwalker', 'robot_snakes', 'crusher', 'guardian']},
         show=['생김새가 곧 표시다 — 용은 괴수, 로봇은 기계'],
         good=['그림만 보면 어느 무리인지 안다'],
         bad=['**사람이 14명**이다 — 틀로 쓰기에는 한쪽으로 너무 쏠린다',
              '누가 누구를 이기는지에 이유가 약하다 — 「요괴가 괴수를 이긴다」는 누구도 먼저 떠올리지 못한다',
              '기계 몸이 로봇 하나뿐이다']),
]


def mock(sid, caption='그림으로 그린 시안입니다. 실제 게임 화면이 아닙니다.'):
    """화면 시안(_exchange/in/_mock3_{id}.png)이 있으면 넣는다. 용량을 줄이려고 JPEG 로 바꾼다."""
    p = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'in', f'_mock3_{sid}.png')
    if not os.path.exists(p):
        return ''
    import io
    from PIL import Image
    buf = io.BytesIO()
    Image.open(p).convert('RGB').save(buf, 'JPEG', quality=88)
    src = 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()
    return (f'<figure class="mock"><img src="{src}" alt="화면 시안">'
            f'<figcaption>{caption}</figcaption></figure>')


def chips(keys):
    cells = []
    for k in keys:
        src = sprite(k)
        img = f'<img src="{src}" alt="">' if src else '<span class="noimg"></span>'
        cells.append(f'<figure class="unit">{img}<figcaption>{NAME.get(k, k)}</figcaption></figure>')
    return ''.join(cells)


def md(s):
    """**굵게** 만 지원."""
    out, bold = [], False
    for part in s.split('**'):
        out.append(f'<strong>{part}</strong>' if bold else part)
        bold = not bold
    return ''.join(out)


def section(s):
    mine = {k: (n, c, d) for k, n, c, d in s['mine']}
    theirs = {k: (n, c, d) for k, n, c, d in s['theirs']}
    rows = []
    for a, b, line in s['pairs']:
        mn, mc, mdsc = mine[a]
        tn, tc, tdsc = theirs[b]
        rows.append(f'''
<div class="pair">
  <div class="side">
    <div class="tag" style="--c:{mc}">{mn}</div><div class="desc">{mdsc}</div>
    <div class="lab">몸 {len(s['hosts'].get(a, []))}명</div><div class="units">{chips(s['hosts'].get(a, []))}</div>
  </div>
  <div class="mid"><div class="arrow" aria-hidden="true">→</div><div class="line">{line}</div></div>
  <div class="side">
    <div class="tag" style="--c:{tc}">{tn}</div><div class="desc">{tdsc}</div>
    <div class="lab">적 · 보스 {len(s['foes'].get(b, []))}</div><div class="units">{chips(s['foes'].get(b, []))}</div>
  </div>
</div>''')
    badge = f'<span class="pick">{s["verdict"]}</span>' if s['verdict'] else ''
    li = lambda xs: ''.join(f'<li>{md(x)}</li>' for x in xs)
    return f'''
<section id="{s['id']}">
  <h2>{s['title']} {badge}</h2>
  <p class="lede">{s['sub']}</p>
  <div class="pairs">{''.join(rows)}</div>
  <div class="look"><div><h3>화면에서 어떻게 보이나</h3><ul>{li(s['show'])}</ul></div>
  <div class="mocks">{mock(s['id'])}{mock('terrain', '둘째 겹 — 방의 구조. 총알은 기둥에 막히고, 수류탄은 넘기고, 레이저는 뚫는다') if s['id'] == 'material' else ''}</div></div>
  <div class="pc"><div><h3>좋은 점</h3><ul>{li(s['good'])}</ul></div>
  <div><h3>걸리는 점</h3><ul>{li(s['bad'])}</ul></div></div>
</section>'''


def weapon_table():
    rows = ''.join(
        f'<tr><td><span class="mini"><img src="{sprite(k)}" alt=""></span>{NAME[k]}</td><td>{w}</td><td>{sk}</td></tr>'
        for k, w, sk in WEAPON)
    return f'<div class="cmp"><table><thead><tr><th>호스트</th><th>무엇으로 싸우나</th><th>스킬 · 특성</th></tr></thead><tbody>{rows}</tbody></table></div>'


CSS = '''
:root{--ground:#F2F4F6;--surface:#FFFFFF;--surface2:#E9EDF1;--ink:#151B22;--muted:#5B6672;--line:#D2D9E0;--accent:#1F6FD0;--pick:#0B7F70;--pickbg:#D6EFEA}
@media (prefers-color-scheme: dark){:root:not([data-theme="light"]){--ground:#0E1318;--surface:#171E26;--surface2:#202A34;--ink:#E4EAF0;--muted:#93A0AD;--line:#2B3743;--accent:#6FB1FF;--pick:#56D3BF;--pickbg:#12352F}}
:root[data-theme="dark"]{--ground:#0E1318;--surface:#171E26;--surface2:#202A34;--ink:#E4EAF0;--muted:#93A0AD;--line:#2B3743;--accent:#6FB1FF;--pick:#56D3BF;--pickbg:#12352F}
*{box-sizing:border-box}
body{margin:0;background:var(--ground);color:var(--ink);font-family:"IBM Plex Sans KR","Malgun Gothic","Apple SD Gothic Neo",sans-serif;font-size:15px;line-height:1.6}
main{max-width:1080px;margin:0 auto;padding:24px 16px 80px}
h1{font-family:"Do Hyeon",sans-serif;font-weight:400;font-size:32px;line-height:1.2;margin:0 0 8px;text-wrap:balance}
h2{font-family:"Do Hyeon",sans-serif;font-weight:400;font-size:24px;margin:0 0 2px;display:flex;align-items:center;gap:10px;flex-wrap:wrap}
h3{font-size:13.5px;font-weight:600;margin:16px 0 6px;color:var(--muted);letter-spacing:.02em}
p{margin:0 0 10px;max-width:72ch}
.lede{color:var(--muted)}
ul{margin:0 0 8px;padding-left:20px;max-width:80ch}
li{margin:2px 0}
nav{display:flex;gap:6px;flex-wrap:wrap;margin:14px 0 26px}
nav a{padding:6px 12px;border:1px solid var(--line);border-radius:999px;background:var(--surface);color:var(--ink);text-decoration:none;font-size:13.5px}
nav a:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
section{background:var(--surface);border:1px solid var(--line);border-radius:14px;padding:20px;margin:0 0 22px;scroll-margin-top:12px}
.pick{font-family:"IBM Plex Sans KR",sans-serif;font-size:12.5px;font-weight:600;color:var(--pick);background:var(--pickbg);padding:2px 10px;border-radius:999px}
.pairs{display:flex;flex-direction:column;gap:10px;margin-top:10px}
.pair{display:grid;grid-template-columns:minmax(0,1.5fr) 150px minmax(0,1fr);gap:12px;align-items:center;background:var(--surface2);border-radius:12px;padding:12px}
.side{min-width:0}
.tag{display:inline-block;background:var(--c);color:#fff;font-family:"Do Hyeon",sans-serif;font-size:20px;border-radius:8px;padding:4px 16px;text-shadow:0 1px 2px rgba(0,0,0,.45)}
.desc{display:inline;margin-left:8px;font-size:13px;color:var(--muted)}
.lab{font-size:12px;color:var(--muted);margin:8px 0 4px}
.units{display:flex;flex-wrap:wrap;gap:6px}
.unit{margin:0;width:60px;text-align:center}
.unit img,.noimg{width:54px;height:54px;object-fit:contain;image-rendering:pixelated;background:var(--surface);border-radius:8px;display:block;margin:0 auto}
.unit figcaption{font-size:11px;line-height:1.25;color:var(--muted);margin-top:2px}
.mid{text-align:center}
.arrow{font-size:34px;line-height:1;color:var(--ink)}
.line{font-size:13.5px;font-weight:600;line-height:1.35;margin-top:4px}
.pc{display:grid;grid-template-columns:1fr 1fr;gap:16px}
.mocks{display:flex;flex-wrap:wrap;gap:14px;margin-top:10px}
.mock{margin:0;flex:1 1 300px;max-width:420px}
.mock img{width:100%;height:auto;display:block;border-radius:10px;border:1px solid var(--line)}
.mock figcaption{font-size:12px;color:var(--muted);margin-top:4px}
.cmp{overflow-x:auto;border:1px solid var(--line);border-radius:10px;background:var(--surface)}
table{border-collapse:collapse;width:100%;font-size:14px}
th,td{padding:7px 11px;text-align:left;border-bottom:1px solid var(--line);vertical-align:middle}
th{background:var(--surface2);font-size:12.5px;white-space:nowrap}
tr:last-child td{border-bottom:0}
.mini img{width:30px;height:30px;object-fit:contain;image-rendering:pixelated;vertical-align:middle;margin-right:8px}
@media (max-width:760px){.pair{grid-template-columns:minmax(0,1fr)}.pc{grid-template-columns:minmax(0,1fr)}.arrow{transform:rotate(90deg)}}
'''

HTML = f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>AVSR 상성 체계 후보</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Do+Hyeon&family=IBM+Plex+Sans+KR:wght@400;500;600&display=swap">
<style>{CSS}</style></head><body><main>
<h1>상성 체계 후보</h1>
<p class="lede">불 · 물 같은 속성을 호스트에 덧씌우지 않고, <strong>지금 있는 호스트 23명이 이미 가진 것</strong>에서 상성을 꺼내 봤습니다. 먼저 호스트가 실제로 무엇으로 싸우는지 늘어놓고, 거기서 나오는 틀 넷을 같은 호스트와 같은 적으로 나눠 견줍니다. 누가 어디에 들어가는지는 초안입니다.</p>
<nav aria-label="차례"><a href="#have">호스트가 가진 것</a>{''.join(f'<a href="#{s["id"]}">{s["title"].split(" · ", 1)[0]}</a>' for s in SCHEMES)}<a href="#cmp">한눈에 견주기</a></nav>
<section id="have"><h2>호스트가 가진 것</h2>
<p class="lede">틀을 고르기 전에 본 것. 이 표에서 저절로 갈리는 선이 네 가지였습니다 — 무기의 성질, 한 발의 무게, 사거리, 정체.</p>
{weapon_table()}</section>
{''.join(section(s) for s in SCHEMES)}
<section id="cmp"><h2>한눈에 견주기</h2>
<div class="cmp"><table><thead><tr><th>틀</th><th>몸의 나뉨</th><th>설명 없이 통하나</th><th>화면에서 보이는 것</th><th>가장 큰 걸림돌</th></tr></thead><tbody>
<tr><td>안 1 · 무기 × 재질</td><td>8 · 7 · 8</td><td>통함 (총알은 쇠에 튕긴다)</td><td>재질마다 다른 타격 이펙트, 같은 색 맞추기</td><td>「혼」 쪽 적이 둘뿐</td></tr>
<tr><td>안 2 · 한 방 · 연타 · 넓게</td><td>10 · 8 · 5</td><td>통함 (갑옷엔 한 방)</td><td>숫자 크기와 갑옷 깨짐</td><td>어중간한 몸이 많음</td></tr>
<tr><td>안 3 · 거리</td><td>6 · 3 · 14</td><td>통함 (붙으면 못 쏜다)</td><td>적의 행동</td><td>원거리 14명 쏠림</td></tr>
<tr><td>안 4 · 정체</td><td>14 · 3 · 5 · 1</td><td>이유가 약함</td><td>생김새</td><td>사람 14명 쏠림</td></tr>
</tbody></table></div></section>
</main></body></html>'''

with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write(HTML)
print(OUT, len(HTML) // 1024, 'KB')
for s in SCHEMES:
    hs = [k for ks in s['hosts'].values() for k in ks]
    print(s['id'], len(hs), len(set(hs)), [len(v) for v in s['hosts'].values()],
          '그림 없음', [k for k in hs + [k for ks in s['foes'].values() for k in ks] if not sprite(k)])
