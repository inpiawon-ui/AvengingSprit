"""캐릭터별 액티브 · 필살기 녹화 영상 모음 페이지 (PD 2026-10-16 「다 플레이해서 영상으로 정리해서 내가 볼 수 있게」).

녹화: `SkillReworkRecorder.Batch` → `_exchange/skillfx_now/clip_all_{키}.mp4` (0.6초 액티브 · 필살기 4~4.5초)
결과: `_exchange/doc_skill_video/index.html` + `video/{키}.mp4`(줄인 것) — Artifact 로 올린다.
쓰기: python skill_video_page.py
"""
import html
import os
import subprocess

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
EX = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange')
OUT = os.path.join(EX, 'doc_skill_video')
VID = os.path.join(OUT, 'video')
TSV = os.path.join(ROOT, 'Projects', 'AVSR', 'Localization', 'AVSR_Strings.tsv')
HOST_TABLE = os.path.join(ROOT, 'Assets', 'BundleResource', 'TableData', 'HostTable.asset')

# 등급 순서(위가 먼저) — 등급은 HostTable._grade (0 R · 1 SR · 2 SSR), 사신은 전설(히든)
GRADES = [('SSR', ['amazon_elite', 'dragoon', 'medium', 'baseball', 'vampire']),
          ('SR', ['hopper_smg', 'commando_laser', 'ninja', 'ninja_chain', 'robot', 'commando_missile']),
          ('R', ['gangster', 'thug', 'amazon', 'hopper', 'commando_mg', 'commando_grenade',
                 'salamander', 'dragon_blue', 'guru', 'white_wizard', 'snowwoman']),
          ('전설', ['death'])]

# 녹화 시각(초) — 기본은 액티브 0.6 · 필살기 4.5. 영매만 4.0
ULT_AT = {'medium': 4.0}

# 필살기 설명은 지금 게임에 들어간 연출 그대로 적는다(기획 컨셉 표와 달라진 것이 있다 — 드라군 · 흡혈귀 등)
ULT_DESC = {
    'amazon_elite': '3초 동안 붉은 기운을 두르고 날아온 탄 · 붙은 적을 흡수한다 → 땅이 갈라지며 모은 만큼 붉은 섬광으로 되돌려 터뜨린다.',
    'dragoon': '불기둥 속에서 용이 솟고 올라타 불을 뿜으며 적 무리 한가운데로 돌진한다. 지나간 자리에 불길, 끝에 대형 화염구.',
    'medium': '강령 문양과 빛기둥 속에서 큰 골렘이 솟아 8초 동안 2초마다 푸른 충격파로 내리찍고, 빛 가루로 사라진다.',
    'baseball': '몸을 돌려 회오리를 키우며 바깥 바람 줄기로 적을 끌어모은 뒤 풀스윙으로 날린다.',
    'vampire': '체력을 내주고 적 사이를 붉은 호로 오가며 할퀸다. 피 구슬을 빨아들여 몸 중심에서 터뜨린다.',
    'hopper_smg': '빛기둥과 함께 도약 → 금빛 자국을 끌며 가장 먼 적까지 돌파하며 양옆 난사 → 도착하면 하늘색 보호막.',
    'commando_laser': '총구에 하늘색 기운을 모았다가 굵은 관통 빔. 빔이 지나간 적마다 차례로 전기 폭발, 쏜 뒤 뒤로 밀린다.',
    'ninja': '보라 연막 속에서 보라빛 분신 셋이 서서 6초 동안 같이 표창을 던지고, 분신이 적에게 뛰어들어 벤다.',
    'ninja_chain': '먼 적에게 사슬을 걸고 분홍 섬광 → 날아가 연타 → 분홍 빛기둥으로 내려찍고 땅이 갈라진다(보스를 끌어오지 않는다).',
    'robot': '하늘색 빛기둥과 함께 포탑 셋이 내려앉아 같이 쏘고, 마지막에 미사일 일제 사격.',
    'commando_missile': '쫓을 적마다 붉은 조준 표식 → 6기가 흰 연기 꼬리를 끌며 휘어 날아가 화염구로 터진다.',
    'gangster': '가장 센 적을 붉은 조준경으로 1.5초 조인 뒤 붉은 관통탄 한 발 → 치명 섬광(보스는 최대 체력 % 피해).',
    'thug': '2.5초 동안 돌며 사방으로 쏘고, 멈추며 사방 16발.',
    'amazon': '적 사이를 잔상으로 옮겨 다니며 번개로 치고 내려찍는다. 시전 중 무적.',
    'hopper': '앞 적들에게 붉은 칼날 → 붉은 사슬 고리로 묶음 → 적 사이를 돌진하며 X 자로 벤다(치명 확정).',
    'commando_mg': '하늘색 에너지 방벽이 몸 앞에 붙어 4초 동안 탄을 막고 적을 밀어내다 깨져 흩어진다.',
    'commando_grenade': '반경 7m 안 모든 적에게 폭탄을 붙이고 2초 뒤 연쇄 폭발(그동안 평타 쉼).',
    'salamander': '입에서 독 줄기 → 넓은 독 늪(거품 · 감속) → 늪 곳곳에서 독이 솟구친다.',
    'dragon_blue': '번개 구슬 셋이 몸 둘레를 돌며 적에게 번개를 잇고, 끝에 하늘에서 거대 낙뢰.',
    'guru': '금빛 만다라 결계로 적을 가운데로 끌어모아 묶은 뒤 금빛 폭발.',
    'white_wizard': '몸 둘레에 빛 구슬이 모였다가 금빛 꼬리로 휘어 날아가 맞은 자리마다 반짝 폭발.',
    'snowwoman': '적마다 서리 마법진 → 얼음 결정 감옥에 가둠 → 한꺼번에 깨진다.',
    'death': '해골 다섯이 일어나 같이 싸우고, 4초 뒤 혼불이 적마다 날아가 터진다.',
}


