"""납품 PNG → Unity 임포트 스펙 생성 + 파일 배치.

아틀라스 배정은 화면 설계서(UISpec/*.json)에서 도출한다 — 파일명 = 요소명이므로
어느 화면 소속인지 알 수 있다. 여러 화면에 공유되는 요소(NotifyBadge 등)는
가장 먼저 등장한 화면에 배정한다(아틀라스 중복 수록 방지).

배치 규약 (05_prefabs.md / 02_addressables.md)
  텍스처 : Assets/BaseResource/{프리팹명}/*.png      ← 아틀라스 PackingSource
  아틀라스: Assets/BundleResource/Atlas/{소문자}.spriteatlasv2  ← Addressable 등록 대상
"""
import io, os, re, json, shutil, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SPECDIR = os.path.join(ROOT, 'Assets', 'Scripts', 'Editor', 'UISpec')
BASERES = os.path.join(ROOT, 'Assets', 'BaseResource')
HOSTS = ['amazoness', 'rambo', 'wizard', 'ninja', 'mafia', 'hitman',
         'yogamaster', 'dragon', 'robot', 'snowwoman', 'slugger', 'vampire']

# 화면 → (프리팹명, 아틀라스명). 순서 = 공유 요소 우선 배정 순서
SCREENS = [('Title', 'TitleMainUI', 'titlemainui'),
           ('Lobby', 'LobbyMainUI', 'lobbymainui'),
           ('HostSelect', 'HostSelectPanel', 'hostselectpanel')]


def load_manifest():
    p = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'AVSR_AssetManifest.md')
    s = io.open(p, encoding='utf-8').read()
    pat = re.compile(
        r'^\|\s*`([A-Za-z0-9_{}]+\.png)`[^|]*\|[^|]*\|\s*\**(\d+)\**\s*\|\s*\**(\d+)\**\s*\|'
        r'\s*\**([YN])\**\s*\|\s*\**([0-9,\-]+)\**\s*\|\s*\**([a-z\-]+)\**\s*\|', re.M)
    out = {}
    for fn, w, h, a, s9, piv in pat.findall(s):
        names = [fn.replace('{hostKey}', k) for k in HOSTS] if '{hostKey}' in fn else [fn]
        for n in names:
            out[n] = {'w': int(w), 'h': int(h), 'alpha': a, 'pivot': piv,
                      'slice9': [] if s9.strip() == '-' else [int(x) for x in s9.split(',')]}
    return out


def element_owner():
    """요소명(소문자) → (프리팹명, 아틀라스명). 먼저 등장한 화면이 소유."""
    owner = {}
    for key, prefab, atlas in SCREENS:
        spec = json.load(io.open(os.path.join(SPECDIR, f'{key}.json'), encoding='utf-8'))
        for n in spec['nodes']:
            owner.setdefault(n['name'].lower(), (prefab, atlas))
    return owner


def main(src):
    man = load_manifest()
    owner = element_owner()
    rows, unassigned = [], []
    for fn, sp in sorted(man.items()):
        stem = fn[:-4]
        # hostslotportrait_amazoness → 요소는 hostslotportrait
        base = stem
        for k in HOSTS:
            if stem.endswith('_' + k):
                base = stem[:-(len(k) + 1)]
                break
        for suf in ('_selected', '_locked'):
            if base.endswith(suf):
                base = base[:-len(suf)]
        hit = owner.get(base)
        if hit is None:
            unassigned.append(fn)
            hit = ('LobbyMainUI', 'lobbymainui')   # 기본값
        prefab, atlas = hit
        rows.append({'file': fn, 'prefab': prefab, 'atlas': atlas,
                     'w': sp['w'], 'h': sp['h'], 'alpha': sp['alpha'],
                     'pivot': sp['pivot'], 'slice9': sp['slice9']})

    # 파일 배치
    copied = 0
    for r in rows:
        d = os.path.join(BASERES, r['prefab'])
        os.makedirs(d, exist_ok=True)
        s = os.path.join(src, r['file'])
        if os.path.exists(s):
            shutil.copy2(s, os.path.join(d, r['file']))
            copied += 1

    io.open(os.path.join(SPECDIR, '_import.json'), 'w', encoding='utf-8').write(
        json.dumps({'assets': rows}, ensure_ascii=False, indent=1))

    from collections import Counter
    c = Counter(r['atlas'] for r in rows)
    lines = [f'스펙 {len(rows)}개 / 복사 {copied}개', '']
    for k, v in c.items():
        lines.append(f'  {k:18} {v:3}개  → Assets/BaseResource/{dict((a,p) for _,p,a in SCREENS)[k]}/')
    lines.append('')
    lines.append(f'화면 미배정(로비로 기본 배정) {len(unassigned)}건: {unassigned}')
    lines.append(f'9-slice 보유: {sum(1 for r in rows if r["slice9"])}개 / pivot=left: {sum(1 for r in rows if r["pivot"]=="left")}개')
    io.open(os.path.join(os.path.dirname(os.path.abspath(__file__)), '_import_report.txt'),
            'w', encoding='utf-8').write('\n'.join(lines))


if __name__ == '__main__':
    main(sys.argv[1])
