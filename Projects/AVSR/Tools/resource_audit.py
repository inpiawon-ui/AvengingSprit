# -*- coding: utf-8 -*-
"""리소스 점검 — 안 쓰는 리소스 · 포맷이 최적화 안 된 리소스를 찾는다(Unity 를 켜지 않고 파일만 읽는다).

python Projects/AVSR/Tools/resource_audit.py
  → Projects/AVSR/_exchange/ref/resource_audit/report.md · unused.tsv · format.tsv

판정 순서
1. 빌드에 들어가는 뿌리: 빌드 씬(EditorBuildSettings) · 어드레서블 항목 · Resources 폴더
2. 뿌리에서 GUID 참조를 따라간다(프리팹 · 씬 · 표 · 재질 · 아틀라스 · 폰트). 아틀라스가 폴더를 묶으면 그 폴더 그림도 따라간다
3. 아틀라스 안 그림은 코드가 **이름**으로 꺼낸다(GetSprite("…") · $"unit_{key}_{dir}") — 코드 · 표의 글자와 이름 틀을 맞춰 본다
4. 그래도 아무 데서도 안 걸리면 「안 씀」, 이름 틀에만 걸리면 「동적 이름 — 보류」
"""
import os, re, struct, sys, io, collections

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
A = os.path.join(ROOT, 'Assets')
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'resource_audit')
TEXT_EXT = {'.unity', '.prefab', '.asset', '.mat', '.spriteatlasv2', '.spriteatlas', '.controller', '.anim',
            '.overrideController', '.shadergraph', '.shadersubgraph', '.playable', '.mask', '.preset', '.lighting',
            '.fontsettings', '.guiskin', '.renderTexture', '.terrainlayer', '.inputactions'}
RES_EXT = {'.png', '.jpg', '.jpeg', '.tga', '.psd', '.wav', '.mp3', '.ogg', '.ttf', '.otf', '.mat', '.prefab',
           '.asset', '.spriteatlasv2', '.shader', '.shadergraph', '.mp4', '.anim', '.controller'}
GUID_RE = re.compile(r'guid: ([0-9a-f]{32})')


def rel(p):
    return os.path.relpath(p, ROOT).replace('\\', '/')


def read(p):
    try:
        with open(p, 'r', encoding='utf-8', errors='ignore') as f:
            return f.read()
    except OSError:
        return ''


def png_size(p):
    try:
        with open(p, 'rb') as f:
            h = f.read(24)
        if h[:8] == b'\x89PNG\r\n\x1a\n':
            return struct.unpack('>II', h[16:24])
    except OSError:
        pass
    return (0, 0)


# ── 1. 파일 · GUID 목록 ─────────────────────────────────────────
guid_of, path_of, folder_guid = {}, {}, {}
files = []
for dp, dns, fns in os.walk(A):
    for fn in fns:
        p = os.path.join(dp, fn)
        if fn.endswith('.meta'):
            target = p[:-5]
            m = re.search(r'^guid: ([0-9a-f]{32})', read(p), re.M)
            if not m:
                continue
            g = m.group(1)
            path_of[g] = target
            guid_of[target] = g
            if os.path.isdir(target):
                folder_guid[g] = target
        else:
            files.append(p)

# ── 2. 참조 그래프 ───────────────────────────────────────────────
refs = collections.defaultdict(set)
for p in files:
    ext = os.path.splitext(p)[1]
    if ext in TEXT_EXT:
        txt = read(p)
        refs[p] = set(GUID_RE.findall(txt))
# 폰트 · 그림 .meta 가 다른 GUID 를 가리키는 경우(폰트 대체 목록 등)는 .asset 쪽에 있다

# 뿌리
roots = set()
ebs = read(os.path.join(ROOT, 'ProjectSettings', 'EditorBuildSettings.asset'))
for blk in re.finditer(r'enabled: 1\s+path: (\S+)\s+guid: ([0-9a-f]{32})', ebs):
    roots.add(blk.group(2))
# 프로젝트 설정이 가리키는 것(앱 아이콘 · 스플래시 · 항상 포함 셰이더 등)도 뿌리다
ps_dir = os.path.join(ROOT, 'ProjectSettings')
for fn in os.listdir(ps_dir):
    if fn.endswith('.asset'):
        roots.update(GUID_RE.findall(read(os.path.join(ps_dir, fn))))
