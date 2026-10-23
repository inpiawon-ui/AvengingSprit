"""RE:BORN UI 리뉴얼 — 화면별 새 글자(Localization/rb_strings_*.tsv, 5개 언어)를 문자열 TSV 에 합친다.

같은 키가 있으면 바꾸고, 없으면 끝에 붙인다. 자리표시 {…} 개수가 언어마다 다르면 알린다(한국어 {1} 조사 자리 같은 의도된 것은 예외 목록).
그다음 유니티 메뉴 「언어팩/TSV → 문자열 표 반영」.

python rb_strings_merge.py
"""
import glob
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
LOC = os.path.join(ROOT, 'Projects', 'AVSR', 'Localization')
MAIN = os.path.join(LOC, 'AVSR_Strings.tsv')
PH = re.compile(r'\{(\d+)[^}]*\}')
# 한국어만 쓰는 조사 자리({1} — 「영매로 · 청룡으로」)
KO_ONLY = {'ui.rb.lobby.sortie_with'}


def main():
    lines = open(MAIN, encoding='utf-8').read().split('\n')
    index = {l.split('\t')[0]: i for i, l in enumerate(lines) if l and not l.startswith('#')}
    added = replaced = 0
    for path in sorted(glob.glob(os.path.join(LOC, 'rb_strings_*.tsv'))):
        for row in open(path, encoding='utf-8').read().split('\n')[1:]:
            if not row.strip():
                continue
            c = row.split('\t')
            if len(c) != 6:
                print('칸 수 틀림:', os.path.basename(path), c[0])
                continue
            nums = [set(PH.findall(x)) for x in c[1:]]
            if c[0] not in KO_ONLY and any(n != nums[0] for n in nums[1:]):
                print('자리표시 어긋남:', c[0])
            if c[0] in index:
                lines[index[c[0]]] = row
                replaced += 1
            else:
                lines.append(row)
                index[c[0]] = len(lines) - 1
                added += 1
    open(MAIN, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines))
    print('더함 %d · 바꿈 %d' % (added, replaced))


if __name__ == '__main__':
    main()
