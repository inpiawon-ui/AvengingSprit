"""사운드 파일 72개의 사용처 설명을 한곳에서 관리하고, 매칭 문서 5절과 매니페스트에 반영한다.

- AVSR_SoundUsage.md 의 "## 5. 파일별 설명" 절을 새로 쓰거나 교체한다(재실행해도 한 번만 들어간다).
- sound_manifest.json 각 항목에 usage · evidence 필드를 넣는다.
사용법: python describe_files.py <리포 루트>
"""
import json
import sys
from pathlib import Path

DOC = "Assets/BundleResource/Sounds/AVSR_SoundUsage.md"
MANIFEST = "Projects/AVSR/_sound_extract/sound_manifest.json"
SECTION = "## 5. 파일별 설명"
OLD_SAMPLES_NOTE = "- `Samples/`(OKI 원본 샘플)는 위 BGM·SFX 안에서 사운드 CPU가 조합해 쓰는 원재료라서, 개별 사용처를 매기지 않았다."
NEW_SAMPLES_NOTE = "- `Samples/`(OKI 원본 샘플) 37개의 개별 사용처는 5절에 있다. OKI2 샘플 1–22는 SFX 16–38과 1:1로 대응하고, OKI1 샘플은 BGM 드럼 파트다."

UNUSED = "원작 프로그램이 한 번도 요청하지 않음(요청 경로 전수 조사)"

