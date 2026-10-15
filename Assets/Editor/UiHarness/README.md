# UI 검증 하네스 (UiHarness)

화면 상태마다 여러 해상도로 **실제 게임 뷰를 캡처**하고, 그 자리에서 **레이아웃·리소스를 자동 검사**한다.
게임을 모른다 — 게임마다 «프로필 클래스 하나»만 만들면 같은 도구로 돈다.
네임스페이스 `GameFramework.Game.Editor.UiHarness`, 어셈블리 `UiHarness.Editor`(키트).

## 구성

| 파일 | 역할 |
|---|---|
| `UiHarnessProfile.cs` | `IUiHarnessProfile`(게임이 구현) · `UiHarnessScenario`(상태 하나) |
| `UiHarnessRules.cs` | 판(Contain) · 예외(AllowRule) · 겹쳐 쌓는 화면(ExclusiveChildren) · 모달 창(Modal) 설정 |
| `UiLayoutChecker.cs` | 레이아웃 검사 C1~C6 · 리소스 검사 R1~R4 |
| `UiSpriteAlpha.cs` | 원본 PNG 알파 표본(9분할 좌표 변환 포함) — C6 가림 판정용 |
| `UiHarnessDriver.cs` | 이벤트시스템 레이캐스트로 «진짜로 눌러 본다»(다른 부품이 가리면 실패) |
| `UiHarnessRunner.cs` | 부트 씬 → 플레이 진입 → 상태별 해상도 전환·캡처·검사 → 결과 폴더 |

## 다른 게임에서 쓰는 법

### 1. 프로필 클래스 하나 (게임 에디터 어셈블리에)

파라미터 없는 생성자가 있어야 한다(플레이 진입 뒤 다시 만든다).
게임 에디터 어셈블리가 `UiHarness.Editor`(키트가 주는 asmdef)·`UniTask` 를 참조해야 한다.

```csharp
public sealed class MyGameUiHarnessProfile : IUiHarnessProfile
{
    [MenuItem("MyGame/Verify/UI Harness (Main)")]
    public static void RunMenu() => UiHarnessRunner.Run(new MyGameUiHarnessProfile());

    public string Name => "MyMain";                                  // 결과 폴더 이름
    public string BootScene => "Assets/Scenes/Boot.unity";
    public string MockFolder => null;                                // 시안 폴더(리포 기준) — 없으면 null
    public IReadOnlyList<(string ViewName, string Tag)> Resolutions { get; } =
        new[] { ("Phone 16x9", "169"), ("Tablet 4x3", "43") };       // 게임 뷰에 등록한 크기 이름 · 파일 꼬리표
    public UiHarnessRules Rules { get; } = new UiHarnessRules()
        .Contain(@"/RankingPanel$", 0f, "랭킹 판");
    public IReadOnlyList<UiHarnessScenario> Scenarios { get; } = new[]
    {
        new UiHarnessScenario { Name = "home", Enter = d => UniTask.CompletedTask, Expect = () => null },
        new UiHarnessScenario { Name = "shop", Enter = d => d.Click("Btn_Shop"), Expect = () => null, Leave = d => d.Click("Btn_Back") },
    };
    public Transform Root() => GameObject.Find("MainCanvas")?.transform;
    public async UniTask GotoStart(UiHarnessDriver d) { /* 씬 로드 후 첫 화면까지 대기 */ }
}
```

- `Enter`: 시작 화면에서 그 상태로 간다(`d.Click(이름)` · `d.PressDown(이름)`). 가려져 있으면 **실패**한다.
- `Expect`: 도달했는지 — 문제 없으면 `null`, 아니면 이유 문자열. 못 왔으면 찍지 않고 실패로 남긴다.
- `Settle`: 해상도를 바꾼 뒤 다시 세울 것(누름 유지 등). `Leave`: 시작 화면으로 돌아오기.
- 상태마다 새로 열고 싶으면 `GotoStart` 에서 씬을 다시 로드한다.

### 2. 게임 뷰 크기 등록

`Resolutions` 의 `ViewName` 은 **Game 뷰 크기 목록에 미리 등록된 이름**이어야 한다
(Game 뷰 드롭다운 → `+` → Fixed Resolution). 등록이 없으면 그 상태가 «게임 뷰 크기가 등록돼 있지 않다» 로 실패한다.
끝나면 원래 크기로 되돌린다.

### 3. 실행

위 프로필처럼 `MenuItem` 을 하나 달아 `UiHarnessRunner.Run(프로필)` 을 부른다.
스스로 플레이에 진입한다. 컴파일 직후나 이미 도는 중에 겹쳐 돌리지 않는다(`IsRunning` 이면 무시).

### 4. 결과 폴더

`Temp/UiHarness/<프로필이름>_<yyyyMMdd_HHmmss>/` (리포 밖 Temp — 커밋되지 않는다)

- `<상태>_<꼬리표>.png` — 캡처
- `report.json` — 기계가 읽는 결과 (`mockFolder` 포함)
- `report.md` — 사람이 읽는 요약. **맨 위의 «실패 N / 경고 M»** 이 합격 기준이다.

## 규칙표

