# 인게임 배경 심플화 — 부품 발주문(.ps1) · 시안 크롭 앵커를 만든다 (PD 2026-10-29)
# 결과: _exchange/order_bg_*.ps1 · _exchange/ref/bg_order/mock_*_crop.png
# 쓰는 법: python Projects/AVSR/Tools/bg_simple_orders.py  [이름 ...]  (이름을 주면 그 발주문만 다시 만든다)
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
EX = ROOT / "Projects/AVSR/_exchange"
REF = EX / "ref/bg_order"

STAGES = ["junkyard", "missile", "street", "rooftop", "lab", "refinery"]
KO = {"junkyard": "쓰레기 집적장", "missile": "미사일 기지", "street": "밤 도시 거리",
      "rooftop": "옥상", "lab": "연구소", "refinery": "정유소"}
MOCK = {"junkyard": "in/mock_simple3_ch1.png"}
MOCK.update({s: f"in/mock_simple2_ch{i}.png" for i, s in enumerate(STAGES[1:], 2)})
MOCK_V2 = {s: f"in/mock_simple_obj_ch{i}.png" for i, s in enumerate(STAGES, 7)}
V2_NAME = {"junkyard": "불타는 집적장", "missile": "봉쇄된 저장기지", "street": "망령의 거리",
           "rooftop": "최후의 옥상", "lab": "감시 구역", "refinery": "그림자 공장"}

# 승인 시안에서 읽은 물건 — 블록 3종
OBJS = {
    "junkyard": "회색 철판 상자(시안에 가장 많은 블록) · 파랑과 갈색 드럼통 두 개 묶음 · 회청색 철 컨테이너 상자",
    "missile": "올리브색 군용 탄약 상자 · 미사일 탄두 2~3개를 세운 거치대 · 노랑-검정 경계 바리케이드",
    "street": "파란 신문 자판함 · 빨간 음료 자판기 · 빨간 소화전",
    "rooftop": "에어컨 실외기 · 기울어진 태양광 판 · 콘크리트 굴뚝 상자",
    "lab": "초록 액체가 든 배양 캡슐 · 계기판 달린 기계 콘솔 · 방사능 표시 드럼통",
    "refinery": "녹슨 원통 탱크 · 빨간 밸브 달린 배관 덩어리 · 철 부품 상자",
}
# 7~12 변형 — 시안 발주 때 정한 다섯 종류 그대로
OBJS_V2 = {
    "junkyard": "불씨 드럼통 · 녹아내린 고철 덩어리 · 불붙은 타이어 더미 · 쇠 압축 블록 · 깨진 용광로 통",
    "missile": "잠긴 철문 셔터 · 탄약 상자 더미 · 레이더 접시 · 미사일 거치대 · 경고 바리케이드",
    "street": "꺼진 네온 간판 · 묘비처럼 기운 표지판 · 버려진 공중전화 부스 · 쓰러진 가로등 · 문 닫힌 노점 수레",
    "rooftop": "물탱크 · 안테나 철탑 받침 · 헬기 착륙 표시 판(낮은 받침대 블록으로) · 쓰러진 광고판 · 환기 덕트 묶음",
    "lab": "감시 카메라 기둥 · 서버 랙 · 레이저 차단기 기둥 · 보안 게이트 · 경보등 상자",
    "refinery": "꺼진 작업등 · 컨베이어 벨트 토막 · 프레스 기계 · 쇠사슬 걸린 갈고리 기둥 · 부품 통",
}

# 다시 받을 때 보탠 말 (1차 검수 2026-10-29)
WATER_NOTE = {
    "lab": "- [다시 받음] 1차본은 바탕이 분홍이 아니라 어두운 색이었고 웅덩이 둘레에 빛 번짐이 둘러 있었다. **웅덩이 밖은 전부 완전한 분홍(#FF00FF) 한 색**, 번짐 · 그림자 금지.\n",
}
FLOOR_NOTE = {
    "rooftop": "- [다시 받음] 1차본은 시안보다 어둡고 탁한 갈색이었다. 시안 바닥의 **밝은 주황 · 살구빛 노을 톤**(밝기 · 채도)을 맞춘다. 대각선 햇살 무늬는 그리지 말고 색만 맞춘다.\n",
}
BLOCK_NOTE = {
    "missilev2": "- [다시 받음] 1차본 레이더 접시 옆에 시안 속 **적 로봇(올리브색 기계 병사)이 붙어 왔다.** 물건만 그린다 — 사람 · 로봇 · 적 · 캐릭터를 블록에 붙이지 마라. 레이더 접시는 받침대 위 접시 하나로.\n",
    "refinery": "- [다시 받음] 1차본은 1000 x 400 작은 그림에 오른쪽 두 물건이 칸 밖으로 잘렸고 분홍 테두리가 번졌다. 그림 크기를 넉넉히(가로 1500 px 이상) 잡고 물건이 **잘리지 않게 온전히** 그린다. 물건 테두리는 어두운 갈색 · 검정 도트로 끝내고 분홍이 섞이지 않게 한다.\n",
}