addr = {}   # guid → address
grp_dir = os.path.join(A, 'AddressableAssetsData', 'AssetGroups')
for fn in os.listdir(grp_dir):
    if fn.endswith('.asset'):
        txt = read(os.path.join(grp_dir, fn))
        for m in re.finditer(r'- m_GUID: ([0-9a-f]{32})\s+m_Address: (.*)', txt):
            addr[m.group(1)] = (m.group(2).strip(), fn[:-6])
            roots.add(m.group(1))
for p in files:
    if '/Resources/' in p.replace('\\', '/') and p in guid_of:
        roots.add(guid_of[p])
# 프레임워크 · 플러그인 · 설정은 손대지 않는 영역이라 뿌리로 본다
for p in files:
    rp = rel(p)
    if rp.startswith(('Assets/GameFramework/', 'Assets/Plugins/', 'Assets/Settings/', 'Assets/TextMesh Pro/',
                      'Assets/AddressableAssetsData/')) and p in guid_of:
        roots.add(guid_of[p])


def under(folder, p):
    f = folder.replace('\\', '/').rstrip('/') + '/'
    return p.replace('\\', '/').startswith(f)


reach, stack = set(), list(roots)
folder_packed_by = collections.defaultdict(set)   # 폴더 → 그 폴더를 묶는 아틀라스
while stack:
    g = stack.pop()
    if g in reach:
        continue
    reach.add(g)
    p = path_of.get(g)
    if not p:
        continue
    if g in folder_guid:   # 폴더(아틀라스 packable · 어드레서블 폴더 항목) — 안의 파일 전부
        for q in files:
            if under(p, q) and q in guid_of and guid_of[q] not in reach:
                stack.append(guid_of[q])
        continue
    for r in refs.get(p, ()):
        if r not in reach:
            stack.append(r)

# 아틀라스가 묶는 폴더/파일
atlas_packs = {}
for p in files:
    if p.endswith('.spriteatlasv2'):
        txt = read(p)
        pk = re.search(r'packables:(.*?)\n  m_IsVariant', txt, re.S)
        gs = GUID_RE.findall(pk.group(1)) if pk else []
        atlas_packs[p] = gs
packed_in = {}   # 그림 경로 → 아틀라스 경로
for ap, gs in atlas_packs.items():
    for g in gs:
        t = path_of.get(g)
        if not t:
            continue
        if os.path.isdir(t):
            for q in files:
                if under(t, q):
                    packed_in.setdefault(q, ap)
        else:
            packed_in.setdefault(t, ap)

# ── 3. 코드 · 표에서 쓰는 이름 ──────────────────────────────────
literals, templates, prefixes = set(), [], set()
STR_RE = re.compile(r'(\$?)@?"((?:[^"\\\n]|\\.)*)"')
# ⚠ 에디터 코드(빌더 · 도구)는 빼고 센다 — 빌더가 경로로 부르는 그림은 만든 프리팹이 GUID 로 갖고 있어야 쓰인다.
#   빌더 글자만 보고 「사용」으로 치면 예전 빌더가 남긴 이름이 전부 산 것처럼 보인다
def is_editor(p):
    q = p.replace(os.sep, '/')
    return '/Editor/' in q
code_files = [p for p in files if p.endswith('.cs') and not is_editor(p)]
editor_words = set()
for p in files:
    if p.endswith('.cs') and is_editor(p):
        editor_words.update(w.lower() for w in re.findall(r'[A-Za-z0-9_]{3,}', read(p)))
for p in code_files:
    txt = read(p)
    for m in STR_RE.finditer(txt):
        interp, s = m.group(1), m.group(2)
        if interp and '{' in s:
            pat = re.sub(r'\{[^}]*\}', '\x00', s)
            parts = pat.split('\x00')
            # 앞에 고정 글자(「fx_」 이상)가 박힌 틀만 — 「{a}_{b}」 같은 틀은 아무 이름에나 맞는다
            if len(re.sub(r'[^A-Za-z]', '', parts[0])) < 2 or len(parts[0]) < 3:
                continue
            rx = r'([A-Za-z0-9_]+?)'.join(re.escape(x) for x in parts)
            templates.append(re.compile('^' + rx + '$', re.I))
        else:
            literals.add(s.lower())
            # 「"fx_" + name + "_" + i」 같은 이어 붙이기 — 앞 조각을 접두어로 본다
            if s.endswith('_') and len(s) >= 3:
                prefixes.add(s.lower())