# 파일 → (원작에서 쓰인 곳 · 상황, 근거 등급)
DESCRIPTIONS = {
    # ── BGM ──────────────────────────────────────────
    "BGM/bgm_01.wav": ("스테이지 1(건설 중인 철골 빌딩)·스테이지 3(밤거리·중식당) 일반 구간, 어트랙트 데모 플레이. 루프곡. → 챕터 1·3 일반 구간, 타이틀·어트랙트", "확정"),
    "BGM/bgm_02.wav": ("스테이지 1–5 보스전. 각 스테이지 마지막 구역에 들어서면 재생. 루프곡. → 챕터 1–5 보스전", "확정"),
    "BGM/bgm_03.wav": ("스테이지 2(붉은 철골 공장)·스테이지 5(파이프·드럼통 공장) 일반 구간. 루프곡. → 챕터 2·5 일반 구간", "확정"),
    "BGM/bgm_04.wav": ("스테이지 4(하수도) 보스 전까지 전 구간. 루프곡. → 챕터 4 일반 구간", "확정"),
    "BGM/bgm_05.wav": ("스테이지 6(기지 내부) 일반 구간(구역 0·1·4). 루프곡. → 챕터 6 일반 구간", "확정"),
    "BGM/bgm_06.wav": ("최종 보스 킹핀전(스테이지 6 마지막 구역). 루프곡. → 챕터 6 보스전", "확정"),
    "BGM/bgm_07.wav": ("보스 격파 징글. 6개 보스 모두 격파 직후 3.5초 한 번. → 스테이지 성공 종료", "확정"),
    "BGM/bgm_08.wav": ("스테이지 2 중간 구역(KEEP OUT 창고)·스테이지 6 구역 2(어두운 건물 내부). 루프곡. → 챕터 2·6 중간 구역", "확정"),
    "BGM/bgm_09.wav": ("스테이지 6 구역 3(어둠 속 나무 상자 구역). 루프곡. → 챕터 6 중간 구역", "확정"),
    "BGM/bgm_10.wav": ("컨티뉴 카운트다운(\"CONTINUE 10…\"). 에너지를 모두 잃으면 17초 한 번. → 실패·고스트 HP 0", "확정"),
    "BGM/bgm_11.wav": ("엔딩. 킹핀 격파 → 격파 징글 뒤 유령 독백·스태프 롤·THE END 동안 70.8초 한 번. → 엔딩 컷신", "확정"),
    "BGM/bgm_12.wav": ("이름 입력(랭킹) 화면. 게임오버 후와 엔딩 후 모두. 루프곡. → 기록·랭킹 화면", "확정"),
    # ── SFX (각각 OKI2 샘플 하나로 구성, 17번만 OKI1) ─────
    "SFX/sfx_16.wav": (f"{UNUSED}. 내용은 OKI2 샘플 1(0.12초 짧은 음정음) 한 번", "원작 미사용 확정"),
    "SFX/sfx_17.wav": ("코인 투입음. 크레딧이 오를 때마다. 원본 OKI1 샘플 15. → 로비 CONTINUE 진입·크레딧", "확정"),
    "SFX/sfx_18.wav": (f"{UNUSED}. 내용은 OKI2 샘플 2를 약 5.1초 주기로 반복하는 루프음", "원작 미사용 확정"),
    "SFX/sfx_19.wav": ("공통 피격음. 캐릭터가 공격에 맞는 순간. 근접·마법형 호스트(Amazon 2종·Guru·Ninja Shuriken·Magician 2종·SnowWoman·Vampire·Slugger)는 이것이 사실상 공격음. 원본 OKI2 샘플 3. → 오토어택 타격", "확정"),
    "SFX/sfx_20.wav": ("대폭발. 보스 파츠 파괴·6개 보스 격파 때 OKI2 샘플 4를 연쇄로 터뜨리는 루프음(약 5.3초 주기). 직전에 효과음 전체 정지. → 보스 격파", "확정"),
    "SFX/sfx_21.wav": (f"{UNUSED}. 내용은 OKI2 샘플 5(0.46초) 한 번", "원작 미사용 확정"),
    "SFX/sfx_22.wav": (f"{UNUSED}. 내용은 OKI2 샘플 6(0.25초) 한 번", "원작 미사용 확정"),
    "SFX/sfx_23.wav": ("아이템 획득. 유령 에너지 회복·열쇠·호스트 체력 회복 아이템을 먹는 순간. 원본 OKI2 샘플 7. → 아이템·골드 코인 획득", "확정"),
    "SFX/sfx_24.wav": ("Dragon (Green·Blue) 화염 브레스 공격음. 화면의 적을 모두 쓰러뜨리는 폭탄 아이템 획득음으로도 쓰임. 원본 OKI2 샘플 8. → 샐러맨더·청룡(드라군 추정) 공격, 전체 공격 아이템", "확정"),
    "SFX/sfx_25.wav": (f"{UNUSED}. 내용은 OKI2 샘플 9(2.64초) 한 번", "원작 미사용 확정"),
    "SFX/sfx_26.wav": (f"{UNUSED}. 내용은 OKI2 샘플 10(0.99초) 한 번", "원작 미사용 확정"),
    "SFX/sfx_27.wav": ("Slugger 배트로 탄을 받아쳐 되돌리는 반사음(플레이어·적 슬러거 모두). 원본 OKI2 샘플 11. → 슬러거 반사", "확정"),
    "SFX/sfx_28.wav": ("특수 슬롯 객체(종류 0x22)가 적을 때릴 때 19번 대신 나는 피격음. 원본 OKI2 샘플 12", "발생 조건 확정 · 대상 캐릭터 확인 불가"),
    "SFX/sfx_29.wav": ("Crusher 쇠구슬이 바닥에 떨어지는 충격음. 원본 OKI2 샘플 13. → 크러셔 보스 패턴", "확정"),
    "SFX/sfx_30.wav": (f"{UNUSED}. 내용은 OKI2 샘플 14(1.33초) 한 번", "원작 미사용 확정"),
    "SFX/sfx_31.wav": (f"{UNUSED}. 내용은 OKI2 샘플 15(1.21초 저음) 한 번", "원작 미사용 확정"),
    "SFX/sfx_32.wav": ("Robot Snakes 보스 공격음. Commando (Missiles Pack) 미사일 발사음으로도 추정. 원본 OKI2 샘플 16. → 로봇 스네이크 보스, 미사일 코만도", "보스 확정 · 코만도 추정"),
    "SFX/sfx_33.wav": ("총격. Gangster 2종·Hopper 2종·Commando (Machine Gun)이 쏠 때. 원본 OKI2 샘플 17. → 갱스터·폭력배·호퍼 2종·코만도(기관총) 공격", "확정"),
    "SFX/sfx_34.wav": ("여성 호스트 피격 비명. 빙의 중 에너지가 깎이는 순간(원작 호스트 번호 1·2·23–26). 원본 OKI2 샘플 18. → 여성 호스트 피격·사망", "확정"),
    "SFX/sfx_35.wav": ("남성 호스트 피격 비명. 빙의 중 에너지가 깎이는 순간(원작 호스트 번호 3–22). 원본 OKI2 샘플 19. → 남성 호스트 피격·사망", "확정"),
    "SFX/sfx_36.wav": ("Python·Sludge 보스가 솟구치며 공격할 때. 원본 OKI2 샘플 20. → 파이썬·슬러지 보스 패턴", "확정"),
    "SFX/sfx_37.wav": ("Ninja (Chain) 사슬 공격음. 원본 OKI2 샘플 21. → 닌자(사슬) 공격", "확정"),
    "SFX/sfx_38.wav": ("폭발형 공격음. Commando (Grenade) 수류탄·Robot 로켓·Kingpin 비행 기계 공격. 원본 OKI2 샘플 22. → 수류탄 코만도·로봇 공격, 킹핀 보스", "확정"),
    # ── Samples: OKI1 = BGM 드럼 파트 (악기 이름은 음향 특징 기준 — 사람 청취로 확정 필요) ──
    "Samples/oki1_001.wav": ("BGM 드럼 1채널 기본 박자. BGM 1–10·12 거의 전곡에서 가장 많이 울림. 짧고 음정 성분이 강함 → 킥(베이스) 드럼으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_002.wav": ("BGM 드럼 2·3채널. BGM 1·2·3·5·6·7·8·9·10. 0.53초 잡음형 → 스네어 드럼으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_003.wav": ("BGM 1·2·3·8 필인에서 004·005와 함께 짧게 사용. 음정 있는 0.65초 → 탐(높은 음)으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_004.wav": ("BGM 1·2·3·8 필인. 003보다 낮은 음정 → 탐(중간 음)으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_005.wav": ("BGM 1·2·3·8 필인. 가장 낮은 음정 → 탐(낮은 음)으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_006.wav": ("어떤 BGM·SFX에서도 재생되지 않음. 0.42초 고음 잡음형", "원작 미사용 확정"),
    "Samples/oki1_007.wav": ("BGM 8(250회)의 주력 타악, BGM 6·1에도 사용. 0.46초 중간 잡음형 → 클랩·림샷 계열로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_008.wav": ("BGM 드럼 3채널 잘게 쪼개는 박자. BGM 1·2·4·5·7·9·10·12. 0.48초 고음 잡음 → 닫힌 하이햇으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_009.wav": ("BGM 1·5·6·7·9·10·12 강조 박. 1.11초 고음 잡음 → 오픈 하이햇·심벌로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_010.wav": ("BGM 3·6 강조 박. 1.30초 여운 있는 잡음 → 크래시 심벌 계열로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_011.wav": ("BGM 6(364회) 주력, BGM 4·12·3에도 사용. 0.43초 짧은 고음 → 쉐이커·탬버린 계열로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_012.wav": ("BGM 3 강조(16회), BGM 10에서 한 번. 2.07초 긴 여운 잡음 → 긴 심벌로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_013.wav": ("BGM 4(하수도) 전용. 음정이 뚜렷한 0.40초 → 카우벨·우드블록 계열로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_014.wav": ("BGM 5(스테이지 6) 전용 주력 타악(147회). 0.31초 저역 잡음 → 스네어·클랩 변형으로 보임", "사용처 확정 · 악기 이름은 청취 확인 필요"),
    "Samples/oki1_015.wav": ("SFX 17 코인 투입음의 원본(2.09초)", "확정"),
    # ── Samples: OKI2 = 효과음 원본, SFX 16–38 과 1:1 ──
    "Samples/oki2_001.wav": ("SFX 16의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_002.wav": ("SFX 18의 원본(SFX 18이 이 샘플을 반복). 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_003.wav": ("SFX 19 공통 피격음의 원본", "확정"),
    "Samples/oki2_004.wav": ("SFX 20 대폭발의 원본(SFX 20이 이 샘플을 연쇄로 재생)", "확정"),
    "Samples/oki2_005.wav": ("SFX 21의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_006.wav": ("SFX 22의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_007.wav": ("SFX 23 아이템 획득음의 원본", "확정"),
    "Samples/oki2_008.wav": ("SFX 24 드래곤 브레스·전체 공격 아이템의 원본", "확정"),
    "Samples/oki2_009.wav": ("SFX 25의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_010.wav": ("SFX 26의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_011.wav": ("SFX 27 슬러거 탄 반사음의 원본", "확정"),
    "Samples/oki2_012.wav": ("SFX 28 특수 객체 피격음의 원본", "발생 조건 확정 · 대상 캐릭터 확인 불가"),
    "Samples/oki2_013.wav": ("SFX 29 크러셔 쇠구슬 충격음의 원본", "확정"),
    "Samples/oki2_014.wav": ("SFX 30의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_015.wav": ("SFX 31의 원본. 원작 게임에서 요청되지 않음", "원작 미사용 확정"),
    "Samples/oki2_016.wav": ("SFX 32 로봇 스네이크 공격음의 원본", "확정"),
    "Samples/oki2_017.wav": ("SFX 33 총격음의 원본", "확정"),
    "Samples/oki2_018.wav": ("SFX 34 여성 호스트 피격 비명의 원본", "확정"),
    "Samples/oki2_019.wav": ("SFX 35 남성 호스트 피격 비명의 원본", "확정"),
    "Samples/oki2_020.wav": ("SFX 36 파이썬·슬러지 공격음의 원본", "확정"),
    "Samples/oki2_021.wav": ("SFX 37 닌자(사슬) 공격음의 원본", "확정"),
    "Samples/oki2_022.wav": ("SFX 38 수류탄·로켓·킹핀 공격음의 원본", "확정"),
}

GROUPS = [
    ("### 5.1 BGM (12)", "BGM/"),
    ("### 5.2 SFX (23)", "SFX/"),
    ("### 5.3 Samples (37)", "Samples/"),
]


def build_section(items):
    lines = [
        SECTION,
        "",
        "사운드 파일 72개 전부의 사용처다. 파일 옆 **→** 뒤는 프로젝트에서 대응하는 곳이다.",
        "",
        "- Samples 사용처는 사운드 CPU가 곡을 연주할 때 OKI 칩에 보내는 재생 명령을 곡마다 추적해 얻었다.",
        "- 드럼 악기 이름은 소리 길이·잡음 성분·음정 성분으로 판단한 것이라, 사람이 들어 보고 확정해야 한다.",
        "",
    ]
    by_dst = {item["dst"]: item for item in items}
    for title, prefix in GROUPS:
        lines += [title, "", "| 파일 | 길이 | 원작에서 쓰인 곳 · 상황 | 등급 |", "|---|---|---|---|"]
        for dst in sorted(d for d in DESCRIPTIONS if d.startswith(prefix)):
            usage, grade = DESCRIPTIONS[dst]
            length = by_dst[dst]["lengthSec"]
            lines.append(f"| `{dst}` | {length:.2f}초 | {usage} | {grade} |")
        lines.append("")
    return "\n".join(lines)


def main(root):
    root = Path(root)
    manifest_path = root / MANIFEST
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    items = manifest["items"]
    dsts = {item["dst"] for item in items}
    missing = sorted(dsts - DESCRIPTIONS.keys())
    extra = sorted(DESCRIPTIONS.keys() - dsts)
    if missing or extra:
        raise SystemExit(f"매니페스트와 설명 목록이 다르다 — 설명 없음 {missing}, 매니페스트에 없음 {extra}")
    for item in items:
        item["usage"], item["evidence"] = DESCRIPTIONS[item["dst"]]
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

    doc_path = root / DOC
    doc = doc_path.read_text(encoding="utf-8")
    if SECTION in doc:
        doc = doc[:doc.index(SECTION)]
    # 5절 앞 구분선을 매번 새로 붙이므로, 남아 있는 끝 공백·구분선을 모두 걷어내야 재실행해도 늘지 않는다
    doc = doc.rstrip()
    while doc.endswith("---"):
        doc = doc[:-3].rstrip()
    if OLD_SAMPLES_NOTE in doc:
        doc = doc.replace(OLD_SAMPLES_NOTE, NEW_SAMPLES_NOTE)
    elif NEW_SAMPLES_NOTE not in doc:
        raise SystemExit("2.2절 Samples 안내 문장을 찾지 못했다")
    doc = doc.rstrip() + "\n\n---\n\n" + build_section(items)
    doc_path.write_text(doc, encoding="utf-8", newline="\n")
    print(f"described {len(items)} files → {DOC}, {MANIFEST}")


if __name__ == "__main__":
    main(sys.argv[1])