HEAD = """이미지 생성 도구로 직접 그린다. API 키를 쓰는 CLI 폴백은 쓰지 마라. 스크립트로 도형을 그려 때우지 마라. 한 장만 그린다.
게임 「Avenging Spirit RE:BORN」(도트 픽셀 아트, 세로 모바일 탑다운 액션) — 인게임 배경 심플화의 **게임에 넣을 부품**이다. 시안이 아니다.
먼저 Projects/AVSR/_exchange/guide_ingame_simple.md 를 읽는다(「새 챕터 · 새 무대 규칙」 다섯 줄).
"""
TAIL_RULES = """- 그림에 빛을 굽지 마라: 번지는 광채 · 아우라 · 반짝이 · 렌즈 빛 금지(빛은 게임에서 이펙트로 따로 낸다). 도트로 그린 작은 불빛 점 · 램프 몸통은 괜찮다.
- 글자 · 숫자 · 메모 · 테두리 액자 · 워터마크 금지.
"""


def ps1(body: str, name: str):
    text = ("$env:OPENAI_API_KEY = $null\n"
            "Set-Location \"C:\\won\\UnityProject\\AvengingSprit\"\n"
            "$prompt = @'\n" + HEAD + body + "'@\n"
            "codex exec --sandbox workspace-write --skip-git-repo-check $prompt\n")
    (EX / f"order_bg_{name}.ps1").write_text(text, encoding="utf-8-sig")


def crop(src: str, dst: str):
    if (REF / dst).exists():
        return
    im = Image.open(EX / src).convert("RGB")
    # HUD(위 190) · 조작 버튼(아래 1030~) 을 뺀 플레이 영역
    im.crop((0, 190, 720, 1030)).save(REF / dst)


def floor(s):
    ps1(f"""[무대] {KO[s]} — 전투 방 **바닥 한 장**.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/{MOCK[s]} (PD 승인 시안) — 플레이 영역만 자른 것: Projects/AVSR/_exchange/ref/bg_order/mock_{s}_crop.png
  이 시안의 **바닥 타일 · 좌우 가장자리 벽 · 색 · 명암 · 도트 굵기**를 그대로 따른다.
[틀 정답] Assets/BundleResource/RoomFloor/roomfloor_env_{s}.png (지금 게임 바닥 — 이것을 열어 틀을 잰다. 화풍은 따르지 마라)
  - 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방이고, **위 108 px · 아래 108 px 는 방 밖 벽 띠**, 좌우 끝 약 24 px 는 가장자리 벽이다 — 지금 파일의 띠 자리와 폭을 그대로 맞춘다.
  - 위 · 아래 띠와 좌우 벽은 시안의 가장자리 벽과 같은 재질로, 무대다움은 벽 띠의 조명 한 줄 · 패널 무늬로만 낸다.
[빼야 할 것] 시안의 장애물 블록 · 물건 · 캐릭터 · 적 · 적 탄 · 바닥 빨간 경고 띠 · 그림자 · HUD · 버튼은 **전부 빼고** 빈 바닥만 그린다(블록은 따로 깐다).
- 걸어 다니는 바닥: 큰 타일 결 하나 · 낮은 대비 · 낮은 채도 · 얼룩 · 금 · 낙서 없음.
- **시안보다 더 비우지 마라**: 시안 바닥에 있는 타일 크기 · 이음선 · 차선(노란 줄 등) · 패널 무늬와, 시안 좌우 벽에 붙은 무대 장식(벽에 선 미사일 · 경고 줄무늬 · 네온관 · 배관 등)은 시안 그대로 살린다. 시안에 없는 것만 더하지 마라.
{FLOOR_NOTE.get(s, "")}{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{s}_floor.png (720 x 1152, PNG)
""", f"{s}_floor")