# 이름 조각 — 대소문자는 가리지 않는다(표의 「C011」 → 그림 「card_c011」)
#   data: 표 · json · 프리팹 · 씬에 적힌 글자(이름 그대로 쓰이는 출처)
#   words: data + 코드의 모든 낱말(enum 이름 「Strike」 → 「affinity_strike」 같은 틀의 빈칸 값)
data = set()
for s in literals:
    data.update(re.findall(r'[a-z0-9_]+', s))
for p in files:
    ext = os.path.splitext(p)[1]
    is_table = ext == '.asset' and '/TableData/' in p.replace(os.sep, '/')
    if ext in ('.json', '.txt', '.prefab', '.unity') or is_table:
        data.update(w.lower() for w in re.findall(r'[A-Za-z0-9_]{3,}', read(p)))
words = set(data)
for p in code_files:
    words.update(w.lower() for w in re.findall(r'[A-Za-z][A-Za-z0-9]*', read(p)))
tokens = words


def seg_ok(x):
    x = x.lower()
    return x.isdigit() or len(x) <= 2 or x in words or x.rstrip('0123456789') in words


def name_use(stem):
    """'literal' · 'template' · 'prefix' · None"""
    low = stem.lower()
    if low in literals or low in data:
        return 'literal'
    for rx in templates:
        m = rx.match(stem)
        # 빈칸에 들어간 값이 코드 · 표에 있는 글자(호스트 키 · 방향 · 숫자)여야 한다
        if m and all(all(seg_ok(x) for x in g.split('_') if x) for g in m.groups()):
            return 'template'
    for pf in prefixes:
        if low.startswith(pf):
            segs = [x for x in low[len(pf):].split('_') if x]
            if segs and all(seg_ok(x) for x in segs):
                return 'prefix'
    return None


# ── 4. 판정 ─────────────────────────────────────────────────────
rows = []
for p in files:
    ext = os.path.splitext(p)[1].lower()
    if ext not in RES_EXT:
        continue
    rp = rel(p)
    if not rp.startswith(('Assets/BaseResource/', 'Assets/BundleResource/', 'Assets/Resources/',
                          'Assets/Screenshots/', 'Assets/Scenes/')):
        continue
    g = guid_of.get(p)
    size = os.path.getsize(p)
    stem = os.path.splitext(os.path.basename(p))[0]
    atlas = packed_in.get(p)
    atlas_live = atlas is not None and guid_of.get(atlas) in reach
    # GUID 로 직접 걸린 것(프리팹 · 표 · 재질이 가리킴) — 아틀라스 폴더를 통한 도달과 구분한다
    direct_ref = any(g in rs for q, rs in refs.items() if q != atlas and not q.endswith('.spriteatlasv2'))
    in_addr = g in addr
    status, why = None, ''
    if in_addr:
        a, grp = addr[g]
        nu = name_use(a.split('/')[-1]) or name_use(a) or (a in literals and 'literal')
        # 주소를 코드가 부르는가(주소 그대로 · 주소 틀)
        called = a in literals or any(rx.match(a) for rx in templates) or \
                 any(a.startswith(pf) for pf in prefixes if '/' in pf)
        if called or nu or direct_ref:
            status = '사용'
            why = '주소 ' + a
        else:
            status = '보류'
            why = f'어드레서블 {grp} 「{a}」 — 코드에 주소 글자가 안 보인다(표 · 조립 주소일 수 있다)'
    elif direct_ref and g in reach:
        status, why = '사용', 'GUID 참조(프리팹 · 표 · 재질)'
    elif atlas and atlas_live:
        nu = name_use(stem)
        if nu == 'literal':
            status, why = '사용', f'아틀라스 {os.path.basename(atlas)} · 이름 직접'
        elif nu:
            status, why = '사용(동적)', f'아틀라스 {os.path.basename(atlas)} · 코드가 이름을 조립해 꺼낸다({nu})'
        else:
            status, why = '안 씀', f'아틀라스 {os.path.basename(atlas)} 에 묶여만 있고 게임 코드가 이름을 부르지 않는다' +                 (' (에디터 빌더에만 이름이 나온다)' if stem.lower() in editor_words else '')
    elif atlas and not atlas_live:
        status, why = '안 씀', f'묶인 아틀라스 {os.path.basename(atlas)} 가 어드레서블 · 빌드에 없다'
    elif g in reach:
        status, why = '사용', '빌드 경로에서 도달'
    else:
        nu = name_use(stem)
        if ext in ('.png', '.jpg') and rp.startswith('Assets/BaseResource/') and nu:
            status, why = '보류', f'참조 없음 · 이름({nu})만 걸린다 — 아틀라스 밖이라 런타임에 못 꺼낸다'
        else:
            status, why = '안 씀', '아무 데서도 참조 · 이름 사용이 없다' +                 (' (에디터 빌더에만 이름이 나온다)' if stem.lower() in editor_words else '')
    rows.append((status, rp, size, why))

