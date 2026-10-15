# 에셋 매니페스트 — 추가 모드 (망령의 탑 · 망령의 밤)

- **Version**: `1.0` · **Last Updated**: 2026-10-15 · **Status**: 부품 납품 · 자르기 끝 — 유니티가 비면 적용
- 화면 설계서: [`AVSR_Screen_Modes.md`](AVSR_Screen_Modes.md) · 통과 시안: `_exchange/in/mock_mode_{L,T1,T5,T8,N1,N3,N7}.png` · 묶음 `_exchange/mode_ui_mocks_sheet.png`
- 부품(게임 크기 · 투명): `Projects/AVSR/_exchange/out/mode_ui/` · 정리판 `_parts_board.png` · 시안 대조 `_compare.png`
- 발주 `Tools/order_mode_ui_parts.py` · 자르기 `Tools/cut_mode_ui_parts.py` · 옮기기 `Tools/apply_mode_ui_parts.py` · 임포트 `_exchange/out/mode_ui/ModeUiImport.cs.txt`

---

## 1. 적용 순서 (유니티가 비었을 때)

```
1. python Projects/AVSR/Tools/apply_mode_ui_parts.py          ← 그림을 Assets/BaseResource/ 아래로 (아래 2절 표)
2. _exchange/out/mode_ui/ModeUiImport.cs.txt → Assets/Scripts/Editor/ModeUiImport.cs 로 복사 → 컴파일 확인
3. 메뉴 Tools/Game/모드 UI/임포트 설정                          ← Sprite · Point · 9분할 · 새 아틀라스 4개 · 큰 바탕 ASTC
4. python Projects/AVSR/Tools/resource_audit.py               ← 새 파일 경고 0
5. 프리팹 · 화면 (화면 설계서 순서) → 시안과 같은 배율로 나란히 캡처해 대조
```

## 2. 새 그림

| 납품(out/mode_ui) | 넣을 곳 `Assets/BaseResource/` | 크기 | 9분할 L·B·R·T | 쓰는 노드 (화면 설계서) |
|---|---|---|---|---|
| `tower_plate` | `InGameMainUI/tower_plate` | 136×128 | — | `TowerPlate` (T5 · T6) |
| `night_plate` | `InGameMainUI/night_plate` | 144×106 | — | `NightPlate` (N2 ~ N5) |
| `mode_chip` | `InGameMainUI/mode_rule_chip` | 160×46 | 23·0·23·0 | `TowerRuleChip` · `NightWaveChip` · `NightCounterHint` |
| `mode_band` | `InGameMainUI/mode_intro_band` | 720×207 | — | `TowerIntroBanner` · `NightStartHintBanner` · `NightBannerText` 바탕 (밤도 같은 띠) |
| `bighost_plate` | `InGameMainUI/bighost_plate` | 204×92 | — | 대형 호스트 머리 위 이름 · 체력 · 경직 홈 (N3 · N4) |
| `mode_frame` | `ModeCommon/mode_frame` | 340×120 | 16·16·16·16 | `TowerWeekBar` · `TowerSweepBox` · 입장 줄 · `NightFlowRow` 칸 · `NightBestBox` · `NightRewardBox` · 결과 창 안 칸 |
| `entry_pip_1` / `_2` | `ModeCommon/mode_entry_pip_on` / `_off` | 24×24 | — (켜짐 점은 번진 빛을 반경 11px 에서 끊음 — 코덱스 검수) | `TowerEntryPip` · `NightEntryPip` · 결과 창 입장 점 |
| `tower_ladder_cell_1` | `TowerEntryPanel/tower_ladder_cell_past` | 270×96 | 24 | `TowerLadderCell` 지난 층 |
| `tower_ladder_cell_2` | `TowerEntryPanel/tower_ladder_cell_next` | 270×96 | 24 | 앞 층 |
| `tower_ladder_cell_3` | `TowerEntryPanel/tower_ladder_cell_now` | 270×96 | 26 | 지금 층 |
| `tower_ladder_cell_4` | `TowerEntryPanel/tower_ladder_cell_boss` | 270×96 | 24 | 보스 층(왼쪽 초상 칸 — 초상은 `ch_thumbframe` 안에 얹지 말고 그 자리에 직접) |
| `tower_badge_go` | `TowerEntryPanel/tower_badge_go` | 78×40 | 12·0·12·0 | 지금 층 칸의 「도전」 표식 |
| `tower_floor_card` | `TowerEntryPanel/tower_floor_card` | 400×470 | 24 | `TowerFloorCard` · **T2 · T3 · T4 팝업 틀도 이것**(따로 발주 안 함) |
| `night_day_cell_1` / `_2` | `NightEntryPanel/night_day_cell` / `_today` | 124×112 | — | `NightDayCell` 보통 / 오늘 |
| `night_today_card` | `NightEntryPanel/night_today_card` | 690×445 | 40 | `NightTodayCard` |
| `night_today_art` | `NightEntryPanel/night_today_art` | 340×190 | — | 오늘 카드 오른쪽 위 그림(왼쪽이 어둡게 사라진다 — 글자 밑) |
| `night_result_frame` | `NightResultPopup/night_result_frame` | 662×876 | — | `NightResultPopup` 틀(안쪽 칸 · 버튼은 기존 것 얹음) |
| `modeart_tower` | `LobbyMainUI/modeart_tower` | 330×150 | — | `ModeDefenseCard` 그림(지금 `modeart_defense` 대신) |
| `tower_panel_bg` | `ModeBackdrop/tower_panel_bg` | 720×1280 | — | `TowerEntryPanel` 바탕 — 아틀라스 밖 · ASTC 4x4 |
| `night_panel_bg` | `ModeBackdrop/night_panel_bg` | 720×1280 | — | `NightEntryPanel` 바탕 — 아틀라스 밖 · ASTC 4x4 |

