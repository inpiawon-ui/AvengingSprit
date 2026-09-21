# -*- coding: utf-8 -*-
"""여러 유닛을 **줄줄이** 처리한다 — 발주 → 검수 → 빠꾸 재발주 → 반영.

유닛 하나에 명령 하나씩 치면 35명을 못 끝낸다. 목록을 주면 알아서 돈다.

한 유닛에 하는 일
  ① 다섯 방향 발주 → 납품
  ② 자르고 검수
  ③ 빠꾸 난 방향만 **한 번 더** 발주 → 검수
  ④ 통과한 방향만 게임에 반영(원본은 `old/` 에 남는다)
  ⑤ 두 번 해도 안 되는 방향은 목록으로 남긴다 — 사람이 볼 자리다

쓰는 법:
  python unit_up_run.py hopper robot scrapgunner ...
  python unit_up_run.py --rest        # 아직 문제 있는 유닛 전부(문제 많은 순)
"""
import os
import subprocess
import sys
import threading

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import unit_qa_all as qa
import unit_up_batch as batch

ROOT = qa.ROOT
OUT = batch.OUT

# 이 판으로 다룰 수 없는 것 — 프레임 구성이 다르다(보스 연출·단발 그림)
SKIP = {"boss", "guardian", "python", "kingpin", "crusher", "robot_snakes", "sludge"}


def order_and_deliver(key, facings):
    subprocess.run([sys.executable, os.path.join(HERE, "unit_up_order.py"), key],
                   stdout=subprocess.DEVNULL)
    threads = []
    for facing in facings:
        order = os.path.join(OUT, f"order_{key}_{facing}.txt")
        if not os.path.exists(order):
            continue
        t = threading.Thread(target=batch.run_codex, args=(order,))
        t.start()
        threads.append(t)
    for t in threads:
        t.join()


def check(key, facings):
    passed, failed = [], []
    for facing in facings:
        fails = batch.cut_and_check(key, facing)
        (failed if fails else passed).append(facing)
    return passed, failed


def apply(key, facings):
    for facing in facings:
        sheet = os.path.join(OUT, f"out_{key}_{facing}_raw.png")
        subprocess.run([sys.executable, os.path.join(HERE, "unit_up_pick.py"),
                        key, facing, sheet, "--apply"], stdout=subprocess.DEVNULL)


def rest_units():
    """아직 문제가 남은 유닛을 문제 많은 순으로."""
    rows = []
    for key in sorted(os.listdir(qa.UNITS)):
        if key in SKIP or not os.path.isdir(os.path.join(qa.UNITS, key)):
            continue
        n = sum(len(qa.check(key, f)[0]) for f in qa.FACINGS)
        if n:
            rows.append((n, key))
    rows.sort(reverse=True)
    return [k for _, k in rows]


def main():
    keys = sys.argv[1:]
    if keys == ["--rest"]:
        keys = rest_units()
    print("처리할 유닛:", " ".join(keys), flush=True)

    stuck = []
    for key in keys:
        if key in SKIP:
            print(f"[{key}] 건너뜀(프레임 구성이 다르다)", flush=True)
            continue
        print(f"\n===== {key} =====", flush=True)
        order_and_deliver(key, qa.FACINGS)
        passed, failed = check(key, qa.FACINGS)
        print(f"  1차 통과 {len(passed)} / 빠꾸 {failed}", flush=True)

        if failed:
            order_and_deliver(key, failed)
            passed2, failed2 = check(key, failed)
            passed += passed2
            failed = failed2
            print(f"  2차 통과 {len(passed2)} / 남은 빠꾸 {failed}", flush=True)

        if passed:
            apply(key, passed)
            print(f"  반영 {len(passed)}방향", flush=True)
        if failed:
            stuck.append((key, failed))

    print("\n===== 끝 =====", flush=True)
    for key, facings in stuck:
        print(f"  {key}: {' '.join(facings)} — 사람이 볼 것", flush=True)
    if not stuck:
        print("  전부 통과", flush=True)


if __name__ == "__main__":
    main()
