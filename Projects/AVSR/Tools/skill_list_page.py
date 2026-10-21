"""캐릭터 스킬 정리 페이지 — 지금 게임에 들어가 있는 스킬을 한 장으로 (PD 2026-10-21).

글자는 언어팩 원본(Localization/AVSR_Strings.tsv, 한국어 칸)에서, 등급 · 공격 방식 · 액티브 쿨다운은
HostTable.asset 을 덤프한 _exchange/doc_skill_list/hosts_meta.tsv 에서 읽는다(유니티에서 다시 뽑으면 이 파일만 바꾼다).
영상은 _exchange/doc_skill_video/video/{키}.mp4 가 있으면 링크를 단다.

쓰기: python Projects/AVSR/Tools/skill_list_page.py  →  Projects/AVSR/_exchange/doc_skill_list/index.html
"""
import csv
import html
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
STRINGS = os.path.join(ROOT, 'Localization', 'AVSR_Strings.tsv')
OUT_DIR = os.path.join(ROOT, '_exchange', 'doc_skill_list')
META = os.path.join(OUT_DIR, 'hosts_meta.tsv')
VIDEO_DIR = os.path.join(ROOT, '_exchange', 'doc_skill_video', 'video')

GRADE = {'B': 'R', 'A': 'SR', 'S': 'SSR'}
GRADE_ORDER = ['S', 'A', 'B']
KIND = {'Single': '단발', 'Rapid': '연사', 'Melee': '근접', 'Pierce': '관통', 'Spread': '확산', 'Pulse': '파동'}
HIDDEN = {'death'}  # 사신 — 히든 캐릭터(목록 · 상자 · 상점에서 막혀 있다)


def load_strings():
    rows = csv.reader(open(STRINGS, encoding='utf-8'), delimiter='\t')
    next(rows)
    return {r[0]: (r[1] if len(r) > 1 else '') for r in rows if r}


def load_meta():
    return list(csv.DictReader(open(META, encoding='utf-8'), delimiter='\t'))


def t(s, *keys):
    """앞 키부터 찾는다 — 스킬 글자는 스킬 키(psg_h11 · psv_thug)와 몸 키(thug)에 나뉘어 있다."""
    v = next((s[k] for k in keys if s.get(k)), '')
    return html.escape(v).replace('\\n', '<br>')


def card(s, m):
    k = m['key']
    a, p = m.get('active', ''), m.get('passive', '')
    name = t(s, f'host.{k}.name')
    concept = t(s, f'host.{k}.concept')
    video = os.path.join(VIDEO_DIR, f'{k}.mp4')
    vlink = (f'<a class="vid" href="../doc_skill_video/video/{k}.mp4">영상 보기</a>'
             if os.path.exists(video) else '<span class="muted">영상 없음</span>')
    hidden = '<span class="tag hidden">히든</span>' if k in HIDDEN else ''
    growth_a = t(s, f'askill.{k}.growth', f'askill.{a}.growth')
    growth_p = t(s, f'pskill.{k}.growth', f'pskill.{p}.growth')
    return f'''
<article class="host" id="{k}">
  <header>
    <div class="who">
      <h3>{name} {hidden}</h3>
      <p class="concept">{concept}</p>
    </div>
    <div class="meta">
      <span class="tag g-{GRADE[m['grade']]}">{GRADE[m['grade']]}</span>
      <span class="tag">{KIND.get(m['kind'], m['kind'])}</span>
      <code>{k}</code>
      {vlink}
    </div>
  </header>
  <div class="skills">
    <section class="sk active">
      <h4><span class="lbl">액티브</span>{t(s, f'askill.{a}.name', f'askill.{k}.name')}<span class="cd">쿨 {m['cooldown']}초</span></h4>
      <p class="short">{t(s, f'askill.{a}.short', f'askill.{k}.short')}</p>
      <p>{t(s, f'askill.{a}.desc', f'askill.{k}.desc')}</p>
      <p class="opt"><b>Lv5 옵션</b> {t(s, f'askill.{a}.opt', f'askill.{k}.opt')}</p>
      {f'<p class="growth"><b>성장</b> {growth_a}</p>' if growth_a else ''}
    </section>
    <section class="sk ult">
      <h4><span class="lbl">필살기</span>{t(s, f'ult.{k}.name')}<span class="en">{t(s, f'uskill.{k}.en')}</span></h4>
      <p class="short">{t(s, f'ult.{k}.short')}</p>
      <p>{t(s, f'ult.{k}.desc')}</p>
      <p class="muted">빙의마다 1회 · 못 가진 몸은 판당 1회 체험</p>
    </section>
    <section class="sk passive">
      <h4><span class="lbl">패시브</span>{t(s, f'pskill.{p}.name', f'pskill.{k}.name')}</h4>
      <p class="short">{t(s, f'pskill.{p}.short', f'pskill.{k}.short')}</p>
      <p>{t(s, f'pskill.{p}.desc', f'pskill.{k}.desc')}</p>
      {f'<p class="growth"><b>성장</b> {growth_p}</p>' if growth_p else ''}
    </section>
  </div>
</article>'''