- 9분할 값은 그림에서 잰 값(모서리 장식이 끝나는 줄)에 여유 2~3px. 늘려 보고 모서리가 깨지면 그 값만 고친다.
- 새 아틀라스: `ModeCommon` · `TowerEntryPanel` · `NightEntryPanel` · `NightResultPopup` (Atlas 폴더 · 주소 `atlas/{소문자}` 는 후처리기가 건다). `InGameMainUI` · `LobbyMainUI` 는 있는 아틀라스에 들어간다.
- 모든 도트 부품: Sprite · Point · 밉맵 끔 · Read/Write 끔 · 압축은 아틀라스(안드로이드 ASTC 4x4) — 규칙 `09_import_settings.md`.

## 3. 있는 그림을 그대로 쓰는 것 (발주 안 함)

| 쓰는 곳 | 있는 그림 |
|---|---|
| 뒤로 버튼(T1 · N1) | `ChapterHost/ch_back.png` |
| 제목 띠(T1 · N1 「망령의 탑」 · 「망령의 밤」) | `ChapterHost/ch_titlebar.png` |
| 큰 금 버튼(「13층 도전 ▶」 · 「시작 ▶」 · 결과 창 「다음 / 다시」) | `ChapterHost/ch_start_button.png` · `ChapterScreens/button_yellow_wide.png` |
| 청록 버튼(「소탕」 · 「육성하러 가기」) | `LobbyMainUI/buttonblue.png` |
| 어두운 버튼(「나가기」 · 「닫기」 · 「취소」) | `ChapterScreens/button_back.png` |
| 「＋ 추가 입장」 작은 버튼 | `ChapterHost/ch_pill.png` + `ch_icon_gem.png` |
| 탑 결과 창(T8 · T9) | `ChapterScreens/result_frame.png` · `result_rowpanel.png` · `result_subplate.png` (통과한 T8 시안이 챕터 결과 창 그대로) |
| 초상 칸 | `ChapterHost/ch_thumbframe.png` · `Growth/Faces/face_{key}.png` |
| ✓ · 별 · 해골 | `ChapterHost/ch_check.png` · `ch_star_on.png` · `ch_icon_skull.png` |
| 금화 · 젬 | `ChapterHost/ch_icon_coin.png` · `ch_icon_gem.png` |
| 상성 보석 | `InGameMainUI/rps_gem_{blade,force,magic}.png` |
| 나가기 자물쇠 | `LobbyMainUI/modelockicon.png` |
| 딤 | `ChapterHost/ch_dim.png` |

## 4. 코덱스 검수 (2026-10-15, `_exchange/review_modeui_parts_answer.md`)

17종 중 15종 통과. 지적 둘과 처리:
- `entry_pip_1` 켜짐 점의 빛이 동그라미 밖으로 번짐 → 자르기에서 반경 11px 밖을 끊었다(그림은 그대로).
- `tower_ladder_cell` 1(지난) · 2(앞)이 너무 비슷 · 4(보스) 안쪽 붉은 네모가 어색 → **다시 발주하지 않는다.**
  시안에서도 지난 층은 칸 자체가 아니라 **흐리게(알파 0.6) + ✓** 로 구분했고, 보스 칸 붉은 네모는 **보스 초상이 앉는 자리**다(초상이 덮는다).
  적용 뒤 화면에서 구분이 약하면 그때 1장만 다시 받는다.

## 5. 남은 것

- 밤 전용 띠(보라)는 따로 발주하지 않았다 — 탑 띠(`mode_intro_band`)를 같이 쓴다. 밤 화면에서 어색하면 그때 변형 1장.
- 글자는 그림에 없다 — 전부 TMP · 언어팩 키(화면 설계서 6절). 3언어 넣고 칸 확인.

## 개정 이력

| 버전 | 날짜 | 변경 |
|---|---|---|
| 1.0 | 2026-10-15 | 최초 — 부품 17종(21장) 납품 · 자르기 · 9분할 실측 · 옮기기 · 임포트 도구 준비 |