# ── 5. 포맷 점검 ─────────────────────────────────────────────────
fmt = []


def blk(txt, target):
    m = re.search(r'buildTarget: ' + target + r'\n(.*?)(?=\n  - serializedVersion|\n  spriteSheet|\n  packingSettings|\Z)', txt, re.S)
    if not m:
        return {}
    return dict(re.findall(r'(\w+): (-?\d+)', m.group(1)))


for p in files:
    ext = os.path.splitext(p)[1].lower()
    rp = rel(p)
    if not rp.startswith(('Assets/BaseResource/', 'Assets/BundleResource/')):
        continue
    meta = read(p + '.meta')
    if ext == '.png':
        w, h = png_size(p)
        top = dict(re.findall(r'^  (\w+): (-?\d+)', meta, re.M))
        mip = re.search(r'enableMipMap: (\d)', meta)
        mip = mip and mip.group(1) == '1'
        readable = top.get('isReadable') == '1'
        ttype = top.get('textureType')
        filt = re.search(r'filterMode: (-?\d+)', meta)
        filt = filt.group(1) if filt else '?'
        d = blk(meta, 'DefaultTexturePlatform'); an = blk(meta, 'Android')
        dmax = int(d.get('maxTextureSize', 2048)); dcomp = d.get('textureCompression', '1')
        a_over = an.get('overridden') == '1'
        amax = int(an.get('maxTextureSize', dmax)) if a_over else dmax
        acomp = an.get('textureCompression', dcomp) if a_over else dcomp
        afmt = an.get('textureFormat', '-1') if a_over else '-1'
        atlas = packed_in.get(p)
        issues = []
        direct = atlas is None   # 아틀라스에 묶인 원본은 빌드에 안 들어간다 — 압축 · 읽기 설정은 아틀라스 쪽이 정한다
        if max(w, h) > amax:
            issues.append(f'원본 {w}x{h} 가 최대 {amax} 보다 커서 줄어든다(흐려짐)')
        if readable and direct and '/Cutscene/' not in rp:   # 컷신은 색 빼기(GetPixels32)에 읽기가 필요하다
            issues.append('Read/Write 켜짐(메모리 2배)')
        if mip and ttype == '8' and direct:
            issues.append('스프라이트인데 밉맵 켜짐(용량 +33%)')
        # 도트 그림(유닛 · 인게임 · 팝업 연출)만 Point 를 본다 — 로비 일러스트 · 배경은 부드러운 필터가 맞다
        pixel_art = any(k in rp for k in ('/Unit/', '/InGameMainUI/', '/PopupFx/', '/Card/'))
        if filt != '0' and ttype == '8' and pixel_art:
            issues.append(f'도트 그림인데 필터가 Point 가 아니다(filterMode {filt}) — 확인 필요')
        if direct and acomp == '0' and afmt == '-1' and w * h >= 256 * 256:
            mb = w * h * 4 / 1048576
            issues.append(f'아틀라스 밖 · 압축 없음(RGBA32) — 메모리 약 {mb:.1f}MB, ASTC 6x6 이면 약 {mb / 9:.1f}MB')
        if issues:
            fmt.append(('그림', rp, f'{w}x{h}', '; '.join(issues)))
    elif ext == '.spriteatlasv2':
        meta = read(p + '.meta')
        d = blk(meta, 'DefaultTexturePlatform'); an = blk(meta, 'Android')
        a_over = an.get('overridden') == '1'
        comp = (an if a_over else d).get('textureCompression', '?')
        mx = (an if a_over else d).get('maxTextureSize', '?')
        f2 = (an if a_over else d).get('textureFormat', '-1')
        ts = dict(re.findall(r'(\w+): (-?\d+)', meta.split('platformSettings')[0]))
        issues = []
        area = sum(png_size(q)[0] * png_size(q)[1] for q, ap in packed_in.items() if ap == p and q.endswith('.png'))
        est = area * 4 * 1.3 / 1048576   # 여백 · 빈칸 어림 1.3배
        if comp == '0' and f2 == '-1':
            issues.append(f'압축 없음(RGBA32) · 그림 면적으로 어림 약 {est:.1f}MB(ASTC 4x4 면 {est / 4:.1f}MB)')
        if ts.get('generateMipMaps') == '1':
            issues.append('밉맵 켜짐')
        if ts.get('readable') == '1':
            issues.append('Read/Write 켜짐')
        if ts.get('filterMode') not in ('0', None):
            issues.append('필터 Point 아님')
        if not a_over:
            issues.append('안드로이드 따로 세팅 없음(기본값을 그대로 쓴다)')
        pk = re.search(r'padding: (\d+)', meta)
        if issues:
            fmt.append(('아틀라스', rp, f'최대 {mx} · 어림 {est:.1f}MB', '; '.join(issues)))
    elif ext in ('.wav', '.mp3', '.ogg'):
        lt = re.search(r'loadType: (\d)', meta); cf = re.search(r'compressionFormat: (\d)', meta)
        q = re.search(r'quality: ([\d.]+)', meta); mono = re.search(r'forceToMono: (\d)', meta)
        lt = lt.group(1) if lt else '?'; cf = cf.group(1) if cf else '?'
        sz = os.path.getsize(p) / 1048576
        issues = []
        if cf == '0':
            issues.append('PCM(압축 없음)')
        is_bgm = '/BGM/' in rp.replace('\\', '/')
        if is_bgm and lt == '0':
            issues.append('BGM 인데 Decompress On Load(메모리에 다 풀린다)')
        if not is_bgm and lt == '2' and sz < 1:
            issues.append('짧은 효과음인데 Streaming')
        if '/Samples/' in rp.replace('\\', '/'):
            pass
        if issues:
            fmt.append(('소리', rp, f'{sz:.1f}MB', '; '.join(issues)))

