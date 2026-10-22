# 3단계 조사 — 우리 쪽 실측 (2026-10-22, 읽기 전용 조사)

## 결론
- 15방 골격은 `RoomImporter60.SourceOf`(Assets/Scripts/Editor/RoomImporter60.cs:62-69) 한 곳 — 6챕터×10방 배정표 `RoomDef60` 을 방 15개로 다시 앉힌다. 방 종류는 원본 방 Kind 를 따른다.
- 런타임(HUD 「01 / 15」, 진행도, 엘리트)은 방 표를 세므로 30방이면 따라온다. 15·14·11 이 박힌 곳은 임포터 · 굽기 · 배율 · 음악 · 골드.
- 레벨업은 「싸움 있던 방을 비우면 +1」(BattleDirector.StageLevel.cs:43-52). `GainExp` 는 밤 모드만. 적별 경험치 없음(전역 10). 필요량은 공식(40×1.45^(Lv-1)).
- 챕터 수 = chapters.tsv 줄 수. 11챕터부터 막히는 곳: 방 지도 파일 · 상점 표 6챕터 · 무대 6종 · 로비 상품 10개.

## 1. 30방으로 바꿀 때 고칠 곳
| 위치 | 내용 |
|---|---|
| RoomImporter60.cs:46 | `RoomsPerChapter = 15` |
| RoomImporter60.cs:48-69 | 1~3 전투 · 4 천사 · 5~7 전투 · 8 중간보스 · 9~11 전투 · 12 상점 · 13~14 전투 · 15 보스 (`SourceOf` 15칸) |
| RoomImporter60.cs:610-615 | `LayoutOf` `no<=7` 앞뒤 나눔 |
| RoomImporter60.cs:140 | 메뉴 「× 15방」 |
| rooms90_build.py:216 | `COMBAT_NO` 전투방 11개 |
| rooms90_build.py:568, 577 | 배치도 `cols = 15` |
| rooms90_build.py:686-691 | 챕터마다 COMBAT_NO 다 있어야 통과 |
| ChapterTableImporter.cs:162 | `RunGold = RoomGold * 11 + Mid + Boss` |
| GameConfig.cs:697-705 | 방 배율 `Clamp(roomNo,1,14)/13` (1 → 1.6) |
| SoundTableBuilder.cs:305-311 | 중간 구역 곡 방 범위 |
| chapters.tsv · GameConfig 툴팁 | 「15번 방 보스 · 8번 방 중간보스」 |
| 따라오는 곳 | `ChapterRoomCount`(BattleDirector.cs:5471) · `RoomTotal`(:2930) · `RoomNumberOf`(:5482) · HUD(InGameMainUI.cs:1030) · `RoomProgress`(Room.cs:135) |

## 2. 방 지도 형식
- 10칸×16줄(1칸 = 1 m), 맨 윗줄 문 구역. 헤더 `[CH1_001] 이름 | 설명` · `legend:` · `map:`.
- 적 글자 = 소문자 한 글자 한 기, legend 값은 **몸 키**(`*` = 빼앗을 호스트). 역할은 적지 않는다.
- 틀 + 채우기로 바꾸려면: legend 를 역할 글자로 · build.py `TRASH[ch]`·`DEBUT` 검사를 역할 기준으로 · 틀 ID → 챕터 · 방 배정 표 신설 · 임포터 `WriteSpawnsHand`(:485)에서 챕터 trash 로 채우기 · `ChapterTableImporter` mix 집계(:113-125).
- 이미 있는 갈아끼우기: 목록에 없는 잡몹은 `TrashForSlot` 이 같은 역할로 · 같은 몸이면 진행도로 Lv.

## 3. 레벨업 · 경험치
| 항목 | 위치 |
|---|---|
| 레벨업 | `TickStageLevel`(StageLevel.cs:43-52) · 처치 때 `FillStageExp` 는 막대만 |
| 3택1 | `TickPendingLevelUp` → `OfferBuff`(BattleDirector.cs:8125-8150) |
| `GainExp` · `_exp` | BattleDirector.cs:8085-8109 · c002 배율 `_buffs.ExpGainMul` |
| c002 성장 가속 | CardImporter.cs:59-63 `live: false` |
| 적 경험치 | `ExpPerEnemy` 10 · `ExpPerBoss` 60(GameConfig.cs:120-122) — 몬스터 표에 칸 없음 |
| 필요량 | `ExpToNext` = 40×1.45^(Lv-1)(GameConfig.cs:437-442) |
| 죽은 값 | 방 `_exp`(RoomImporter60.cs:370-395)를 읽는 곳 없음 |

## 4. 특수 방
천사 4번(Rest → 회복 제단) · 중간보스 8번(깬 뒤 악마 제단) · 상점 12번 · 보스 15번 · 보물상자 몹 없음(BattleDirector.cs:10051 에서 없앰) · 엘리트는 뒤 절반.

## 5. 챕터 10 제한
GameConfig.cs:688 `ChapterCount = _chapters.Length` · 번호 이어짐 검사 · `rooms90_ch11.txt` 없으면 실패 · 무대 6종 외는 1로 떨어짐(RoomImporter60.cs:77-86) · ShopTable 6챕터 · BossTable 6 · 로비 상품 `chapter_01..10` · 문자열 `stage.{n}.1.name`.

## 6. 방 배율
`EnemyRoomMul` = 1 + 0.60×(room−1)/13 · 챕터 배율 hpMul · atkMul 곱.