def strings():
    d = {}
    with open(TSV, encoding='utf-8-sig') as f:
        for line in f:
            p = line.rstrip('\r\n').split('\t')
            if len(p) >= 2:
                d[p[0]] = p[1]
    return d


def active_keys():
    s = open(HOST_TABLE, encoding='utf-8').read()
    out = {}
    for b in s.split('- _hostKey: ')[1:]:
        key = b.split('\n')[0].strip()
        i = b.find('_activeSkillKey: ')
        if i >= 0:
            out[key] = b[i + 17:].split('\n')[0].strip()
    return out


def shrink(key):
    src = os.path.join(EX, 'skillfx_now', f'clip_all_{key}.mp4')
    dst = os.path.join(VID, f'{key}.mp4')
    if not os.path.exists(src):
        return False
    # 세로 720x1280 → 360x640 · H.264 · 소리 없음 — 23개를 한 번에 올려도 한도(64MB) 안에 든다
    subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', src, '-vf', 'scale=360:-2:flags=lanczos',
                    '-c:v', 'libx264', '-preset', 'slow', '-crf', '28', '-pix_fmt', 'yuv420p',
                    '-movflags', '+faststart', '-an', dst], check=True)
    return True


def main():
    os.makedirs(VID, exist_ok=True)
    s = strings()
    act = active_keys()
    cards = []
    total = 0
    for grade, keys in GRADES:
        for k in keys:
            ok = shrink(k)
            if ok:
                total += os.path.getsize(os.path.join(VID, f'{k}.mp4'))
            ak = act.get(k, '')
            cards.append(dict(
                key=k, grade=grade, ok=ok,
                name=s.get(f'host.{k}.name', k),
                aname=s.get(f'askill.{ak}.name', '—'), adesc=s.get(f'askill.{ak}.desc', ''),
                uname=s.get(f'uskill.{k}.name', '—'), udesc=ULT_DESC.get(k, ''),
                ult=ULT_AT.get(k, 4.5)))
    page = render(cards)
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf-8').write(page)
    print('cards', len(cards), 'missing', [c['key'] for c in cards if not c['ok']], 'video MB', round(total / 1e6, 1))


GRADE_CLASS = {'SSR': 'ssr', 'SR': 'sr', 'R': 'r', '전설': 'lg'}


