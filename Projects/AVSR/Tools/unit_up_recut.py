# -*- coding: utf-8 -*-
"""이미 반영한 방향을 **같은 납품 판**으로 다시 잘라 반영한다 — 자르기 규칙을 고쳤을 때 쓴다.

run_rest.log 를 읽어 반영된 유닛 · 방향만 고른다. 두 번 다 빠꾸 난 방향(«사람이 볼 것»)은
남은 판이 빠꾸 판이라 건드리지 않는다.

쓰는 법:  python unit_up_recut.py [로그 경로]
"""
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
OUT = os.path.join(ROOT, 'Projects', 'AVSR', '_exchange', 'ref', 'char_up')
FACINGS = ['s', 'se', 'e', 'ne', 'n']


def main():
    log = sys.argv[1] if len(sys.argv) > 1 else os.path.join(OUT, 'run_rest.log')
    text = open(log, encoding='utf-8-sig').read()
    units = re.findall(r'===== (\w+) =====', text)
    stuck = {}
    for key, dirs in re.findall(r'  (\w+): ([a-z ]+) — 사람이 볼 것', text):
        stuck[key] = set(dirs.split())
    for key in units:
        if key == '끝':
            continue
        for f in FACINGS:
            if f in stuck.get(key, ()):
                continue
            sheet = os.path.join(OUT, f'out_{key}_{f}_raw.png')
            if not os.path.exists(sheet):
                continue
            subprocess.run([sys.executable, os.path.join(HERE, 'unit_up_pick.py'), key, f, sheet, '--apply'],
                           stdout=subprocess.DEVNULL, check=True)
        print(key, '다시 반영', [f for f in FACINGS if f not in stuck.get(key, ())], flush=True)


if __name__ == '__main__':
    main()