# 같은 그림이 두 번(내용 같음)
import hashlib
dups = collections.defaultdict(list)
for p in files:
    if p.lower().endswith('.png') and rel(p).startswith(('Assets/BaseResource/', 'Assets/BundleResource/')):
        with open(p, 'rb') as f:
            dups[hashlib.md5(f.read()).hexdigest()].append(rel(p))
dup_rows = [v for v in dups.values() if len(v) > 1]

# ── 6. 쓰기 ─────────────────────────────────────────────────────
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, 'unused.tsv'), 'w', encoding='utf-8') as f:
    f.write('판정\t경로\t바이트\t근거\n')
    for r in sorted(rows):
        f.write('\t'.join(map(str, r)) + '\n')
with open(os.path.join(OUT, 'format.tsv'), 'w', encoding='utf-8') as f:
    f.write('종류\t경로\t크기\t문제\n')
    for r in sorted(fmt):
        f.write('\t'.join(r) + '\n')
with open(os.path.join(OUT, 'duplicates.tsv'), 'w', encoding='utf-8') as f:
    for v in dup_rows:
        f.write('\t'.join(v) + '\n')

cnt = collections.Counter(r[0] for r in rows)
byt = collections.Counter()
for r in rows:
    byt[r[0]] += r[2]
print('판정', dict(cnt))
print('MB', {k: round(v / 1048576, 1) for k, v in byt.items()})
print('포맷 문제', collections.Counter(r[0] for r in fmt))
print('중복 묶음', len(dup_rows))
