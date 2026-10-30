# 배경 리뉴얼 남은 것 발주문 (2026-10-30) — 대기실(중간 보스 방) 바닥 · 방 밖 + 방 밖 그림 위 띠 없애기
# 왜: 10-29 적용 뒤 캡처(bg_apply/game_shot/ch01.png) — 바닥 그림의 아래 벽 띠(108 px) 밑에 방 밖 그림 윗부분의 난간 띠가
#     또 붙어 입구 아래 난간이 두 겹으로 보였다. 대기실 바닥 · 방 밖은 9월 옛 화풍 그대로였다.
# 결과: _exchange/order_bgfix_*.ps1 · _exchange/run_bgfix_*.ps1 · 기준 크롭 _exchange/ref/bg_order/fix1030_*.png
# 쓰는 법: python Projects/AVSR/Tools/bg_fix_orders_1030.py
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
EX = ROOT / "Projects/AVSR/_exchange"
REF = EX / "ref/bg_order"
RF = "Assets/BundleResource/RoomFloor"

STAGES = ["junkyard", "missile", "street", "rooftop", "lab", "refinery"]
KO = {"junkyard": "쓰레기 집적장", "missile": "미사일 기지", "street": "밤 도시 거리",
      "rooftop": "옥상", "lab": "연구소", "refinery": "정유소"}
BOSSES = {"guardian": "lab", "kingpin": "refinery", "python": "street", "robot_snakes": "junkyard", "sludge": "rooftop"}
BOSS_KO = {"guardian": "가디언", "kingpin": "킹핀", "python": "파이썬", "robot_snakes": "로봇 뱀", "sludge": "슬러지"}

HEAD = """이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
"""
TAIL = """- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
"""
# 방 밖 그림의 새 규칙 — 위 띠 없음
NO_BAND = """[가장 중요 — 위 끝에 띠 금지] 이 그림 **바로 위에는 방 바닥 그림의 아래 벽 띠(높이 108 px — 난간 · 램프 · 패널)가 이미 붙어 있다**(참고 크롭: {band}).
  그래서 이 그림의 **맨 윗줄(y=0)부터 곧바로 방 밖 바닥**(복도 · 바깥 바닥 판)으로 시작한다. 위 끝에 난간 · 벽 띠 · 램프 줄 · 패널 띠 · 테두리 선을 **절대 그리지 마라** —
  10-29 판은 위 끝에 띠가 또 있어 게임에서 난간이 두 겹으로 보였다(캡처: Projects/AVSR/_exchange/bg_apply/game_shot/ch01.png 화면 가운데 아래의 겹친 난간 두 줄).
  위 끝 색 · 밝기는 참고 크롭 띠 아래쪽 바닥과 자연스럽게 이어지게.
"""


def ps1(body: str, name: str):
    text = ("$env:OPENAI_API_KEY = $null\n"
            "Set-Location \"C:\\won\\UnityProject\\AvengingSprit\"\n"
            "$prompt = @'\n" + HEAD + body + "'@\n"
            "codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n")
    (EX / f"order_bgfix_{name}.ps1").write_text(text, encoding="utf-8-sig")
    return name


def band_crop(floor_path: Path, tag: str) -> str:
    """바닥 그림 아래 띠(108) + 그 위 바닥 60 px — 방 밖 그림 바로 위에 오는 것."""
    dst = REF / f"fix1030_band_{tag}.png"
    im = Image.open(floor_path).convert("RGB")
    im.crop((0, im.height - 168, im.width, im.height)).save(dst)
    return str(dst.relative_to(ROOT)).replace("\\", "/")


def stage_apron(s: str, v2: bool):
    tag = f"{s}v2" if v2 else s
    floor = EX / f"bg_apply/{tag}/roomfloor_env_{tag}.png"
    band = band_crop(floor, tag)
    now = f"Projects/AVSR/_exchange/bg_apply/{tag}/roomapron_{tag}.png"
    title = f"{KO[s]}{' (7~12챕터 변형 무대)' if v2 else ''}"
    return ps1(f"""[무대] {title} — 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장. **지금 그림을 고쳐 다시 그리는 것**이다.
[지금 그림 — 화풍 · 색 · 물건 정답] {now}
  이 그림의 화풍 · 색 · 도트 굵기 · 좌우 물건 · 아래로 갈수록 어두워지는 것을 **그대로** 따른다. 바꾸는 것은 위 끝 하나뿐이다.
{NO_BAND.format(band=band)}- 지금 그림에서 위 끝 난간 · 벽 띠가 있던 자리는 그 아래 바닥 판 결을 위로 이어 채운다. 좌우 물건은 위 끝에 닿아도 되지만 가로로 이어지는 띠는 안 된다.
- 크기 정확히 **720 x 540**. 가운데는 입구 길이라 비워 둔다.
{TAIL}[저장] Projects/AVSR/_exchange/in/bgfix_{tag}_apron.png (720 x 540, PNG)
""", f"{tag}_apron")