def side(s):
    ps1(f"""[무대] {KO[s]} — 넓은 화면(태블릿)에서 방 **왼쪽 바깥**을 채우는 **세로 벽 띠 한 장**.
[화풍 · 색 정답] Projects/AVSR/_exchange/{MOCK[s]} — 시안의 좌우 가장자리 벽(크롭: Projects/AVSR/_exchange/ref/bg_order/mock_{s}_crop.png) 재질 · 색 · 도트 굵기를 그대로 잇는다.
[틀 정답] Assets/BundleResource/RoomFloor/roomside_{s}.png (지금 것 — 크기 · 쓰임만 본다, 화풍은 따르지 마라)
- 크기 정확히 **160 x 320**. 게임이 이 그림을 **세로로 끝없이 이어 붙인다** — 위 끝과 아래 끝이 이음매 없이 이어져야 한다(위아래 반복 무늬).
- **오른쪽 변이 방 바닥에 맞닿는 앞면**이다(오른쪽 게임 벽은 이 그림을 좌우로 뒤집어 쓴다). 오른쪽 10~20 px 에 벽 앞 모서리(밝은 윗선 + 어두운 앞면)를 두고 왼쪽으로 갈수록 벽 위 · 바깥 구조물로 이어진다.
- 단순하게: 큰 패널 · 기둥 결 하나와 시안의 벽 조명 한 줄 정도. 잔 부품 · 흩어진 물건 금지.
{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{s}_side.png (160 x 320, PNG)
""", f"{s}_side")


def blocks(s, v2=False):
    n = 5 if v2 else 3
    objs = OBJS_V2[s] if v2 else OBJS[s]
    mock = MOCK_V2[s] if v2 else MOCK[s]
    title = f"{V2_NAME[s]}(7~12챕터 변형 — 무대 {KO[s]})" if v2 else KO[s]
    tag = f"{s}v2" if v2 else s
    ps1(f"""[무대] {title} — 전투 방 **장애물 블록 {n}종**을 한 장에.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/{mock} (PD 승인 시안) — 플레이 영역 크롭: Projects/AVSR/_exchange/ref/bg_order/mock_{tag}_crop.png
  시안 속 물건의 **모양 · 색 · 도트 굵기 · 명암**을 그대로 옮긴다. 시안에서 그 물건을 찾아 같은 것으로 그린다.
[그릴 것 {n}종 — 왼쪽부터 이 순서] {objs}
[틀 정답] Assets/BaseResource/InGameMainUI/obj_{s}_block_1.png (지금 게임 블록 — 칸 크기 · 위에서 비스듬히 내려다본 각도 · 바닥에 선 자리만 본다. 화풍은 따르지 마라)
- 블록 하나 = 칸 하나를 꽉 채우는 **덩어리 하나**: 가로 1칸, 세로는 칸 가로의 0.8~1.4배. **윗면 밝게 + 앞면 어둡게** 두 면으로 높이가 보인다. 잔 부품을 흩뿌리지 마라.
- 바닥에 닿는 아래 변은 평평하게, 물건 밑에 그림자 판 · 바닥 조각을 붙이지 마라.
- 바탕은 **완전한 분홍(#FF00FF)** 한 색. 블록끼리 사이를 넉넉히(블록 폭의 절반 이상) 띄우고 **한 줄로** 늘어놓는다. 블록 안에 분홍을 쓰지 마라.
- 블록 하나를 대략 가로 200 px 크기로 크게 그린다(게임에서 72 px 로 줄인다 — 줄여도 읽히게 굵고 단순한 도트로).
{BLOCK_NOTE.get(tag, "")}{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{tag}_blocks.png (PNG)
""", f"{tag}_blocks")


def main():
    REF.mkdir(parents=True, exist_ok=True)
    only = set(sys.argv[1:])
    for s in STAGES:
        crop(MOCK[s], f"mock_{s}_crop.png")
        crop(MOCK_V2[s], f"mock_{s}v2_crop.png")
        for fn, name in ((floor, f"{s}_floor"), (side, f"{s}_side"), (blocks, f"{s}_blocks")):
            if not only or name in only:
                fn(s)
        if not only or f"{s}v2_blocks" in only:
            blocks(s, v2=True)


# ─────────────────────────────────────────
# 2차(PD 2026-10-29 「전체적으로 어울리게 다 바꿔줘」) — 물 · 가시판 · 울타리 · 방 밖 바닥 · 보스 방
BOSS_OF = {"robot_snakes": "junkyard", "crusher": "missile", "python": "street",
           "sludge": "rooftop", "guardian": "lab", "kingpin": "refinery"}