def render(cards):
    e = html.escape
    counts = {}
    for c in cards:
        counts[c['grade']] = counts.get(c['grade'], 0) + 1
    tabs = ['<button class="tab" data-g="all" aria-pressed="true">전체 <b>23</b></button>']
    for g, _ in GRADES:
        tabs.append(f'<button class="tab" data-g="{GRADE_CLASS[g]}" aria-pressed="false">{e(g)} <b>{counts.get(g, 0)}</b></button>')
    items = []
    for c in cards:
        g = GRADE_CLASS[c['grade']]
        video = (f'<video src="video/{c["key"]}.mp4" muted loop playsinline preload="metadata" '
                 f'controls aria-label="{e(c["name"])} 액티브 · 필살기 녹화"></video>'
                 if c['ok'] else '<div class="novid">녹화 없음</div>')
        items.append(f'''
<article class="card" data-g="{g}" id="{c['key']}">
  <div class="screen">{video}</div>
  <div class="info">
    <header><span class="badge {g}">{e(c['grade'])}</span><h2>{e(c['name'])}</h2></header>
    <button class="skill" data-t="0.4" type="button">
      <span class="kind">액티브 · 0:00.6</span><strong>{e(c['aname'])}</strong><span class="desc">{e(c['adesc'])}</span>
    </button>
    <button class="skill ult" data-t="{c['ult'] - 0.3:.1f}" type="button">
      <span class="kind">필살기 · 0:0{c['ult']:.1f}</span><strong>{e(c['uname'])}</strong><span class="desc">{e(c['udesc'])}</span>
    </button>
  </div>
</article>''')
    return TEMPLATE.replace('{{TABS}}', '\n'.join(tabs)).replace('{{CARDS}}', '\n'.join(items))


