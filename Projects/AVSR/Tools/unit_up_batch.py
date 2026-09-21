# -*- coding: utf-8 -*-
"""캐릭터 한 명을 **명령 한 번**에 — 발주 → 납품 → 자르기 → 검수.

37명 × 5방향 = 185장이다. 손으로 반복하면 끝나지 않고, 중간에 반드시 빠뜨린다.

하는 일
  ① `unit_up_order.py` 로 방향별 발주서를 찍는다(실측 문제를 숫자째로 박은 것)
  ② 다섯 방향을 **동시에** 코덱스에 보낸다
  ③ 돌아온 판을 `unit_up_cut.py` 로 잘라 규격을 맞추고 **자동 검수**한다
  ④ 통과 못 한 방향만 목록으로 돌려준다 — 그것만 다시 보내면 된다

쓰는 법:
  python unit_up_batch.py <유닛키>            # 다섯 방향
  python unit_up_batch.py <유닛키> s e        # 고른 방향만
  python unit_up_batch.py <유닛키> --cut-only # 이미 받아 둔 판만 자르고 검수

⚠ 코덱스는 **ChatGPT 로그인**으로만 쓴다. API 키가 환경에 있으면 지우고 부른다.
"""
import os
import subprocess
import sys
import threading

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import unit_qa_all as qa

ROOT = qa.ROOT
OUT = os.path.join(ROOT, "Projects", "AVSR", "_exchange", "ref", "char_up")
CODEX = r"C:\won\tools\node-v24.18.0-win-x64\codex.ps1"


class UsageLimit(Exception):
    """코덱스 사용량 한도. 이 뒤로는 몇 번을 보내도 빈손이다 — 즉시 멈춘다."""


# 한도에 한 번 걸리면 같은 판의 나머지 스레드도 보내지 않는다
LIMIT_HIT = threading.Event()


def run_codex(order_path):
    """발주서 하나를 코덱스에 보낸다. 표준입력으로 밀어 넣는다.

    ⚠ 사용량 한도에 걸리면 **빈손으로 정상 종료**한다(납품 파일만 안 생긴다).
      그걸 모르고 돌리면 «빠꾸 → 재발주 → 빠꾸»를 끝없이 돈다(2026-09-21 세 명을 통째로 날렸다).
      출력에서 한도 문구를 찾아 `LIMIT_HIT` 을 세운다.
    """
    if LIMIT_HIT.is_set():
        return -1
    env = dict(os.environ)
    env.pop("OPENAI_API_KEY", None)   # ⚠ 로그인으로만 쓴다
    with open(order_path, "r", encoding="utf-8") as f:
        text = f.read()
    cmd = ["powershell", "-NoProfile", "-Command",
           f"& '{CODEX}' exec --sandbox workspace-write --skip-git-repo-check -"]
    p = subprocess.run(cmd, input=text.encode("utf-8"), env=env,
                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=1800)
    out = p.stdout.decode("utf-8", "replace")
    if "usage limit" in out:
        LIMIT_HIT.set()
        for line in out.splitlines():
            if "usage limit" in line:
                print("  ‼ 코덱스 사용량 한도:", line.strip()[:160], flush=True)
                break
    return p.returncode


def cut_and_check(key, facing):
    """자르고 검수한다. 통과 못 한 이유를 돌려준다(없으면 빈 목록)."""
    sheet = os.path.join(OUT, f"out_{key}_{facing}_raw.png")
    if not os.path.exists(sheet):
        return ["납품이 없다"]
    p = subprocess.run([sys.executable, os.path.join(HERE, "unit_up_cut.py"), key, facing, sheet],
                       stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    text = p.stdout.decode("utf-8", "replace")
    fails = [line.strip() for line in text.splitlines() if "빠꾸" in line or "‼" in line]
    return fails


def main():
    key = sys.argv[1]
    args = sys.argv[2:]
    cut_only = "--cut-only" in args
    facings = [a for a in args if a in qa.FACINGS] or qa.FACINGS

    if not cut_only:
        subprocess.run([sys.executable, os.path.join(HERE, "unit_up_order.py"), key],
                       stdout=subprocess.DEVNULL)
        threads = []
        for facing in facings:
            order = os.path.join(OUT, f"order_{key}_{facing}.txt")
            if not os.path.exists(order):
                print(f"{facing}: 발주서 없음 — 건너뜀")
                continue
            t = threading.Thread(target=run_codex, args=(order,))
            t.start()
            threads.append((facing, t))
        for facing, t in threads:
            t.join()
            print(f"{facing}: 납품 도착")

    print(f"\n── {key} 검수 ──")
    again = []
    for facing in facings:
        fails = cut_and_check(key, facing)
        if fails:
            again.append(facing)
            print(f"  {facing:3s} 빠꾸 — {' / '.join(fails)}")
        else:
            print(f"  {facing:3s} 통과")
    print("\n다시 보낼 방향:", " ".join(again) if again else "없음")


if __name__ == "__main__":
    main()
