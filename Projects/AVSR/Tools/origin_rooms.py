# -*- coding: utf-8 -*-
"""궁수의 전설 원작 방 구성을 그대로 읽어 온다 — 장마다 방 번호 → 역할 · 맵 후보 묶음(3단계, 2026-10-22).

PD 10-22 「원작에서 배치 자체를 잘해 놨으니 그걸 벤치마킹해서 구조를 그렇게 · 컨셉만 우리 게임에 맞게」.
원작 보고서(`WA-Game-LegendOfArcher/Docs/design/origin-report/chapters/<장>.html`)에는
  · `window.MAPS` — 맵 키 → [가로 11, 칸 문자열, {몬스터 id: 수}, {보스 id: 수}]
  · 방마다 `<details id=rN>` — 역할(일반 · 천사 · 보스 …) · 맵 후보 수 · 앞 3개 키
가 있다. 후보 키 이름(`Level_M_<묶음>_<번호>`)의 묶음으로 그 방의 후보 전체를 MAPS 에서 다시 모은다.

칸 글자: . 빈칸 · # 돌 · 나무(몸 · 탄 막음) · ~ 물(몸 막음 · 탄 통과) · = 울타리(몸 막음 · 탄 통과) · ^ 가시(밟으면 피해) ·
         " 풀(꾸밈) · t 함정 레일 · n NPC · o 기타 · m 몬스터 · b 보스

쓰는 법: python origin_rooms.py            → Projects/AVSR/Rooms/origin_rooms.json + 요약 출력
"""
import glob
import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.normpath(os.path.join(ROOT, '..', 'WA-Game-LegendOfArcher', 'Docs', 'design', 'origin-report', 'chapters'))
OUT = os.path.join(ROOT, 'Projects', 'AVSR', 'Rooms', 'origin_rooms.json')

ROOM_RE = re.compile(r"<details id=r(\d+)><summary><b>방 \d+</b> · ([^·<]+?) · 맵 후보 (\d+)개[^<]*</summary>(.*?)</details>", re.S)
KEY_RE = re.compile(r"data-map='([^']+)'")


def band_of(key):
    """'elevel/main15/Level_M_0_0001' → 'elevel/main15/Level_M_0_' (같은 묶음의 앞부분)."""
    return re.sub(r'\d+$', '', key)


def read_chapter(path):
    t = open(path, encoding='utf-8').read()
    m = re.search(r'window\.MAPS=(\{.*?\});', t, re.S)
    maps = json.loads(m.group(1)) if m else {}
    name = re.search(r'<h1[^>]*>([^<]+)', t)
    rooms = []
    for rid, role, count, body in ROOM_RE.findall(t):
        keys = KEY_RE.findall(body)
        bands = sorted({band_of(k) for k in keys})
        pool = sorted(k for k in maps if any(k.startswith(b) for b in bands)) if bands else []
        rooms.append({'room': int(rid), 'role': role.strip(), 'candidates': int(count), 'bands': bands, 'pool': pool})
    return {'id': os.path.splitext(os.path.basename(path))[0], 'name': name.group(1).strip() if name else '',
            'rooms': rooms}, maps


def main():
    chapters, allmaps = [], {}
    for p in sorted(glob.glob(os.path.join(SRC, '*.html')), key=lambda s: int(re.sub(r'\D', '', os.path.basename(s)) or 0)):
        ch, maps = read_chapter(p)
        allmaps.update(maps)
        if ch['rooms']:
            chapters.append(ch)
    thirty = [c for c in chapters if len(c['rooms']) == 30]
    print(f'장 {len(chapters)} · 30방 장 {len(thirty)} · 맵 {len(allmaps)}')
    for c in thirty[:3]:
        print(c['id'], c['name'], [(r['room'], r['role'], len(r['pool'])) for r in c['rooms']][:12], '…')
    bad = [(c['id'], r['room']) for c in thirty for r in c['rooms'] if r['candidates'] and len(r['pool']) < min(3, r['candidates'])]
    print('후보를 못 모은 방', len(bad), bad[:10])
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w', encoding='utf-8') as f:
        json.dump({'chapters': chapters, 'maps': allmaps}, f, ensure_ascii=False)
    print(OUT, os.path.getsize(OUT) // 1024, 'KB')


if __name__ == '__main__':
    main()