def main():
    s = load_strings()
    meta = load_meta()
    groups = []
    for g in GRADE_ORDER:
        hosts = [m for m in meta if m['grade'] == g]
        groups.append(f'<h2 id="grade-{GRADE[g]}">{GRADE[g]} <span class="count">{len(hosts)}명</span></h2>'
                      + ''.join(card(s, m) for m in hosts))
    toc = ''.join(
        f'<a href="#{m["key"]}"><span class="tag g-{GRADE[m["grade"]]}">{GRADE[m["grade"]]}</span>{t(s, "host." + m["key"] + ".name")}</a>'
        for g in GRADE_ORDER for m in meta if m['grade'] == g)
    page = f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>캐릭터 스킬 정리</title>
<style>
:root {{ --bg:#f6f5f2; --card:#fff; --fg:#1d1c1a; --muted:#77736c; --line:#e4e0d8; --accent:#5b3fd6;
  --act:#2f6fd0; --ult:#c8372d; --pas:#3a8a55; --r:#6f7680; --sr:#2f6fd0; --ssr:#b07a12; }}
@media (prefers-color-scheme: dark) {{ :root {{ --bg:#16151a; --card:#201f26; --fg:#ece9f2; --muted:#9a96a6; --line:#34323d;
  --accent:#a593ff; --act:#6aa4ff; --ult:#ff7a6e; --pas:#6fcf93; --r:#a4aab3; --sr:#6aa4ff; --ssr:#e2b04a; color-scheme:dark; }} }}
* {{ box-sizing:border-box }}
body {{ margin:0; background:var(--bg); color:var(--fg); font:15px/1.6 "Pretendard","Malgun Gothic","Apple SD Gothic Neo",sans-serif; }}
.wrap {{ max-width:1100px; margin:0 auto; padding:32px 16px 64px; }}
h1 {{ margin:0 0 4px; font-size:26px; }}
.sub {{ color:var(--muted); margin:0 0 20px; }}
.toc {{ display:flex; flex-wrap:wrap; gap:6px; margin:0 0 28px; }}
.toc a {{ text-decoration:none; color:var(--fg); background:var(--card); border:1px solid var(--line); border-radius:999px; padding:3px 10px 3px 4px; font-size:13px; display:inline-flex; gap:6px; align-items:center; }}
h2 {{ margin:36px 0 12px; font-size:20px; border-bottom:2px solid var(--line); padding-bottom:6px; }}
h2 .count {{ color:var(--muted); font-size:14px; font-weight:normal; }}
.host {{ background:var(--card); border:1px solid var(--line); border-radius:12px; padding:16px 18px; margin:0 0 14px; }}
.host header {{ display:flex; flex-wrap:wrap; justify-content:space-between; gap:8px 16px; align-items:flex-start; margin-bottom:10px; }}
.host h3 {{ margin:0; font-size:18px; }}
.concept {{ margin:2px 0 0; color:var(--muted); font-size:13px; }}
.meta {{ display:flex; flex-wrap:wrap; gap:6px; align-items:center; font-size:13px; }}
.meta code {{ color:var(--muted); font-size:12px; }}
.tag {{ display:inline-block; font-size:12px; font-weight:700; padding:1px 8px; border-radius:6px; border:1px solid var(--line); }}
.g-R {{ color:var(--r); }} .g-SR {{ color:var(--sr); }} .g-SSR {{ color:var(--ssr); }}
.tag.hidden {{ color:var(--muted); font-weight:normal; }}
.vid {{ color:var(--accent); font-weight:600; }}
.skills {{ display:grid; grid-template-columns:repeat(3, minmax(0, 1fr)); gap:10px; }}
@media (max-width:820px) {{ .skills {{ grid-template-columns:1fr; }} }}
.sk {{ border-top:3px solid var(--line); padding-top:8px; min-width:0; }}
.sk.active {{ border-color:var(--act); }} .sk.ult {{ border-color:var(--ult); }} .sk.passive {{ border-color:var(--pas); }}
.sk h4 {{ margin:0 0 4px; font-size:15px; display:flex; flex-wrap:wrap; gap:6px; align-items:baseline; }}
.lbl {{ font-size:11px; letter-spacing:.04em; color:var(--muted); font-weight:600; }}
.cd, .en {{ font-size:12px; color:var(--muted); font-weight:normal; }}
.sk p {{ margin:0 0 6px; font-size:13.5px; }}
.short {{ font-weight:600; }}
.opt, .growth {{ color:var(--fg); }}
.opt b, .growth b {{ font-size:11px; color:var(--muted); margin-right:4px; }}
.muted {{ color:var(--muted); font-size:12px; }}
</style></head><body><div class="wrap">
<h1>캐릭터 스킬 정리</h1>
<p class="sub">지금 게임에 들어가 있는 23명의 액티브 · 필살기 · 패시브. 글자는 언어팩(한국어), 등급 · 공격 방식 · 쿨다운은 호스트 표에서 뽑았다.
버튼은 빙의 · 액티브 · 필살기 셋(2026-10-16). 등급 표기 R · SR · SSR = 표의 B · A · S.</p>
<nav class="toc">{toc}</nav>
{''.join(groups)}
</div></body></html>'''
    os.makedirs(OUT_DIR, exist_ok=True)
    out = os.path.join(OUT_DIR, 'index.html')
    open(out, 'w', encoding='utf-8', newline='\n').write(page)
    print(out)


if __name__ == '__main__':
    main()