TEMPLATE = r'''<title>스킬 녹화 23인</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Black+Han+Sans&family=Noto+Sans+KR:wght@400;600;800&family=JetBrains+Mono:wght@500&display=swap">
<style>
/* 레이아웃: 게임 빌드 점검 콘솔 — 위에 등급 필터, 아래로 세로 화면(9:16) 카드가 흐른다. 카드 = 녹화 + 스킬 두 줄(누르면 그 장면으로) */
:root {
  --bg: #eef0f6; --panel: #ffffff; --ink: #161a2a; --mute: #5b6178; --line: #d7dbe7;
  --accent: #4b54d6; --ssr: #b98411; --sr: #8a43c9; --r: #3f6fae; --lg: #c23b48;
  --f-display: "Black Han Sans", "Noto Sans KR", sans-serif;
  --f-body: "Noto Sans KR", "Apple SD Gothic Neo", "Malgun Gothic", sans-serif;
  --f-mono: "JetBrains Mono", ui-monospace, Consolas, monospace;
  color-scheme: light;
}
@media (prefers-color-scheme: dark) { :root:not([data-theme="light"]) {
  --bg: #0f1120; --panel: #181b2e; --ink: #e9ebf5; --mute: #9aa0bd; --line: #2b2f48;
  --accent: #8d95ff; --ssr: #f0c14b; --sr: #c38cff; --r: #7fb0ff; --lg: #ff7480; color-scheme: dark; } }
:root[data-theme="dark"] {
  --bg: #0f1120; --panel: #181b2e; --ink: #e9ebf5; --mute: #9aa0bd; --line: #2b2f48;
  --accent: #8d95ff; --ssr: #f0c14b; --sr: #c38cff; --r: #7fb0ff; --lg: #ff7480; color-scheme: dark; }
* { box-sizing: border-box; }
body { background: var(--bg); color: var(--ink); font-family: var(--f-body); font-size: 15px; line-height: 1.55; }
.wrap { max-width: 1240px; margin: 0 auto; padding-inline: 16px; padding-block: 28px 56px; display: grid; gap: 22px; }
.top { display: grid; gap: 10px; }
h1 { font-family: var(--f-display); font-weight: 400; font-size: clamp(28px, 4vw, 40px); line-height: 1.1; margin: 0; text-wrap: balance; letter-spacing: .01em; }
.lede { margin: 0; color: var(--mute); max-width: 68ch; }
.tabs { display: flex; flex-wrap: wrap; gap: 8px; position: sticky; top: env(safe-area-inset-top, 0px); padding-block: 10px; background: var(--bg); z-index: 2; }
.tab { font: 600 14px var(--f-body); color: var(--ink); background: var(--panel); border: 1px solid var(--line); border-radius: 999px; padding: 6px 14px; cursor: pointer; }
.tab b { font-family: var(--f-mono); font-weight: 500; color: var(--mute); margin-left: 4px; }
.tab[aria-pressed="true"] { background: var(--ink); color: var(--bg); border-color: var(--ink); }
.tab[aria-pressed="true"] b { color: var(--bg); opacity: .7; }
.tab:focus-visible, .skill:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 360px), 1fr)); gap: 18px; }
.card { display: grid; grid-template-columns: 170px minmax(0, 1fr); gap: 14px; background: var(--panel); border: 1px solid var(--line); border-radius: 10px; padding: 12px; }
.screen { aspect-ratio: 9 / 16; max-width: 100%; background: #000; border-radius: 6px; overflow: hidden; }
.screen video { width: 100%; height: 100%; object-fit: cover; display: block; }
.novid { height: 100%; display: grid; place-items: center; color: #9aa0bd; font-size: 13px; }
.info { display: grid; align-content: start; gap: 10px; min-width: 0; }
.info header { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
h2 { font-family: var(--f-display); font-weight: 400; font-size: 22px; margin: 0; line-height: 1.15; }
.badge { font: 500 11px var(--f-mono); letter-spacing: .08em; padding: 2px 7px; border-radius: 4px; border: 1px solid currentColor; }
.badge.ssr { color: var(--ssr); } .badge.sr { color: var(--sr); } .badge.r { color: var(--r); } .badge.lg { color: var(--lg); }
.skill { all: unset; display: grid; gap: 2px; padding: 8px 10px; border-radius: 6px; border: 1px solid var(--line); cursor: pointer; }
.skill:hover { border-color: var(--accent); }
.skill.ult { border-color: color-mix(in srgb, var(--accent) 45%, var(--line)); }
.kind { font: 500 11px var(--f-mono); letter-spacing: .06em; color: var(--mute); text-transform: uppercase; }
.skill strong { font-size: 15px; font-weight: 800; }
.desc { font-size: 13px; color: var(--mute); }
.foot { color: var(--mute); font-size: 13px; }
@media (max-width: 460px) { .card { grid-template-columns: 1fr; } .screen { max-width: 260px; margin-inline: auto; width: 100%; } }
</style>
<div class="wrap">
  <div class="top">
    <h1>캐릭터별 액티브 · 필살기</h1>
    <p class="lede">23명을 1챕터에서 한 번씩 플레이한 녹화입니다. 판마다 0.6초에 액티브, 4.5초(영매 4초)에 필살기를 씁니다. 스킬 이름을 누르면 영상이 그 장면으로 갑니다. 녹화 중에는 내 몸이 무적이고, 연출이 잘 보이게 적을 8마리 더 세우고 체력을 4배로 올렸습니다.</p>
  </div>
  <nav class="tabs" aria-label="등급">
{{TABS}}
  </nav>
  <section class="grid">
{{CARDS}}
  </section>
  <p class="foot">녹화 2026-10-16 · 에디터 · 1챕터 · 숙련도 10 시험 세이브</p>
</div>
<script>
(function () {
  var tabs = document.querySelectorAll('.tab');
  var cards = document.querySelectorAll('.card');
  function show(g) {
    tabs.forEach(function (t) { t.setAttribute('aria-pressed', t.dataset.g === g ? 'true' : 'false'); });
    cards.forEach(function (c) { c.hidden = !(g === 'all' || c.dataset.g === g); });
    try { localStorage.setItem('skillvid.tab', g); } catch (e) {}
  }
  tabs.forEach(function (t) { t.addEventListener('click', function () { show(t.dataset.g); }); });
  try { var saved = localStorage.getItem('skillvid.tab'); if (saved) show(saved); } catch (e) {}
  document.querySelectorAll('.skill').forEach(function (b) {
    b.addEventListener('click', function () {
      var v = b.closest('.card').querySelector('video');
      if (!v) return;
      v.currentTime = parseFloat(b.dataset.t) || 0;
      var p = v.play(); if (p && p.catch) p.catch(function () {});
    });
  });
  // 화면에 보이는 카드만 돌린다 — 23개를 한꺼번에 돌리면 휴대폰이 버벅인다
  if ('IntersectionObserver' in window) {
    var io = new IntersectionObserver(function (es) {
      es.forEach(function (x) {
        var v = x.target.querySelector('video'); if (!v) return;
        if (x.isIntersecting) { var p = v.play(); if (p && p.catch) p.catch(function () {}); } else v.pause();
      });
    }, { threshold: 0.4 });
    cards.forEach(function (c) { io.observe(c); });
  }
})();
</script>
'''

if __name__ == '__main__':
    main()
