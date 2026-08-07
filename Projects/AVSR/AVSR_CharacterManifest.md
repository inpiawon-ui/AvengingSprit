# 캐릭터 에셋 매니페스트 — AVSR (호스트 12종 + 고스트)

> **UI 에셋과 파이프라인이 다르다.** UI는 정지 프레임·9-slice·시트 배치 생성이고,
> 캐릭터는 **애니메이션 프레임 일관성**이 전부다. 그래서 문서를 분리한다.
> UI 에셋: [`AVSR_AssetManifest.md`](AVSR_AssetManifest.md) · 상위 제약: [`AVSR_Decisions.md`](AVSR_Decisions.md)
> 워크플로우 표준: [`Template/Prompt_Registry_Template.md`](../../Template/Prompt_Registry_Template.md) §0
> **Version** `0.1` · **Last Updated** `2026-08-07` · **Status** `게이트 B2 검토 대기`

---

## 0. 사용자 확정 (변경 불가)

| 항목 | 값 |
|------|-----|
| 소스 규격 | **48 × 48 px** · 화면 표시 **×2 = 96 px** (정수 배율) |
| 상태 범위 | **Idle · Move · Attack** 3종. Hit·Death·Possessed·Ultimate = 다음 마일스톤 |
| 대상 | 호스트 **12종** + 고스트 본체 |
| 생성 도구 | **ChatGPT 단일.** ComfyUI 미사용. 개별 PNG + ZIP 납품 |
| 생성 단위 | **호스트 1종씩 개별 생성** (UI처럼 여러 캐릭터를 한 배치로 묶지 않는다) |

---

## 1. 공통 아트 규율 (전 캐릭터 · 검수 기준)

`AVSR_AssetManifest.md` §0의 `gpt_style_base`를 **그대로 상속**한다. 캐릭터 전용 추가 규율만 여기 적는다.

| 항목 | 값 | 근거 |
|------|-----|------|
| 비율 | **2등신 SD(치비) 고정** | `[근거: Reference/Hosts/ 12종 앵커 실측 — 머리:몸 ≈ 1:1]` |
| 시점 | **정면(2.5D front-facing).** 탑다운 카메라지만 스프라이트는 정면 | `[근거: ingame_hd_scene 육안 — 어느 방향으로 공격하든 전원 카메라를 본다]` |
| 색 수 상한 | 캐릭터당 **16색 + 아웃라인 1색** (투명 제외) | `[도출: 앵커 실측 12~20색. 검수 B(팔레트 절제) 통과를 위한 하드 상한]` |
| 아웃라인 | 1px 다크 아웃라인 필수 (`#0A0812` 계열) | `[근거: 앵커 12종 전부 다크 아웃라인. 어두운 배경에서 실루엣 분리]` |
| 팔레트 | `AVSR_Decisions.md` §5. **네온 퍼플·마젠타 금지** | 확정 §5 |
| 픽셀 규율 | Point 필터 · **정수 배율만** · 안티앨리어싱 금지 | 확정 #16 |
| 글자 | **이미지 안에 글자 금지** (앵커 하단 한글 라벨은 크롭 대상) | UI 매니페스트 §0-1 |

### 1-1. 48×48 프레임 내부 규약 — **필수**

```
 ┌──────────── 48 ────────────┐
 │ ◄9►  ┌── 몸통 ≤30 ──┐  ◄9► │   ← 좌우 9px = 무기·모션 스윙 여유
 │      │              │      │
 │      │   높이 ≤44   │      │
 │      └──────────────┘      │
 │  ─── 발 접지선 = y0 ───     │   ← 프레임 최하단. pivot 기준선
 └────────────────────────────┘
```

- **몸통 폭 ≤ 30px 중앙 정렬**, 좌우 각 9px은 무기·스윙·총구화염 여유.
- **발 접지선 = 프레임 최하단 y=0.** 전 프레임 동일. 이걸 어기면 재생 시 캐릭터가 위아래로 떨린다.
- 그림자·먼지·타겟 링은 **캐릭터 스프라이트에 그리지 않는다** (인게임 FX 소관, 다음 마일스톤).
- `[도출]` **48×48이 성립하는 이유는 발사체를 분리했기 때문이다.** 총열·마법탄·표창을 프레임 안에 그리려 하면 몸통이 뭉개진다. 프레임에는 **총구 화염까지만**, 날아가는 물체는 §5-3 발사체 에셋이 담당한다.

---

## 2. 결정 ① — 상태별 프레임 수

