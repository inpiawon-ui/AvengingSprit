"""UI 하네스 결과 요약 — 규칙·부품별로 묶어 몇 번 났는지 보여 준다(같은 문제가 상태·해상도마다 반복되므로).

사용:  python Tools/uiharness/summary.py <실행 폴더>
"""
import collections
import json
import re
import sys


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    run = sys.argv[1]
    with open(run + "/report.json", encoding="utf-8") as f:
        report = json.load(f)

    for s in report["states"]:
        if s.get("error"):
            print("도달 실패:", s["name"], "-", s["error"])

    groups = collections.defaultdict(lambda: collections.Counter())
    for s in report["states"]:
        for shot in s["shots"]:
            for v in shot["violations"]:
                if v["rule"] in ("R3", "R4"):
                    continue
                key = (v["severity"], v["rule"])
                part = re.sub(r"_\d+", "_N", v["path"])
                groups[key][f"{part}  ←  {v['against']}  [{shot['resolution']}] {v.get('note') or ''}"] += 1

    for key in sorted(groups):
        print(f"{key[0]} {key[1]}: {sum(groups[key].values())}")
        for text, n in groups[key].most_common(20):
            print(f"    {n:3}  {text}")

    unused = [a for a in report.get("allowed", []) if a["hits"] == 0]
    for a in unused:
        print("안 쓰인 예외:", a["rule"], a["path"], "-", a["why"])


if __name__ == "__main__":
    main()
