"""중국어 번역(번체 · 간체) 4조각을 문자열 TSV 에 합친다 — 2026-10-23 PD 「중국어도 추가해줘」.

입력: Projects/AVSR/Localization/zh/out_{1..4}.tsv(key · zh_hant · zh_hans) · 용어집 zh/glossary.md
검사: 키가 원본에 있는가 · 자리표시 {…} · 태그 <…> · \\n 개수가 한국어와 같은가 · 용어 흔들림(같은 한국어 용어 → 다른 번역)
출력: AVSR_Strings.tsv 에 zh_hant · zh_hans 칸을 붙인다(머리줄도). 그다음 유니티 메뉴 「언어팩/TSV → 문자열 표 반영」.

python zh_merge.py [--check]   (--check 면 검사만)
"""
import collections
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
LOC = os.path.join(ROOT, 'Projects', 'AVSR', 'Localization')
SRC = os.path.join(LOC, 'AVSR_Strings.tsv')

PH = re.compile(r'\{[^}]*\}')
TAG = re.compile(r'<[^>]*>')

# 한국어 용어 → 번체에 꼭 들어가야 할 말(용어집). 한국어 칸에 용어가 있는데 번역에 없으면 흔들림으로 본다
TERMS = {'빙의': '附身', '골드': '金幣', '젬': '寶石', '조각': '碎片', '행동력': '體力', '챕터': '章節', '소환': '召喚'}


def sig(s):
    return sorted(PH.findall(s)), sorted(TAG.findall(s)), s.count('\\n')


def main():
    check_only = '--check' in sys.argv
    zh = {}
    for i in range(1, 5):
        for ln in open(os.path.join(LOC, 'zh', 'out_%d.tsv' % i), encoding='utf-8').read().split('\n')[1:]:
            if not ln:
                continue
            c = ln.split('\t')
            zh[c[0]] = (c[1] if len(c) > 1 else '', c[2] if len(c) > 2 else '')
    lines = open(SRC, encoding='utf-8').read().split('\n')
    bad, miss, drift = [], [], collections.Counter()
    out = []
    for ln in lines:
        if ln.startswith('key\t'):
            out.append('key\tko\tja\ten\tzh_hant\tzh_hans')
            continue
        if not ln or ln.startswith('#'):
            out.append(ln)
            continue
        c = ln.split('\t')
        c += [''] * (4 - len(c))
        key, ko = c[0], c[1]
        hant, hans = zh.get(key, ('', ''))
        if key not in zh and ko:
            miss.append(key)
        for v in (hant, hans):
            if ko and v and sig(v) != sig(ko):
                bad.append(key)
        for k, t in TERMS.items():
            if k in ko and hant and t not in hant:
                drift[k] += 1
        out.append('\t'.join(c[:4] + [hant, hans]))
    print('번역 %d줄 · 빠짐 %d · 자리표시 어긋남 %d' % (len(zh), len(miss), len(set(bad))))
    if miss:
        print(' 빠짐:', miss[:10])
    if bad:
        print(' 어긋남:', sorted(set(bad))[:20])
    print(' 용어가 안 들어간 줄(문맥상 괜찮을 수 있음):', dict(drift))
    if check_only or bad:
        return
    open(SRC, 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
    print('→', SRC)


if __name__ == '__main__':
    main()