| 상태 | 프레임 | 재생 | 프레임 역할 | 근거 |
|------|:---:|------|------------|------|
| `Idle` | **4** | 8fps · 0.5s 루프 | ①기본 ②+2px 상승 ③기본 ④−2px 하강 (호흡) | `[도출: 2프레임은 1991 원작에 충실하나 96px 표시에서 딸꾹질처럼 읽힌다. 6프레임은 2px 진폭 안에서 차이가 보이지 않는다. 4가 하한이자 상한]` |
| `Move` | **6** | 12fps · 0.5s 루프 | ①접지L ②통과 ③최고점 ④접지R ⑤통과 ⑥최고점 | `[도출: 2등신은 다리가 짧아 4프레임 보행이 미끄러지듯 보인다. 접지-통과-최고점 3단계 ×2보 = 6이 걷기가 읽히는 최소 단위]` |
| `Attack` | **6** | 15fps · 0.4s 1회 | ①예비 ②예비 최대 ③릴리즈 ④**임팩트** ⑤후딜 ⑥복귀 | `[근거: 오토어택이라 가장 자주 재생된다. 예비-임팩트-복귀 3단계를 각 2프레임으로 잡아야 타격감이 산다. 4프레임이면 예비가 사라져 뚝 끊긴다]` |

- **발사체 스폰 프레임 = `Attack` ④번.** 클라 배선 기준점 `[도출: 임팩트 프레임과 발사 타이밍이 어긋나면 총구 화염과 탄이 따로 논다]`
- **호스트 1종당 4 + 6 + 6 = 16프레임.** 12종 = **192프레임**.
- ⚠️ 프레임 수를 늘리지 않는다. 늘리면 §4의 시트 일관성 리스크가 그대로 비례해 커진다.

---

## 3. 결정 ② — 방향 처리

### **정면 1벌 + 좌우 flip (Archero 방식) 채택**

| 근거 | 내용 |
|------|------|
| 목업 실증 | `ingame_hd_scene` 육안 — **12종 전원이 카메라를 정면으로 본다.** 오른쪽으로 사격하는 램보도 얼굴은 정면. 4방향 스프라이트가 아니다 |
| 확정 #5 | "목업 100% 재현". 목업이 정면 1벌이므로 4방향은 **규약 위반** |
| 물량 | 4방향 = 192 → **576프레임.** 확정 #23(1개월 내 1차 완료)과 양립 불가 |
| 일관성 | §4 참조 — 방향이 늘수록 얼굴·색이 방향마다 어긋난다. AI 생성에서 방향은 일관성 붕괴의 1순위 요인 |

**flip 운용 규칙**
- 캐릭터가 화면 왼쪽을 향할 때만 `scaleX = -1`. 위·아래 이동은 **정면 스프라이트 그대로** (목업과 동일).
- `[제약]` **좌우 반전 시 어색해지는 비대칭 요소를 그리지 않는다** — 한쪽 눈 안대, 한쪽 어깨 견장, 한쪽 팔 문신 금지.
  무기 파지 손(오른손 고정)은 반전을 허용한다 `[근거: 앵커 12종 전부 오른손 파지. 반전 시 왼손잡이가 되지만 도트 관습상 무시된다]`
- `[WARNING]` **`robot`은 팔 캐논이 한쪽 팔에만 달려 있다**(앵커 실측). 반전 시 캐논이 반대 팔로 넘어간다. **로봇 특성상 좌우 대칭 실루엣이므로 허용**하되, 검수에서 어색하면 캐논을 양팔에 그린다.

---

## 4. 프레임 일관성 — 이 문서의 핵심 문제

### 4-1. 채택 방식 — **상태별 그리드 1장 생성 → 가로 스트립 재배열**

같은 캐릭터를 6번 개별 생성하면 얼굴·비율·색이 6번 다 다르다. **한 번의 생성 안에서 뽑아야** 일관된다.
그러나 GPT 출력 비율은 최대 `1.5:1`이라 **6:1 가로 스트립을 직접 뽑을 수 없다.** 따라서 그리드로 뽑고 후처리에서 편다.

| 상태 | GPT 생성 크기 | 생성 레이아웃 | 셀 크기 | → 최종 시트 |
|------|--------------|--------------|---------|------------|
| `Idle` (4) | **1024 × 1024** | **2열 × 2행** | 512 × 512 | **192 × 48** (4×48) |
| `Move` (6) | **1536 × 1024** | **3열 × 2행** | 512 × 512 | **288 × 48** (6×48) |
| `Attack` (6) | **1536 × 1024** | **3열 × 2행** | 512 × 512 | **288 × 48** (6×48) |

- **읽기 순서 = 좌→우, 그다음 위→아래.** 프롬프트에 명시한다(이미지에 번호를 넣을 수 없으므로).
- **최종 시트 여백 = 0.** 프레임 경계는 정확히 48px 정렬. Offset 0 / Padding 0.