| 규칙 | 내용 | 등급 |
|---|---|---|
| C1 담기 | 자식이 `Contain` 으로 지정한 판 밖으로 나감(C1? = 부모 판 밖, 경고) | 실패 |
| C2 마스크 | 스크롤·마스크 영역 밖으로 잘림 | 실패 |
| C3 화면 | 화면 밖으로 나감 | 실패 |
| C4 글자 넘침 | 글자가 제 칸을 넘침(자동 축소 최소 크기에서도 넘치면 실패, 축소로 겨우 맞으면 경고) | 실패/경고 |
| C5 글자 겹침 | 서로 다른 글자가 겹침(글자 하나하나 상자 기준. 같은 문자열도 검사하며, 같은 글자를 겹쳐 그린 테 장식은 프로필 `AllowRule("C5", 경로, 이유)` 로만 뺀다) | 실패 |
| C6 가림 | 누를 수 있는 부품이 다른 부품에 가려짐(원본 PNG 알파 표본) | 실패 |
| R1 비율 | 스프라이트 비율이 찌그러짐 | 실패 |
| R2 9분할 | 늘려 쓰는데 9분할(Sliced)이 아님 | 실패 |
| R3 확대 | 원본보다 크게 확대해 그림 | 경고 |
| R4 원본 과대 | 쓰이는 크기보다 원본이 지나치게 큼 | 경고 |

등급과 임계값의 정본은 `UiLayoutChecker.cs` 다 — 이 표가 어긋나면 코드가 맞다.

## 예외는 «경로 + 이유» 로 좁게

```csharp
.AllowRule("C2", @"/Screen_Map/MapPage_\d+$", "지도 그림 — 키워 깔고 창이 잘라 낸다")
```

- 규칙 id(`C1`·`R3`… 또는 `"*"` 는 전부) + **경로 정규식 + 이유**를 반드시 적는다. `"*"` 와 넓은 정규식은 최소화한다.
- **쓰이지 않은 예외는 리포트에 따로 뜬다** — 화면이 바뀌어 낡은 예외는 지운다.
- `ExclusiveChildren(부모정규식, 자식정규식)`: 겹쳐 쌓는 화면 묶음 — 이름이 맞는 «보이는» 자식 중 맨 위 하나만 검사한다.
- `Modal(경로정규식)`: 모달 창(화면을 덮는 어두운 판 + 창) 지정. 경로가 맞고 «보이는» 요소가 있으면, 그 요소 하위가 아닌 모든 요소는
  그 상태의 레이아웃 검사(C1~C6)에서 대상·가리개 양쪽으로 빠진다 — 모달 뒤 화면은 모달이 없는 다른 상태가 검사한다. 어두운 판과 창을
  함께 담는 **한 부모**에 걸어야 한다(판만 걸면 창이 «밖» 이 된다). 맞는 요소가 여럿이면 서로 자손인 것은 바깥 쪽만 남기고, 가장 위에
  그려지는(캔버스 정렬 → 위계 순서) 하나가 기준이다. 리소스 검사(R1~R4)는 가려진 화면에도 한다(그림 자체의 결함이라 가림과 무관).
  리포트에 «모달 규칙» 표로 적용 횟수가 남고, 0 이면 «안 열림» 으로 표시된다.
- `Contain(경로, inset, 이유)`: 이 판의 자손은 판 안에 있어야 한다. `inset` 은 판 높이 비율(금테 등 테 두께).

## 한계 (읽고 판단할 것)

- **실시간 시계는 고정하지 않는다** — 시간·애니메이션·파티클이 들어간 화면은 캡처마다 조금 다르다.
- **실제 저장 데이터를 쓴다** — 에디터에 남은 세이브·로케일 상태를 그대로 본다. 필요한 값은 `GotoStart`/`Enter` 에서 맞춘다.
- **TMP 글리프 상자는 근사**다 — C4·C5 는 글리프 상자를 안쪽으로 줄여 재므로 아주 작은 침범은 놓치거나 드물게 오판한다. 의심되면 캡처를 눈으로 본다.
- **C6 가림의 보장 범위**: 글자의 조상·자손에 있는 그림은 가림 후보에서 뺀다(글자 자손의 불투명 덮개는 못 잡는다) ·
  겹친 범위를 4×4 로 표본해 절반 이상 불투명할 때만 가림으로 본다(표본 사이의 얇은 선, 3px 미만 띠는 놓친다) ·
  마스크는 `RectMask2D` 만 읽는다(`Mask` 등은 읽지 않는다). «모든 가림·마스크를 검증했다» 고 보고하지 않는다.
- **`ExclusiveChildren` 전제**: 맨 위 화면이 **완전 불투명**이라고 가정하고 아래 화면은 검사하지 않는다. 위 화면이 반투명이거나
  일부만 덮으면 실제로 보이는 아래 글자가 검사에서 빠진다.
- **판정 줄**: `report.md` 맨 위의 «기계 검사 통과» 는 실패 0 · 도달 실패 0(해상도 전환 뒤 `Expect` 재확인 포함) · 실행 오류 없음 ·
  모든 상태×해상도 샷 존재일 때만 나온다. 그 이상(시안 일치·전체 UI 품질)은 보증하지 않는다.
- 합격은 «규칙 위반 0» 이지 «보기 좋음» 이 아니다 — 시안 대조는 비교판으로 사람이 본다.

## 파이썬 도구 (선택)

리포의 `Tools/uiharness/` 에 있다. 결과 폴더의 `report.json` 만 읽으므로 게임과 무관하다.

```
# 시안 | 해상도별 게임 비교판 (위반 상자: 빨강=실패, 주황=경고). Pillow 필요
uv run --no-project --with pillow python Tools/uiharness/contact_sheet.py <결과 폴더> [--height 360]
#   → <결과 폴더>/sheets/<상태>.png, sheets/_all.png

# 규칙·부품별 위반 요약(같은 문제가 상태·해상도마다 반복되므로 묶어 본다)
python Tools/uiharness/summary.py <결과 폴더>
```
