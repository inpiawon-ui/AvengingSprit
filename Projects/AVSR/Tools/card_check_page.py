"""1단계 레벨업 카드 녹화 검수 페이지 (PD 2026-10-21 「카드별 녹화 진행해줘」).

Editor.log 의 `[CARD]` 줄(CardCheckRecorder 가 남김)과 skillfx_now/clip_card_*.mp4 를 모아
plan_content_expand/step1_cards_play/ 에 영상 · 프레임 띠 · index.html 을 만든다.
쓰기: python Projects/AVSR/Tools/card_check_page.py [notes.json]
  notes.json = {"c014": "확인한 것 한 줄", ...} — 내가 영상을 보고 적은 판정
"""
import html, json, os, re, shutil, subprocess, sys

ROOT = r'C:\won\UnityProject\AvengingSprit'
CLIPS = os.path.join(ROOT, r'Projects\AVSR\_exchange\skillfx_now')
OUT = os.path.join(ROOT, r'Projects\AVSR\_exchange\plan_content_expand\step1_cards_play')
LOG = os.path.join(os.environ['LOCALAPPDATA'], r'Unity\Editor\Editor.log')

RARITY = {'Common': '커먼', 'Rare': '레어', 'Epic': '에픽', 'Legendary': '레전더리'}
RCOL = {'Common': '#9aa4b2', 'Rare': '#4aa3ff', 'Epic': '#b46bff', 'Legendary': '#ffb02e'}


def read_log():
    """카드마다 마지막 기록만 — 같은 카드를 다시 찍었으면 새 것이 이긴다."""
    lines = open(LOG, encoding='utf-8', errors='replace').read().splitlines()
    info = {}
    for i, l in enumerate(lines):
        m = re.match(r'\[CARD\] (c\d+) (.+?) host=(\S+) body=(\S+) lv=(\d+)', l)
        if m:
            after = ''
            for k in range(i + 1, min(i + 4, len(lines))):
                if lines[k].startswith('  후:'):
                    after = lines[k][4:].strip()
            info[m.group(1)] = dict(name=m.group(2), host=m.group(3), body=m.group(4), lv=m.group(5), diff=after)
        m = re.match(r'\[CARD\] done (c\d+) casts=(\d+) hostHp=(\S+)', l)
        if m and m.group(1) in info:
            info[m.group(1)].update(casts=m.group(2), hp=m.group(3))
    return info


def card_meta():
    """카드 등급 · 설명 — 언어팩(한국어)에서."""
    tsv = os.path.join(ROOT, r'Projects\AVSR\Localization\AVSR_Strings.tsv')
    desc = {}
    for l in open(tsv, encoding='utf-8-sig').read().splitlines():
        f = l.split('\t')
        if len(f) > 1 and f[0].startswith('card.') and f[0].endswith('.desc'):
            desc[f[0].split('.')[1]] = f[1]
    return desc


def main():
    notes = {}
    if len(sys.argv) > 1 and os.path.exists(sys.argv[1]):
        notes = json.load(open(sys.argv[1], encoding='utf-8'))
    rarity = {}
    rp = os.path.join(OUT, 'rarity.json')
    if os.path.exists(rp):
        rarity = json.load(open(rp, encoding='utf-8'))
    os.makedirs(OUT, exist_ok=True)
    info = read_log()
    desc = card_meta()
    cards = sorted(k for k in info if os.path.exists(os.path.join(CLIPS, f'clip_card_{k}.mp4')))
    for k in cards:
        src = os.path.join(CLIPS, f'clip_card_{k}.mp4')
        dst = os.path.join(OUT, f'{k}.mp4')
        if not os.path.exists(dst) or os.path.getmtime(src) > os.path.getmtime(dst):
            shutil.copy2(src, dst)
        strip = os.path.join(OUT, f'{k}_strip.png')
        if not os.path.exists(strip) or os.path.getmtime(src) > os.path.getmtime(strip):
            # 고른 뒤 장면만 — 2.5초부터 1초 간격 6장
            subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-ss', '2.5', '-i', src,
                            '-vf', 'fps=1,scale=200:-1,tile=6x1', '-frames:v', '1', strip], check=False)

    rows = []
    for k in cards:
        c = info[k]
        r = rarity.get(k, '')
        note = notes.get(k, '')
        warn = c['body'] != 'True'
        rows.append(f'''
<article class="card{' warn' if warn else ''}" id="{k}">
  <header><span class="id">{k.upper()}</span><h2>{html.escape(c['name'])}</h2>
    <span class="rar" style="--rc:{RCOL.get(r, '#888')}">{RARITY.get(r, r)}</span></header>
  <p class="desc">{html.escape(desc.get(k, ''))}</p>
  <video src="{k}.mp4" controls muted loop preload="metadata"></video>
  <dl>
    <dt>몸</dt><dd>{html.escape(c['host'])}{'' if not warn else ' — ⚠ 몸 조건이 안 맞음'}</dd>
    <dt>레벨</dt><dd>Lv.{c['lv']}</dd>
    <dt>바뀐 수치</dt><dd class="mono">{html.escape(c['diff'])}</dd>
    {f'<dt>확인</dt><dd class="note">{html.escape(note)}</dd>' if note else ''}
  </dl>
</article>''')

    page = f'''<!doctype html><html lang="ko"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>레벨업 카드 검수 영상</title>
<style>
:root{{--bg:#11141c;--panel:#1a1f2b;--fg:#e6ebf5;--mut:#8c98ad;--line:#2b3242;--warn:#ff6b5b}}
body{{margin:0;background:var(--bg);color:var(--fg);font:14px/1.5 "Noto Sans KR",system-ui,sans-serif;padding:24px 16px}}
h1{{font-size:22px;margin:0 0 6px}} .lead{{color:var(--mut);max-width:70ch;margin:0 0 20px}}
.grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:16px}}
.card{{background:var(--panel);border:1px solid var(--line);border-radius:10px;padding:12px;min-width:0}}
.card.warn{{border-color:var(--warn)}}
header{{display:flex;align-items:baseline;gap:8px}} h2{{font-size:17px;margin:0;flex:1}}
.id{{color:var(--mut);font-size:12px;letter-spacing:.06em}}
.rar{{font-size:12px;color:var(--rc);border:1px solid var(--rc);border-radius:99px;padding:0 8px}}
.desc{{color:var(--mut);margin:4px 0 8px}}
video{{width:100%;border-radius:6px;background:#000;aspect-ratio:9/16;max-height:520px}}
dl{{display:grid;grid-template-columns:auto 1fr;gap:2px 10px;margin:8px 0 0;font-size:13px}}
dt{{color:var(--mut)}} dd{{margin:0;min-width:0;overflow-wrap:anywhere}}
.mono{{font-family:ui-monospace,Consolas,monospace;font-size:12px}} .note{{color:#9fe08a}}
</style></head><body>
<h1>레벨업 카드 검수 영상 — {len(cards)}장</h1>
<p class="lead">1챕터 실제 판에서 레벨업 창에 그 카드를 띄워 고른 뒤, 최대 레벨까지 올려 싸우는 장면입니다.
수치만 바뀌는 카드는 「바뀐 수치」 줄(고르기 전 → 후)로 확인합니다.</p>
<div class="grid">{''.join(rows)}</div>
</body></html>'''
    open(os.path.join(OUT, 'index.html'), 'w', encoding='utf-8').write(page)
    print('cards', len(cards), '->', os.path.join(OUT, 'index.html'))


if __name__ == '__main__':
    main()