### 4-2. 셀 → 48px 다운스케일 절차 (정수 배율 보장)

```
512×512 셀  →  중앙 480×480 크롭·투명 패딩  →  ÷10 Nearest  →  48×48
                    (480 = 48 × 10)
```
`[도출: 512/48 = 10.67로 정수가 아니다. 480으로 먼저 맞추면 정확히 ÷10 정수 배율이 되어 확정 #16을 지킨다]`
프롬프트에 **"각 픽셀 블록이 정확히 같은 크기인 48×48 격자 위에 그릴 것"**을 넣어 GPT가 블록 단위로 그리도록 유도한다.

### 4-3. 이 방식의 한계 — **반드시 인지하고 시작할 것**

| # | 한계 | 완화 |
|---|------|------|
| 1 | **포즈 제어가 안 된다.** "6칸에 걸쳐 점진적으로 변하는 시퀀스"를 지시대로 배치하지 않는다. 같은 포즈가 반복되거나 무관한 포즈가 섞인다 | 프레임 역할(§2 표)을 **칸별로 한 문장씩** 프롬프트에 나열한다. 그래도 완벽하지 않다 |
| 2 | **재시도 비용이 크다.** 6칸 중 1칸만 틀려도 **시트 전체를 재생성**해야 한다. 1칸만 다시 뽑으면 그 칸만 다른 캐릭터가 된다 | 히어로 1종으로 포맷을 먼저 검증한 뒤 나머지 11종에 착수한다 (§7) |
| 3 | **읽기 순서가 뒤섞인다.** 좌→우 진행 순서를 보장하지 않는다 | **치명적이지 않다.** 후처리에서 사람이 순서를 재배열해 흡수한다 |
| 4 | 셀마다 캐릭터 크기·발 높이가 미세하게 다르다 | 크롭 단계에서 **발 접지선을 기준으로 정렬**한다(§1-1). 중앙 정렬로 자르면 캐릭터가 떤다 |
| 5 | 포즈 흔들림이 유독 심한 상태가 생긴다 | 그 상태만 **프레임 수를 줄여 재시도**한다 (Attack 6→4). 프레임을 늘려 해결되는 문제가 아니다 |

> 그럼에도 개별 생성보다 낫다. 개별 6회는 **얼굴이 6번 다 다르게 나오고 완화책이 없다.**

---

## 5. 결정 ③ — 호스트별 Attack 모션 차별화

**12종의 역할이 전부 다르다.** 팔 휘두르기 12벌은 이 게임의 차별점을 죽인다.
`역할` = `AVSR_Decisions.md` §4 · `모션` = 앵커 이미지 육안 확인 기반.

### 5-1. 근접 3종 (발사체 없음)

| # | hostKey | 역할 | Attack 모션 (④ 임팩트 프레임 기준) |
|:-:|---------|------|-----------------------------------|
| 1 | `amazoness` | 고속 근거리 | **창 2연속 찌르기.** 창을 몸 뒤로 당겼다가(①②) 오른쪽으로 곧게 뻗는다(④). 창끝에 짧은 백색 슬래시 잔상 |
| 11 | `slugger` | 탄환 반사 | **배트 풀스윙.** 배트를 뒤로 크게 젖혔다가(②) 좌→우 수평 호를 그린다(④). 스윙 궤적에 잔상 호 |
| 12 | `vampire` | 흡혈 전투 | **망토 전개 + 양손 손톱 할퀴기.** 망토를 활짝 펼치며(②) 상체를 앞으로 숙여 X자로 긁는다(④). 적색 할퀸 자국 2줄 |

### 5-2. 원거리 9종 (발사체 필요)