BOSS_KO = {"robot_snakes": "로봇 뱀", "crusher": "크러셔", "python": "파이썬",
           "sludge": "슬러지", "guardian": "가디언", "kingpin": "킹핀"}


def water(s):
    ps1(f"""[무대] {KO[s]} — 전투 방 바닥에 깔리는 **물 칸(웅덩이)** 한 장. 몸은 못 지나가고 탄은 지나가는 칸이다.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/{s}/roomfloor_env_{s}.png (이 무대의 새 바닥 — 이 위에 깔린다) · Projects/AVSR/_exchange/{MOCK[s]} (PD 승인 시안)
[쓰임 정답] Assets/BaseResource/InGameMainUI/obj_{s}_water_1.png · obj_{s}_water_edge_n.png (지금 게임 물 — 칸 크기 · 둑 두께만 본다. 초록 진흙 · 주황 돌 화풍은 버린다)
- 분홍(#FF00FF) 바탕 가운데에 **위에서 내려다본 직사각형 웅덩이 하나**: 가로 5칸 × 세로 4칸 비율(가로:세로 = 5:4), 칸 하나 약 200 px(웅덩이 약 1000 x 800).
- 웅덩이 **안쪽 가장자리**에 둑(턱)을 두른다: 칸 크기의 약 1/3 두께, 새 바닥 · 벽과 같은 재질(금속 테 · 콘크리트 턱)로 윗면 밝게 + 안쪽 면 어둡게 — 물이 한 단 낮아 보이게.
- 수면: **새 바닥보다 확실히 어둡게(밝기 절반쯤)** 하고 물빛(푸른 · 청록 기운)이 돌아 한눈에 「물 · 구덩이」로 읽혀야 한다 — 바닥과 같은 색이면 철 테두리 두른 바닥으로 보여 실패다(1차 샘플에서 그랬다). 큰 결 하나 · 밝은 잔물결 몇 줄로 단순하게. 쓰레기 · 기름 얼룩 · 돌 · 풀 · 거품 덩어리 금지. 적 탄이 수면 위에서도 또렷이 보여야 한다.
- 웅덩이 네 모서리는 직각(둥글게 깎지 마라). 칸 경계선은 그리지 마라. 분홍은 웅덩이 밖에만.
{WATER_NOTE.get(s, "")}{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{s}_water.png (PNG)
""", f"{s}_water")


def hazards(s):
    ps1(f"""[무대] {KO[s]} — 바닥 물건 **4개**를 한 장에: ① 가시판 ② 가시판(조금 다른 배열) ③ 낮은 울타리 ④ 낮은 울타리(조금 다른 모양).
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/{s}/obj_{s}_block_1.png ~ _3.png (이 무대의 새 장애물 블록 — 같은 화풍 · 도트 굵기 · 명암으로) · Projects/AVSR/_exchange/{MOCK[s]} (PD 승인 시안)
[쓰임 정답] Assets/BaseResource/InGameMainUI/obj_{s}_spike_1.png · obj_{s}_fence_1.png (지금 것 — 크기 비율과 쓰임만 본다. 화풍은 버린다)
- 가시판: 바닥에 납작하게 깔린 정사각 판(위에서 내려다봄), 밟으면 아프다는 게 한눈에 — 판 위에 짧고 굵은 가시 3x3 줄. 판 테두리에 노랑-검정 경고 줄무늬 정도. 정사각 칸을 꽉 채운다.
- 낮은 울타리: 칸 하나 폭의 낮은 철책(몸은 막고 탄은 넘어간다) — 블록보다 키가 낮고 사이가 비쳐 보이는 막대 · 철망. 가로 1 : 세로 1.4 칸 안.
- 바탕은 **완전한 분홍(#FF00FF)**. 넷을 **한 줄로** 넉넉히(물건 폭의 절반 이상) 띄운다. 물건 하나 약 200 px 폭.
- 그림자 판 · 바닥 조각 금지.
{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{s}_hazards.png (PNG)
""", f"{s}_hazards")