def boss_apron(b: str):
    s = BOSSES[b]
    floor = ROOT / f"{RF}/roomfloor_{b}.png"
    band = band_crop(floor, f"boss_{b}")
    return ps1(f"""[보스 방] {BOSS_KO[b]} 보스 방 **아래쪽 바깥**(입구 밑) 그림 한 장. **지금 그림을 고쳐 다시 그리는 것**이다.
[지금 그림 — 화풍 · 색 · 테마 물건 정답] {RF}/roomapron_boss_{b}.png
  화풍 · 색 · 도트 굵기 · 테마 물건 · 아래로 갈수록 어두워지는 것을 **그대로** 따른다. 바꾸는 것은 위 끝 하나뿐이다.
[같은 화풍 무대] Projects/AVSR/_exchange/bg_apply/{s}/roomapron_{s}.png
{NO_BAND.format(band=band)}- 지금 그림에서 위 끝 난간 · 관 · 벽 띠가 있던 자리는 그 아래 바닥 판 결을 위로 이어 채운다. 좌우 테마 물건은 위 끝에 닿아도 되지만 가로로 이어지는 띠 · 관은 안 된다.
- 크기 정확히 **720 x 540**. 가운데는 입구 길이라 비워 둔다.
{TAIL}[저장] Projects/AVSR/_exchange/in/bgfix_boss_{b}_apron.png (720 x 540, PNG)
""", f"boss_{b}_apron")


def holding_floor():
    return ps1(f"""[무대] **포로 수용실** — 챕터마다 중간 보스 방(10번 · 20번 방)이 쓰는 방 **바닥 한 장**. 원작 5스테이지 「포로 수용실」이 뿌리다.
[테마 · 색 · 틀 정답] {RF}/roomfloor_env_holding.png (지금 것 — **붉은 · 자줏빛 수용 시설**이라는 정체 · 대표 색 · 위아래 벽 띠 자리 · 바닥 가운데 큰 팔각 구획을 살린다. 흐릿한 대리석 얼룩 · 뿌연 붉은 안개 화풍은 버린다)
[화풍 정답 — 새로 바뀐 무대 바닥] Projects/AVSR/_exchange/bg_apply/junkyard/roomfloor_env_junkyard.png · Projects/AVSR/_exchange/bg_apply/lab/roomfloor_env_lab.png · Projects/AVSR/_exchange/bg_apply/refinery/roomfloor_env_refinery.png
  이 세 장처럼: 또렷한 금속 바닥 판 · 이음선 · 낮은 대비 · 위아래 벽 띠는 난간 · 패널 · 작은 램프 도트. 같은 도트 굵기 · 같은 단순함.
- 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방이고, **위 108 px · 아래 108 px 는 방 밖 벽 띠**, 좌우 끝 약 24 px 는 가장자리 벽이다 — 화풍 정답 바닥들의 띠 자리 · 폭과 똑같이.
- 수용실다움은 벽 띠의 붉은 경고등 · 철창 패널 무늬, 바닥 가운데 큰 팔각 구획선 하나로만 낸다. 바닥은 큰 타일 결 하나 · 얼룩 · 금 · 핏자국 · 안개 없음.
- 대표 색은 지금 것처럼 어두운 자줏빛 · 적갈. 다른 무대(남색 · 청록 · 주황 · 회녹)와 한눈에 갈려야 한다.
- 빈 바닥: 캐릭터 · 물건 · 탄 · HUD 금지.
{TAIL}[저장] Projects/AVSR/_exchange/in/bgfix_holding_floor.png (720 x 1152, PNG)
""", "holding_floor")


def holding_apron():
    band_note = "Projects/AVSR/_exchange/in/bgfix_holding_floor.png 의 아래 168 px(아래 벽 띠 108 + 바닥 60)"
    return ps1(f"""[무대] **포로 수용실**(중간 보스 방) — 방 **아래쪽 바깥**(입구 밑) 그림 한 장.
[이어 붙는 바닥 — 화풍 · 색 정답] Projects/AVSR/_exchange/in/bgfix_holding_floor.png (새 수용실 바닥 — 이 그림 바로 아래에 붙는다)
[테마 정답] {RF}/roomapron_holding.png (지금 것 — 붉은 관 · 수용 장치라는 테마만. 뿌연 붉은 안개 · 번쩍이는 화풍은 버린다)
[같은 화풍 방 밖] Projects/AVSR/_exchange/bg_apply/lab/roomapron_lab.png · Projects/AVSR/_exchange/bg_apply/refinery/roomapron_refinery.png
{NO_BAND.format(band=band_note)}- 크기 정확히 **720 x 540**. 아래로 갈수록 점점 어두워진다. 좌우에 수용 장치 · 철창 같은 큰 덩어리 한두 개, 가운데는 입구 길이라 비워 둔다. 잔 부품 · 연기 · 안개 금지.
{TAIL}[저장] Projects/AVSR/_exchange/in/bgfix_holding_apron.png (720 x 540, PNG)
""", "holding_apron")


def run_file(name: str, items):
    lines = ["$env:OPENAI_API_KEY = $null", '$d = "C:\\won\\UnityProject\\AvengingSprit\\Projects\\AVSR\\_exchange"']
    for n in items:
        lines.append(f'powershell -NoProfile -ExecutionPolicy Bypass -File "$d\\order_bgfix_{n}.ps1" *> "$d\\log_bgfix_{n}.txt"')
    lines.append('"다 끝남"')
    (EX / f"run_bgfix_{name}.ps1").write_text("\n".join(lines) + "\n", encoding="utf-8-sig")


def main():
    REF.mkdir(parents=True, exist_ok=True)
    sample = [holding_floor(), stage_apron("junkyard", False)]
    rest = [holding_apron()]
    rest += [stage_apron(s, False) for s in STAGES if s != "junkyard"]
    rest += [stage_apron(s, True) for s in STAGES]
    rest += [boss_apron(b) for b in BOSSES]
    run_file("sample", sample)
    run_file("rest", rest)
    print("샘플", sample)
    print("나머지", len(rest), rest)


if __name__ == "__main__":
    main()