| # | hostKey | 역할 | Attack 모션 (④ 임팩트 프레임에서 발사체 스폰) |
|:-:|---------|------|---------------------------------------------|
| 2 | `rambo` | 중화기 사격 | **기관총 연사.** 총을 어깨에 고정, 총구에 큰 십자 화염, **반동으로 몸 전체가 1~2px 뒤로 밀린다**(⑤에서 복귀). 탄피 배출 |
| 3 | `wizard` | 마법 시전 | **지팡이 오브 시전.** 지팡이를 머리 위로 들어올리며 오브가 단계적으로 밝아지고(①→③) 앞으로 내리찍으며 마법탄 방출(④) |
| 4 | `ninja` | 표창 투척 | **표창 투척.** 팔을 어깨 뒤로 완전히 접었다가(②) 상체 회전과 함께 채찍처럼 뻗는다(④). 손이 비는 순간이 명확해야 한다 |
| 5 | `mafia` | 확산 사격 | **톰슨건 부채꼴 훑기.** 총구를 좌→우로 **호를 그리며** 연사(③④⑤ 각 프레임마다 총구 각도가 다르다). 총구 화염이 짧고 빠르다 |
| 6 | `hitman` | 정밀 저격 | **조준 후 단발.** ②③에서 **완전히 정지**(조준 홀드 — 유일하게 움직이지 않는 예비 동작), ④에서 소음기 총구에 작은 섬광 1점. 반동 최소 |
| 7 | `yogamaster` | 부양 서포트 | **공중 부양 합장 방출.** 가부좌 부양 상태 유지, 양손을 가슴 앞에 모아 구체를 압축했다가(②③) 양팔을 벌리며 방출(④). **발이 지면에 닿지 않는다** |
| 8 | `dragon` | 화염 브레스 | **화염 브레스.** 목을 뒤로 젖히며 목이 부풀고(②) 앞으로 뻗으며 입에서 화염을 뿜는다(④⑤ 2프레임 지속). 유일한 **지속형** 공격 |
| 9 | `robot` | 로켓 | **팔 캐논 로켓.** 캐논이 예열되며 붉게 발광(①→③), 발사와 함께 **캐논이 뒤로 슬라이드**(④), 배기 연기(⑤) |
| 10 | `snowwoman` | 얼음 컨트롤 | **얼음 결정 생성 후 사출.** 양손 사이에 육각 결정이 단계적으로 커지고(①→③) 손을 앞으로 내밀어 쏜다(④). 입김 서리 |

> `[WARNING]` `yogamaster`는 **부양 상태라 발 접지선 규약(§1-1)의 예외**다. 프레임 최하단 y=0은 **부양 그림자 위치가 아니라 가부좌 밑면**으로 잡되, 전 프레임 동일 높이를 유지한다. pivot 규칙은 다른 호스트와 동일하게 `bottom-center`를 쓴다 `[근거: pivot을 호스트마다 다르게 두면 빙의 시 위치 스왑에 예외 코드가 생긴다]`

### 5-3. 발사체 에셋 — **9종 신규 등재**

- 규격 **16×16** (표시 ×2 = 32px). `dragon`만 예외(§6 표).
- **전부 단일 프레임.** `[도출: 회전·명멸을 4프레임 스프라이트로 뽑으면 프레임 간 형상이 흔들려 회전이 부글거린다. Unity Transform 회전 + 머티리얼 알파 트윈이 픽셀 정합상 완벽하고 공짜다]`
  단, `dragon` 브레스는 회전으로 대체 불가한 형상 변화라 **3프레임**.
- 생성은 **8종을 1장(3×3 그리드, 1칸 공백)** 으로 묶는다 `[근거: 발사체는 서로 다른 캐릭터가 아니라 작은 아이콘 세트라서 UI 배치 규칙(Prompt_Registry §0 규칙 3)이 그대로 적용된다]`

---

## 6. 결정 ④ — 고스트 본체

| 항목 | 결정 | 근거 |
|------|------|------|
| 인게임 규격 | **48 × 48 — 호스트와 동일** | `[근거: ingame_concept 육안 — 필드 고스트가 잡몹과 같은 크기 등급. 동일 그리드를 쓰면 슬라이스·임포터 설정을 공유하고, 빙의 시 위치 스왑에 스케일 보정이 필요 없다]` |
| pivot | **`bottom-center` — 호스트와 통일.** 부유 높이는 프레임 하단 10px 투명 여백으로 표현 | `[도출: 고스트만 center pivot으로 두면 빙의 순간 좌표 오프셋 계산이 코드에 들어간다. 여백으로 흡수하면 스왑이 좌표 교체 한 줄로 끝난다]` |
| 로비 대형 고스트(`GhostAvatar`) 관계 | **완전한 별개 에셋. 같은 소스의 확대가 아니다** | `[근거: lobby_hub 육안 — 로비 고스트는 눈·입·팔·반짝임 디테일과 내부 광원이 인게임 고스트보다 훨씬 촘촘하다. 48×48을 ×4(192)로 확대하면 정수 배율은 지켜지지만 디테일이 없는 채 커지기만 해 검수 A(실루엣)·D(아케이드 톤)를 통과 못 한다]` |
| 중복 방지 | `GhostAvatar`(160×240)는 **`AVSR_AssetManifest.md` B3에 이미 등재됨.** 본 문서는 인게임 고스트만 다룬다 | — |
| 상태 | `Idle` **4** / `Move` **4** | `[도출: 다리가 없어 접지 보행 사이클이 필요 없다. 기울기 + 꼬리 나부낌 4프레임이면 충분하다 — 호스트 Move 6을 그대로 따라갈 이유가 없다]` |
| `Possess` | ⛔ 다음 마일스톤 | 사용자 확정 |