def apron(s):
    ps1(f"""[무대] {KO[s]} — 전투 방 **아래쪽 바깥**(입구 밑) 그림 한 장. 방에 들어서기 전 화면 아래에 보이는 자리다.
[완성도 · 화풍 · 색 정답] Projects/AVSR/_exchange/bg_apply/{s}/roomfloor_env_{s}.png (이 무대의 새 방 바닥 — 이 그림의 **아래 끝 벽 띠 바로 밑에 붙는다**) · Projects/AVSR/_exchange/{MOCK[s]}
[틀 정답] Assets/BundleResource/RoomFloor/roomapron_{s}.png (지금 것 — 크기 · 자리만 본다. 고철 더미 · 연기 같은 복잡한 화풍은 버린다)
- 크기 정확히 **720 x 540**. 위 끝은 새 방 바닥의 아래 벽 띠와 같은 재질 · 색으로 이어지게 시작하고, 아래로 갈수록 방 밖(복도 · 계단 · 바깥 바닥)으로 이어지며 **점점 어두워진다**.
- 심플하게: 큰 패널 · 바닥 결 하나와 이 무대 물건 한두 개 정도. 잔 부품 · 흩어진 쓰레기 · 연기 금지. 가운데는 입구 길이라 비워 둔다.
{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_{s}_apron.png (720 x 540, PNG)
""", f"{s}_apron")


def boss_floor(b):
    s = BOSS_OF[b]
    ps1(f"""[보스 방] {BOSS_KO[b]} 보스 방 **바닥 한 장**.
[지금 것 — 테마 · 색 · 틀 정답] Assets/BundleResource/RoomFloor/roomfloor_{b}.png — 이 보스 방의 **대표 색 · 바닥 무늬의 뜻(가운데 원형 경기장 · 표시 등) · 위아래 띠 자리**를 살린다.
[화풍 정답] Projects/AVSR/_exchange/bg_apply/{s}/roomfloor_env_{s}.png (승인된 새 화풍 바닥 — 이것처럼 심플하게) · Projects/AVSR/_exchange/{MOCK[s]}
- 크기 정확히 **720 x 1152**. 가운데 720 x 936 이 걸어 다니는 방, 위 108 px · 아래 108 px · 좌우 약 24 px 는 벽 — 지금 파일의 띠 자리와 맞춘다.
- 바닥: 큰 타일 결 하나 · 낮은 대비 · 얼룩 · 금 · 연기 · 그을림 없음. 보스 방다움은 **바닥의 큰 표시 하나(원형 경기장 선 등)와 벽 띠**로만 — 지금 것의 표시를 단순하게 옮긴다.
- 캐릭터 · 보스 · 탄 · 물건 · HUD 금지(빈 바닥).
{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_boss_{b}_floor.png (720 x 1152, PNG)
""", f"boss_{b}_floor")


def boss_apron(b):
    s = BOSS_OF[b]
    ps1(f"""[보스 방] {BOSS_KO[b]} 보스 방 **아래쪽 바깥**(입구 밑) 그림 한 장.
[지금 것 — 테마 정답] Assets/BundleResource/RoomFloor/roomapron_boss_{b}.png — 이 보스의 **테마 물건 · 색**을 살린다(무엇이 있는 곳인지).
[이어 붙는 것] Projects/AVSR/_exchange/in/bg_boss_{b}_floor.png 가 있으면 그 아래 벽 띠, 없으면 Projects/AVSR/_exchange/bg_apply/{s}/roomfloor_env_{s}.png 의 아래 벽 띠 — 위 끝이 그 재질 · 색으로 이어지게.
[화풍 정답] Projects/AVSR/_exchange/bg_apply/{s}/roomfloor_env_{s}.png · Projects/AVSR/_exchange/{MOCK[s]} — 심플하게.
- 크기 정확히 **720 x 540**. 아래로 갈수록 어두워진다. 테마 물건은 크고 단순한 덩어리 한두 개로, 잔 부품 · 연기 · 흩어진 잔해 금지. 가운데는 입구 길이라 비워 둔다.
{TAIL_RULES}[저장] Projects/AVSR/_exchange/in/bg_boss_{b}_apron.png (720 x 540, PNG)
""", f"boss_{b}_apron")


def main2(only):
    for s in STAGES:
        for fn, name in ((water, f"{s}_water"), (hazards, f"{s}_hazards"), (apron, f"{s}_apron")):
            if not only or name in only:
                fn(s)
    for b in BOSS_OF:
        for fn, name in ((boss_floor, f"boss_{b}_floor"), (boss_apron, f"boss_{b}_apron")):
            if not only or name in only:
                fn(b)


if __name__ == "__main__":
    main()
    main2(set(sys.argv[1:]))
