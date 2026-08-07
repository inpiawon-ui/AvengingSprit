# 화면 설계서 — 타이틀 (Title)

> 기획 **Stage 2b**. 입력: [`AVSR_Content_Title.md`](../AVSR_Content_Title.md) · 상위 제약: [`AVSR_Decisions.md`](../AVSR_Decisions.md)
> ⚠️ **목업 없음 — 신규 설계.** 팔레트·로고 락업만 로비 목업에서 계승.

- **화면명**: `Title` · **소속 콘텐츠**: `진입·플로우 #11` · **UI 타입(최상위)**: `TitleMainUI` (`~UI`)
- **박스 목업**: `AVSR_Screen_Title.html` · **기준 해상도**: `720 × 1280` (9:16 네이티브)
- **Addressable**: `UI/Title/TitleMainUI` · 라벨 `label_title` · 아틀라스 `atlas/titlemainui`
- **Version**: `0.1` · **Last Updated**: `2026-08-07` · **Status**: `게이트 B2 검토 대기`

---

## 0. 화면 인벤토리 · 네비게이션 플로우 (1차 범위 3화면 공통)

| ID | 화면 | UI 타입 | 참고: constants.md §2 씬 — **씬 그룹핑은 Stage 3 소관, 여기서 정하지 않음** |
|----|------|---------|------------------------------|
| S1 | Title | `TitleMainUI` (`~UI`) | TitleScene |
| S2 | Lobby | `LobbyMainUI` (`~UI`) | LobbyScene |
| S3 | HostSelect | `HostSelectPanel` (`~Panel`) | LobbyScene (S2 직하) |

```
Boot ──▶ S1 Title ──(전면 탭)──▶ S2 Lobby ──┬─ CONTINUE ─┐
                                            ├─ CHAPTER ──┼─▶ S3 HostSelect ─(빙의 시작)─▶ GameScene
                                            └─ HOST ─────┘        └─(뒤로)─▶ S2
```
> 진입 버튼 3개 / **경로 2개** — `CONTINUE`·`CHAPTER` = `ChapterStart`, `HOST` = `HostBrowse`. **호스트 선택 화면은 1개.**
> S1은 되돌아오는 경로가 없다 (재진입 불가, 앱 재실행 시에만).

## 1. 목적 · 진입/이탈
- 목적: 앱 실행 직후 **"1991년 그 게임이다"** 를 각인시키는 브랜드 관문. 기능은 전면 탭 1종뿐.
- 진입: `BootScene` (모듈·테이블·UserData 준비 완료 후 `LoadingStyle.None`)
- 이탈: `TouchArea` 탭 → `LobbyScene` (`LoadingStyle.Overlay`). **되돌아오는 경로 없음.**

## 2. 레이아웃 개요
상하 3분할. **상단 여백(0~340)** = 배경 야경만 노출 / **중단(340~640)** = `TitleLogo` 단일 락업 /
**하단(860~1280)** = `TapToStartText` 점멸 프롬프트 + 법적 표기. 프레임·버튼 위젯은 하나도 두지 않는다
`[근거: 확정 #1 — 원작 아케이드의 "화면 하나 · 프롬프트 하나". 프레임 버튼을 두면 로비 CTA와 같은 언어가 되어 로비 축소판으로 읽힌다]`.

## 3. 요소 트리 (핵심)

> `이름` = 프리팹 GameObject 이름 = 클라 바인딩 키. 좌표는 `720×1280` 좌상단 원점 px.
> 범위: ✅ 1차 동작 / 🟡 표시만 / ⛔ 1차 밖

| 요소 이름 | UI 타입/컴포넌트 | 위치·anchor (x,y,w,h) | 표시 데이터 | 입력→이벤트 | 범위 |
|-----------|-----------------|----------------------|-------------|-------------|:---:|
| `TitleMainUI` | **`~UI`** / RectTransform | Stretch Full `(0,0~1,1)` · sizeDelta `(0,0)` | — | — | ✅ |
| `BackgroundImage` | Image | Stretch Full `(0,0,720,1280)` | 정적 야경 스프라이트 | — | ✅ |
| `TitleLogo` | Image | top-center `(69,380,582,306)` | 정적 로고 락업 — `logolockup` **크롭 ×3 정수 확대** | — | ✅ |
| `TapToStartText` | TMP_Text | top-center `(200,862,320,46)` | `TAP TO START` (비트맵 픽셀) | — | ✅ |
| `CopyrightText` | TMP_Text | bottom-center `(110,1178,500,32)` | IP·퍼블리셔 표기 `[TBD]` | — | ✅ |
| `VersionText` | TMP_Text | bottom-left `(14,1238,116,26)` | `Application.version` | — | ✅ |
| `TouchArea` | Button + Image(α=0) · **에셋 불필요** (스프라이트 없음, 레이캐스트 전용) | Stretch Full · **최하위 형제(최상단 렌더)** | — | 탭 → `TitleStartRequestedEvent` 발행 | ✅ |

- `TouchArea`는 **형제 순서 마지막**에 둔다 — 화면 전체 탭을 가로채는 유일한 입력이므로 항상 최상단이어야 한다.
- `PopupParent` **없음** — `~UI`이며 이 화면에서 여는 `~Popup`이 없다 (`05_prefabs.md`는 `~Panel`에만 요구).
- 설정 버튼·로딩 게이지·로고 3분할 **없음** `[근거: Content_Title §2 "뺀 요소와 근거"]`.

## 4. 상태·분기
- **분기 없다.** 첫 실행/재실행 동일 화면 (신규 유저 판정은 Boot의 `UserData` 초기화 책임).
- `TapToStartText`: `1.0s` 주기 점멸 (0.5 on / 0.5 off). `TitleLogo`: `0.4s` 알파 페이드인, **스케일 애니메이션 금지** `[확정 #16 정수 배율]`.
- **첫 탭 직후 `TouchArea` 즉시 비활성** — 중복 씬 전환 차단.
- 로딩: 탭 시점에 `label_lobby` 사전 로드가 미완이면 `LoadingStyle.Overlay`가 덮는다. 별도 로딩 UI 요소 없음.
- 에러 상태: 없음 (타이틀은 유저 데이터를 읽지도 쓰지도 않는다).

## 5. 연결
- 발행: `TitleStartRequestedEvent` (필드 없음) — `TouchArea` 탭
- 구독: `OnSceneLoaded` (프레임워크) — `NextScene == SceneNames.Title`일 때 초기화 + `label_lobby` 사전 로드 시작
- 다른 화면: **후행 = S2 Lobby (유일한 출구)**. 여는 팝업 없음.

## 6. 미정
- `[TBD — 이유: City Connection 라이선스 계약서 문안 확인 필요]` `CopyrightText` 정확한 표기 문구. 박스 목업은 자리만 확보.
- `[TBD — 이유: 원작 음원 사용 범위가 IP 확인(확정 #25)에 종속]` 타이틀 BGM.
- ~~`TitleLogo` 소스 배율~~ → **해소.** 로비 목업에서 락업을 크롭한 실측치가 **`194×102`** 로 확정되어, **×3 = `582×306`** 이 720폭에 정수 배율로 정확히 들어간다. 별도 제작하지 않고 `logolockup` 1장을 마스터로 쓴다 (`AVSR_AssetManifest.md` §B `source=CROP`).

## 개정 이력
| 버전 | 날짜 | 변경 |
|------|------|------|
| 0.1 | 2026-08-07 | 최초 작성 — 목업 부재 신규 설계. 요소 7종·전면 탭 단일 입력 확정 |
