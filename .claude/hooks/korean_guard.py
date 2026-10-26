# 한국어만 쓰기 지킴이(PD 2026-10-26 「메모에도 추가하고 훅에도 추가해 한글만 사용한다고」).
# 사용자는 한국인이고 영어를 모른다 — 알림만으로는 계속 영어가 새서 막는 훅을 건다.
#   stop  : 답을 끝낼 때 마지막 답이 영어 위주면 막고 한국어로 다시 쓰게 한다
#   pre   : 명령 실행(Bash · PowerShell)의 설명 칸이 영어면 막는다(설명 칸도 PD 화면에 보인다)
# 코드 블록 · `코드` · 파일 경로 · 주소는 세지 않는다(원문 그대로 둬도 된다).
import json
import re
import sys

MAX_LATIN_SHARE = 0.45  # 글자(한글 + 로마자) 중 로마자가 이 비율을 넘으면 영어 위주로 본다


def strip_allowed(text):
    text = re.sub(r"```.*?```", " ", text, flags=re.S)          # 코드 블록
    text = re.sub(r"`[^`]*`", " ", text)                         # 인라인 코드
    text = re.sub(r"\[[^\]]*\]\([^)]*\)", " ", text)             # 마크다운 링크
    text = re.sub(r"https?://\S+", " ", text)                    # 주소
    text = re.sub(r"[A-Za-z]:[\\/][^\s]*", " ", text)            # 윈도 경로
    text = re.sub(r"(?:[\w.-]+/)+[\w.-]+", " ", text)            # a/b/c 경로
    text = re.sub(r"\b[\w-]+\.(?:cs|py|md|png|json|ps1|prefab|asset|tsv|gif|html|txt|meta)\b", " ", text)
    text = re.sub(r"\b[0-9a-f]{7,40}\b", " ", text)              # 커밋 해시
    text = re.sub(r"\b[A-Z][a-z]+[A-Z]\w*\b", " ", text)         # CamelCase 코드 이름
    text = re.sub(r"\b\w*_\w*\b", " ", text)                     # snake_case 이름
    return text


def latin_share(text):
    t = strip_allowed(text)
    hangul = len(re.findall(r"[가-힣]", t))
    latin = len(re.findall(r"[A-Za-z]", t))
    total = hangul + latin
    return (latin / total if total else 0.0), total


def turn_texts(path):
    """이번 차례(마지막 사용자 글 뒤)에 쓴 글 묶음 전부 — 도구 사이 짧은 진행 문장까지(10-26: 마지막 글만 봐서 중간 영어 문장이 지나갔다)."""
    texts = []
    try:
        with open(path, encoding="utf-8") as f:
            lines = f.readlines()
    except OSError:
        return texts
    for line in reversed(lines):
        try:
            row = json.loads(line)
        except ValueError:
            continue
        if row.get("type") == "user":
            content = row.get("message", {}).get("content")
            # 사용자 실제 글(도구 결과 말고)이 나오면 거기서 멈춘다 — 이번 차례만 본다
            if isinstance(content, str) or (isinstance(content, list) and any(isinstance(c, dict) and c.get("type") == "text" for c in content)):
                break
            continue
        if row.get("type") != "assistant":
            continue
        for c in row.get("message", {}).get("content", []) or []:
            if isinstance(c, dict) and c.get("type") == "text" and c.get("text", "").strip():
                texts.append(c["text"])
    return texts


def main():
    # 윈도 기본 출력(cp949)은 「—」 같은 글자에서 죽는다 — UTF-8 로
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stdin.reconfigure(encoding="utf-8")
    mode = sys.argv[1] if len(sys.argv) > 1 else "stop"
    try:
        data = json.load(sys.stdin)
    except ValueError:
        return
    if mode == "pre":
        desc = (data.get("tool_input") or {}).get("description") or ""
        share, total = latin_share(desc)
        if total >= 12 and share > MAX_LATIN_SHARE:
            print(json.dumps({"hookSpecificOutput": {
                "hookEventName": "PreToolUse",
                "permissionDecision": "deny",
                "permissionDecisionReason": "명령 설명 칸이 영어다 — PD 는 영어를 모른다. description 을 한국어로 바꿔 다시 실행하라.",
            }}, ensure_ascii=False))
        return
    if data.get("stop_hook_active"):
        return   # 다시 쓴 뒤에는 한 번만 본다(무한 반복 막기)
    bad = []
    for t in turn_texts(data.get("transcript_path", "")):
        share, total = latin_share(t)
        # 짧은 진행 문장도 잡는다 — 로마자 글자 12 개 이상이고 절반 가까이가 로마자면 영어 문장
        if total >= 12 and share > MAX_LATIN_SHARE:
            bad.append(t.strip().splitlines()[0][:60])
    if bad:
        print(json.dumps({
            "decision": "block",
            "reason": "이번 차례 글 중 영어로 쓴 문장이 있다 — PD 는 영어를 모른다: " + " / ".join(bad[:3])
                      + " … 도구 사이 한 줄까지 한국어로만 쓴다. 이번 차례 보고를 처음부터 한국어로 다시 정리하라.",
        }, ensure_ascii=False))

if __name__ == "__main__":
    main()