**모션**: `Idle` = 제자리 상하 부유(±3px) + 꼬리 흔들림. `Move` = 진행 방향으로 상체 기울임 + 꼬리가 뒤로 길게 나부낌.
**색**: 목업 실측 청백색 발광체. 발광은 **후처리가 아니라 스프라이트에 그려 넣는다**(확정 #16).

---

## 7. 결정 ⑤ — 생성 순서 (히어로 우선)

### 히어로 = **`amazoness`**

| # | 근거 |
|---|------|
| 1 | **`AVSR_AssetManifest.md` B13이 이미 `amazoness`를 초상 히어로로 지정했다.** 초상(192×192)과 인게임 스프라이트(48×48)의 **톤 마스터가 같은 캐릭터여야** 로비→인게임 사이에서 얼굴이 바뀌지 않는다. 이 근거가 가장 강하다 |
| 2 | 확정 #22 — **시작 보유 1종.** 100% 유저가 반드시 본다. 여기서 실패하면 게임 전체 인상이 실패한다 |
| 3 | **근접 3종 중 하나라 발사체 없이 캐릭터 시트 포맷만 단독 검증**할 수 있다. 원거리 히어로를 고르면 시트 실패와 발사체 실패가 섞여 원인 분리가 안 된다 |
| 4 | 골드+퍼플 팔레트라 §1의 **16색 상한 검증**에 적합 (단색 캐릭터로 검증하면 상한이 안 걸린다) |

### 순서

```
STEP 1  amazoness_idle 1장 생성 → §4-2 후처리 → Unity 슬라이스 → 재생 확인
        └ 여기서 시트 포맷(그리드 배치·48격자·발 접지선)이 성립하는지 판정한다. 실패하면 STEP 1 반복.
STEP 2  amazoness_move / amazoness_attack 생성 → 3상태 재생 확인 → 히어로 확정
STEP 3  확정된 amazoness 시트를 톤 레퍼런스로 붙여 나머지 11종 (근접 2종 → 원거리 9종 순)
STEP 4  발사체 8종 시트 1장 + dragon 브레스 1장
STEP 5  고스트 idle / move
```
`[근거: Prompt_Registry_Template.md §0 규칙 2 — 세트 일관성은 완성본끼리 잡는다. 히어로 1장을 먼저 확정하고 후속 생성의 톤 레퍼런스로 넣는다]`
근접 2종(`slugger`·`vampire`)을 원거리보다 먼저 하는 이유: `[도출: 히어로와 같은 근접 모션이라 톤 레퍼런스가 가장 잘 먹는다. 여기서 흔들리면 원거리는 더 흔들린다 — 조기 경보]`

---

## 8. 파일 명세표

> `width`/`height` = **시트 전체 크기**(프레임 수 × 48). `alpha` = 투명 배경 Y/N.
> `slice9` = 캐릭터는 전부 `—`(9-slice 대상 아님). `source` = `GPT`(신규 생성) / `CROP`(목업 크롭) / `CROP+GPT`.
> `element` = Unity AnimationClip 이름. 프리팹 슬롯은 전부 `Host{PascalKey}/Body`(SpriteRenderer) 단일.
> 1차 범위: `Idle` = ✅ 1차 / `Move`·`Attack` = ⛔ 다음 마일스톤이나 **사용자 지시로 지금 생성**.

| filename | element | width | height | alpha | slice9 | pivot | source | note |
|----------|---------|------:|-------:|:-----:|:------:|-------|--------|------|
| `amazoness_idle.png` | `HostAmazoness_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · **히어로** · 창 파지 정면 대기 |
| `amazoness_move.png` | `HostAmazoness_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `amazoness_attack.png` | `HostAmazoness_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 창 2연속 찌르기 · 발사체 없음 |
| `rambo_idle.png` | `HostRambo_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 |
| `rambo_move.png` | `HostRambo_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `rambo_attack.png` | `HostRambo_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 기관총 연사+반동 · 발사체 `proj_rambo` |
| `wizard_idle.png` | `HostWizard_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 앵커는 분홍. 녹색은 코스튬(범위 밖) |
| `wizard_move.png` | `HostWizard_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `wizard_attack.png` | `HostWizard_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 지팡이 오브 시전 · 발사체 `proj_wizard` |
| `ninja_idle.png` | `HostNinja_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 앵커는 청색. 적색은 코스튬(범위 밖) |
| `ninja_move.png` | `HostNinja_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `ninja_attack.png` | `HostNinja_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 표창 투척 · 발사체 `proj_ninja` |
| `mafia_idle.png` | `HostMafia_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 |
| `mafia_move.png` | `HostMafia_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `mafia_attack.png` | `HostMafia_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 톰슨건 부채꼴 훑기 · 발사체 `proj_mafia` |
| `hitman_idle.png` | `HostHitman_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 |
| `hitman_move.png` | `HostHitman_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `hitman_attack.png` | `HostHitman_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 조준 홀드 후 단발 · 발사체 `proj_hitman` |
| `yogamaster_idle.png` | `HostYogamaster_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · **부양 — 발 접지 없음**(§5-2 WARNING) |
| `yogamaster_move.png` | `HostYogamaster_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 보행이 아니라 부양 활공 |
| `yogamaster_attack.png` | `HostYogamaster_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 합장 구체 방출 · 발사체 `proj_yogamaster` |
| `dragon_idle.png` | `HostDragon_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 4족+날개. 유일한 비인간형 |
| `dragon_move.png` | `HostDragon_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 4족 보행 |
| `dragon_attack.png` | `HostDragon_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 화염 브레스 · 발사체 `proj_dragon`(3프레임) |
| `robot_idle.png` | `HostRobot_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 안광 명멸을 호흡 대신 사용 |
| `robot_move.png` | `HostRobot_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `robot_attack.png` | `HostRobot_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 팔 캐논 로켓 · 발사체 `proj_robot` |
| `snowwoman_idle.png` | `HostSnowwoman_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 |
| `snowwoman_move.png` | `HostSnowwoman_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `snowwoman_attack.png` | `HostSnowwoman_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 얼음 결정 사출 · 발사체 `proj_snowwoman` |
| `slugger_idle.png` | `HostSlugger_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 배트 어깨 거치 |
| `slugger_move.png` | `HostSlugger_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `slugger_attack.png` | `HostSlugger_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 배트 풀스윙 · 발사체 없음 |
| `vampire_idle.png` | `HostVampire_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 망토 나부낌을 호흡에 사용 |
| `vampire_move.png` | `HostVampire_Move` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ |
| `vampire_attack.png` | `HostVampire_Attack` | 288 | 48 | Y | — | bottom-center | GPT | frames:6 · ⛔ · 망토 전개+손톱 할퀴기 · 발사체 없음 |
| `ghost_idle.png` | `GhostPlayer_Idle` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ✅1차 · 하단 10px 투명(부유 높이) · `ghost/player` |
| `ghost_move.png` | `GhostPlayer_Move` | 192 | 48 | Y | — | bottom-center | GPT | frames:4 · ⛔ · 호스트 Move 6과 다름(§6) |
| `proj_rambo.png` | `ProjRambo` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 노란 예광탄 (가로 긴 스트릭) |
| `proj_wizard.png` | `ProjWizard` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 청색 마법 오브 |
| `proj_ninja.png` | `ProjNinja` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 4날 표창 · **Transform 회전** |
| `proj_mafia.png` | `ProjMafia` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 짧고 굵은 확산 탄 |
| `proj_hitman.png` | `ProjHitman` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 가늘고 긴 관통 탄 |
| `proj_yogamaster.png` | `ProjYogamaster` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 금색 에너지 구체 |
| `proj_robot.png` | `ProjRobot` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 로켓 (꼬리 불꽃 포함) |
| `proj_snowwoman.png` | `ProjSnowwoman` | 16 | 16 | Y | — | center | GPT | frames:1 · ⛔ · 육각 얼음 결정 |
| `proj_dragon.png` | `ProjDragon` | 144 | 48 | Y | — | center | GPT | frames:3 · ⛔ · **유일한 다프레임 발사체** — 지속 화염 브레스 |

**집계**: 호스트 12종 × 16프레임 = **192** · 고스트 **8** · 발사체 **11**(8×1 + 3) = **총 211프레임 / 47파일 / 생성 40회**
(호스트 시트 36 + 고스트 2 + 발사체 시트 1 + 브레스 1)

### 8-1. `source` 판정 근거 — 전부 `GPT`인 이유 (⚠️ task §5의 긴장 처리)

**`CROP`·`CROP+GPT`가 하나도 없다.** 근거:
- 앵커는 **91×84**다. `91 ÷ 48`도 `84 ÷ 48`도 정수가 아니다. 확정 #16(정수 배율)을 만족하며 48×48로 줄일 방법이 **없다.**
- 앵커는 **정지 초상 1포즈**다. Move·Attack 프레임이 애초에 존재하지 않는다.
- 앵커 하단에 한글 라벨(`설녀`·`슬러거` 등)이 찍혀 있다 — 크롭 자체가 오염돼 있다.

**따라서 48×48은 전부 새로 그린다. 그러나 원작 IP 캐릭터이므로 얼굴·복장이 달라지면 안 된다.**
이 긴장은 **"실루엣·의상·색은 앵커에 고정, 픽셀 밀도와 포즈는 새로"** 로 푼다. 검수 시 아래를 대조한다.

| 앵커에서 **반드시 보존** | 새로 **결정해도 되는 것** |
|--------------------------|---------------------------|
| 머리 장식·모자·헬멧 실루엣 (아마조네스 골드 티아라, 램보 적색 머리띠, 위저드 분홍 첨탑모자, 마피아 페도라, 슬러거 야구모자, 요가마스터 흰 터번, 스노우우먼 얼음 왕관) | 프레임별 팔·다리 각도 |
| 주 색상 3색 (예: 아마조네스 = 골드/퍼플/살색) | 표정 디테일 (48px에서 어차피 눈 2점) |
| 무기 종류·형태 (창·기관총·지팡이·표창·톰슨건·소음기권총·배트·망토) | 무기 길이 (48px 프레임에 맞춰 단축 허용) |
| 실루엣 특징 (드래곤 날개, 뱀파이어 세운 깃, 로봇 팔 캐논) | 아웃라인 두께·음영 단계 |

**판정 방법**: 완성된 `_idle` ①프레임을 앵커와 **나란히 놓고 축소해 본다.** 축소 상태에서 같은 캐릭터로 읽히면 통과(검수 A 실루엣 + 3초 테스트).

### 8-2. Unity 임포트 설정

| 항목 | 값 |
|------|-----|
| Sprite Mode | **Multiple** — Grid By Cell Size **48 × 48**, Padding 0, Offset 0 |
| Pivot | **Bottom** (발사체만 Center) |
| Filter Mode | **Point (no filter)** · Compression **None** · Mip Maps **끔** |
| Pixels Per Unit | **48** `[도출: 1스프라이트 = 1유닛. 타일·이동속도 계산이 정수로 떨어진다]` |
| 저장 경로 | `Assets/BundleResource/Character/{hostKey}/` — Addressable `host/{hostKey}` (constants.md §4 · 라벨 `label_host`) |
| 고스트 | `Assets/BundleResource/Character/ghost/` — 주소 `ghost/player` · 라벨 `label_ghost` |

---

## 9. ChatGPT 작업 지시문 템플릿 (호스트 1종씩)

> `{hostKey}` `{역할}` `{Attack모션}` `{상태}` `{프레임수}` `{그리드}` `{생성크기}` 를 치환해 사용한다.
> **첨부**: `Projects/AVSR/Reference/Hosts/{hostKey}.png` 1장 (+ STEP 3부터는 확정된 `amazoness_idle.png`를 톤 레퍼런스로 추가)

```
[첨부 1] Reference/Hosts/{hostKey}.png  — 이 캐릭터가 "누구인지"를 알려주는 앵커
[첨부 2] amazoness_idle.png            — 톤 레퍼런스 (STEP 3 이후에만)

첨부 1은 이 캐릭터의 정체성(머리 장식, 의상, 주 색상, 무기)만 참조하십시오.
첨부 1의 해상도·픽셀 밀도·포즈는 따라가지 마십시오. 새로 그립니다.

Draw a {그리드} grid of animation frames for a single game character, output size {생성크기}.

CHARACTER: {hostKey} — a 2-head-tall chibi (SD) pixel art character, {역할}.
Keep the head ornament, outfit, main colors and weapon identical to the attached anchor.

STYLE: 1991 arcade-era HD pixel art, dark occult action tone in the style of early-90s
Jaleco arcade games. Crisp hard-edged pixels on a visible uniform pixel grid — every pixel
block exactly the same size. 1px dark outline around the whole character.
Restrained palette: at most 16 colors plus the outline. No anti-aliasing, no soft gradients,
no blur, no modern UI gloss, no neon purple, no magenta.
Glow is painted into the sprite itself, not post-processed.
No text, no letters, no numbers anywhere in the image.

LAYOUT: {그리드} cells, evenly spaced with clear gaps, each cell on the same plain flat
background color that appears nowhere on the character. Reading order is left to right,
then top to bottom. The character is the SAME character in every cell — same face, same
proportions, same colors, same size. Feet touch the SAME baseline height in every cell.
Always facing the camera (front view), even when attacking sideways.

FRAMES ({상태}, {프레임수} frames in reading order):
{프레임 역할을 칸별로 한 문장씩 — §2 표 + §5 모션에서 옮겨 적는다}

DO NOT draw: ground shadow, target ring, dust, motion lines outside the character,
UI elements, or the flying projectile. Only the muzzle flash / weapon tip effect
that touches the character stays in frame.
```

**상태별 `FRAMES` 절 작성 예 (`amazoness_attack`)**

```
FRAMES (attack, 6 frames in reading order):
1. Neutral stance, spear starting to pull back behind the body.
2. Spear fully pulled back, body coiled, weight on the back foot.
3. Body uncoiling forward, spear starting to travel.
4. Spear thrust fully extended to the right, a short white slash streak at the tip.
5. Spear beginning to retract, body still leaning forward.
6. Returning to neutral stance.
```

**납품 지시 (모든 프롬프트 끝에 붙임)**

```
납품:
- 각 상태 시트를 개별 PNG로 저장하고, 전체를 ZIP 1개로 묶어 주십시오.
- 파일명: {hostKey}_idle.png / {hostKey}_move.png / {hostKey}_attack.png
- 이미지를 축소하지 마십시오. 생성 원본 크기 그대로 주십시오. (다운스케일은 이쪽에서 합니다)
```

**후처리 (사용자 → 작업자, §4-2)**

```
① 배경 제거 (평면 단색 배경 → 알파)
② 셀 크롭 — 발 접지선 기준 정렬. 중앙 정렬 금지
③ 각 셀 480×480으로 크롭·투명 패딩 (480 = 48 × 10)
④ ÷10 Nearest 다운스케일 → 48×48   ⚠️ Bilinear/Lanczos 절대 금지
⑤ 읽기 순서대로 가로 합성 → {프레임수}×48 시트
⑥ 색 수 양자화 — 17색(16 + 아웃라인) 상한 적용
⑦ Unity 임포트 (§8-2)
```

---

## 10. 미결 · 경고

| # | 항목 | 상태 |
|---|------|------|
| 1 | `[WARNING]` **앵커 첨부 vs `Prompt_Registry` §0 규칙 1 충돌** | 규칙 1은 "다른 이미지를 레퍼런스로 넣지 말라"(저해상도 앵커링 방지)이고, 사용자 지시는 "앵커 첨부"다. **해소 절차**: STEP 1에서 `amazoness_idle`을 ⓐ앵커 첨부 ⓑ앵커 없이 말로만 두 번 뽑아 비교한다. ⓑ가 명백히 낫다면 **전 호스트를 앵커 없는 text2img로 전환**하고 앵커는 검수 대조용으로만 쓴다 |
| 2 | `[WARNING]` **표시 크기 목업 대비 미달 가능성** | `ingame_hd_scene` 실측 환산(720폭 기준) — 잡몹·호스트 **약 100~125px**, 플레이어 램보 **약 169px**. 우리 스펙은 96px(몸통이 44/48을 채우면 88px). **소스 규격은 확정이므로 건드리지 않는다.** 부족하면 배율(×2)이 아니라 **카메라 오쏘 사이즈**로 보정한다. Stage 3 씬 배치 시 실기 확인 필요 |
| 3 | `[TBD — 이유: 인게임 전투가 1차 범위 밖]` 발사체 속도·수명·판정 크기 | Stage 2 인게임 전투 문서 |
| 4 | `[TBD — 이유: 다음 마일스톤]` Hit · Death · Possessed · Ultimate 프레임 | 상태 4종 추가 시 호스트당 +N프레임. 본 문서 §2 표를 확장한다 |
| 5 | `[TBD — 이유: 아트팀 소관]` `robot` 좌우 flip 시 팔 캐논 위치 (§3 WARNING) | 히어로 검증 후 판정 |
| 6 | `[TBD — 이유: 확정 #25]` 원작 스프라이트 IP 참조 범위 | 사용자 확인 중. 확정 전까지 **앵커(목업) 기준** |
| 7 | 코스튬 변형(`rambo_laser`·`wizard_green`·`ninja_red`) | **1차 범위 밖** [확정 #10]. 앵커는 `Reference/Hosts/`에 확보됨 |
| 8 | 프롬프트 영속 기록 | 생성 착수 시 `AVSR_PromptRegistry.md`에 호스트별 subject·경로를 한 줄씩 기록 |

---

## 개정 이력
| 버전 | 날짜 | 변경 |
|------|------|------|
| 0.1 | 2026-08-07 | 최초 작성 — 프레임 수(4/6/6) · 정면+flip · 호스트별 Attack 모션 12종 차별화 · 발사체 9종 신규 등재 · 고스트 규격·pivot 통일 · 그리드→스트립 생성 방식과 한계 5종 · 파일 47행 · 히어로 `amazoness` |
