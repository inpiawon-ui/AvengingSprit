# 04. CLIENT (클라이언트)

> **Part Code**: CL
> **Version**: 1.3.0
> **Last Updated**: 2026-04-17
> **Document Owner**: Lead Client Programmer
> **의존성**: COM-OVR-001, COM-CVT-001, GD-SYS-001, ART-STY-001
> **기반 문서 (A)**: `Unity_GameDev_Template.md` — 4. CLIENT 섹션을 상세화한 파트 문서(B)

> 📌 **핵심 원칙**: 성능 예산 준수, 플랫폼 최적화. 성능 예산 초과 기능은 디렉터 승인 후에만 허용.

---

## 📑 목차

- [CL-ARC-001: 아키텍처](#cl-arc-001-아키텍처)
- [CL-CON-001: 코딩 컨벤션](#cl-con-001-코딩-컨벤션)
- [CL-RND-001: 렌더링 파이프라인](#cl-rnd-001-렌더링-파이프라인)
- [CL-INP-001: 입력 시스템](#cl-inp-001-입력-시스템)
- [CL-OPT-001: 최적화](#cl-opt-001-최적화)
- [CL-SAV-001: 저장 시스템](#cl-sav-001-저장-시스템)
- [CL-LOC-001: 로컬라이제이션](#cl-loc-001-로컬라이제이션)
- [CL-BLD-001: 빌드 파이프라인](#cl-bld-001-빌드-파이프라인)
- [CL-QA-001: 클라이언트 QA 체크리스트](#cl-qa-001-클라이언트-qa-체크리스트)

---

## CL-ARC-001: 아키텍처

### 전체 레이어 구조

```
┌─────────────────────────────────────┐
│   Presentation Layer (UI / VFX)     │  ← 화면 표현, 사용자 입력 처리
├─────────────────────────────────────┤
│   Application Layer (Game Logic)    │  ← 게임 규칙, 상태 관리
├─────────────────────────────────────┤
│   Domain Layer (Model / Rules)      │  ← 순수 데이터 모델, 비즈니스 규칙
├─────────────────────────────────────┤
│   Infrastructure Layer              │  ← Network / Storage / Platform API
└─────────────────────────────────────┘
```

### 설계 패턴

| 패턴 | 적용 범위 | 라이브러리 예시 |
|------|----------|--------------|
| MVP / MVVM | UI와 로직 분리 | — |
| Event Bus | 시스템 간 느슨한 결합 | UniRx, MessagePipe |
| DI (Dependency Injection) | 의존성 주입 | VContainer, Zenject |
| State Machine | 캐릭터 / 게임 상태 관리 | — |
| Object Pool | 투사체, 이펙트 재사용 | 자체 구현 / PoolManager |
| Command Pattern | Undo 가능한 행동 처리 | — |

### 폴더 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/          (공통 유틸, 베이스 클래스, 이벤트 버스)
│   │   ├── Gameplay/      (전투, 캐릭터, AI, 스킬)
│   │   ├── UI/            (UI 로직, 뷰, 프레젠터)
│   │   ├── Network/       (서버 통신, 프로토콜 정의)
│   │   └── Data/          (ScriptableObject, 데이터 모델)
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── UI/
│   │   ├── VFX/
│   │   └── Gameplay/
│   ├── Scenes/
│   │   ├── _Boot.unity    (부트스트랩 씬)
│   │   ├── Lobby.unity
│   │   └── Battle.unity
│   ├── Art/               (아트 파트 납품 위치)
│   ├── Audio/
│   └── SO/                (ScriptableObject 인스턴스)
├── Plugins/               (서드파티 플러그인 — 수정 금지)
└── StreamingAssets/       (런타임 동적 로드 파일)
```

### 게임 매니저 구조

> 📌 싱글톤 남용 금지. 게임 매니저는 최대 6개 이내. 추가 필요 시 디렉터 승인.

| 매니저 클래스 | 역할 | 의존성 |
|-------------|------|--------|
| `GameManager` | 게임 전체 상태 관리, 씬 전환 | 없음 (최상위) |
| `DataManager` | 게임 데이터 로드·저장, 캐시 | 없음 |
| `NetworkManager` | 서버 통신, 세션 관리 | DataManager |
| `UIManager` | UI 스택 관리, 팝업·HUD 제어 | 없음 |
| `AudioManager` | BGM·SFX 재생, 오디오 풀 | 없음 |
| `PoolManager` | 오브젝트 풀 통합 관리 | 없음 |

### 이벤트 시스템

> 파트 간 의존성 최소화를 위해 이벤트 버스(Event Bus) 패턴 사용.

| 이벤트 ID | 이벤트명 | 발행 주체 | 구독 주체 | 전달 데이터 |
|----------|---------|---------|---------|-----------|
| EVT_001 | OnBattleStart | BattleManager | UIManager, AudioManager | BattleData |
| EVT_002 | OnPlayerDamaged | PlayerController | UIManager, CameraManager | float damage |
| EVT_003 | OnEnemyDead | EnemyController | BattleManager, UIManager | EnemyData |
| EVT_004 | OnLevelUp | CharacterManager | UIManager, AudioManager | int level |
| `EVT_[N]` | `[이벤트명]` | `[발행자]` | `[구독자]` | `[데이터 타입]` |

---

## CL-CON-001: 코딩 컨벤션

> → 참조: COM-CVT-001 (공통 네이밍 규약)

### 코드 작성 규칙

- 파일 1개 = 클래스 1개 원칙. 중첩 클래스는 200라인 이내인 경우에만 허용
- 메서드 최대 길이: **50라인 이하**. 초과 시 분리 리팩토링
- 공개 API (`public` / `protected`)는 **XML 주석 필수**
- Magic Number 금지: 상수(`const`) 또는 ScriptableObject로 추출
- `Update()` 내 `GetComponent`, `Find` 계열 호출 금지 → `Start()`에서 캐싱
- null 체크: `?.` (Null-conditional) 연산자 적극 활용
- `string.Format` 대신 `$""` (문자열 보간) 사용
- `Enum` 비교: `switch` 표현식 사용 권장

> [WARNING] Unity 메인 스레드 외에서 Unity API 호출 금지. 비동기 처리는 **UniTask** 또는 **Coroutine** 사용.

> [WARNING] `Update()` 내 LINQ, string 연산, 박싱/언박싱 절대 금지. 매 프레임 GC 발생으로 스터터링 원인.

### 코드 예시

```csharp
// ✅ 올바른 예시
public class PlayerController : MonoBehaviour, IDamageable
{
    private const float ATTACK_COOLDOWN = 0.5f; // Magic Number 금지

    [SerializeField] private PlayerData _data;   // ScriptableObject 참조
    private Animator _animator;                  // Start에서 캐싱
    private float _attackTimer;

    public float CurrentHp { get; private set; }
    public event Action OnDamaged;
    public event Action OnDeath;

    private void Start()
    {
        _animator = GetComponent<Animator>(); // Awake/Start에서만 호출
        CurrentHp = _data.maxHp;
    }

    /// <summary>데미지를 받습니다.</summary>
    /// <param name="amount">받을 데미지 양</param>
    public void TakeDamage(float amount)
    {
        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnDamaged?.Invoke();
        if (CurrentHp <= 0f) OnDeath?.Invoke();
    }
}
```

### Nullable 및 타입 안전성

- **`#nullable enable`** 프로젝트 전체 적용 (Unity 2021.2+)
- `null` 반환 대신 `Nullable<T>` (`T?`) 또는 커스텀 `Result<T, TError>` 래퍼 패턴 사용
- 예외가 정상 흐름인 경우 명시적 `throw`로 처리

### 비동기 패턴

| 상황 | 권장 방식 | 이유 |
|------|---------|------|
| 단순 시간 지연 | `IEnumerator` Coroutine | Unity 생명주기와 일치 |
| I/O, 네트워크 | `async/await` (UniTask 권장) | Thread 블로킹 방지 |
| 게임 상태 전환 | `IEnumerator` Coroutine | MonoBehaviour 연동 |

> `async void` 사용 금지 — 예외가 소멸됨. `async UniTaskVoid` 또는 반환값 있는 `async UniTask` 사용.

### GC Allocation 금지 패턴 (Update 루프 내)

```csharp
// ❌ GC 유발
void Update() {
    var list = new List<Enemy>();           // 매 프레임 할당
    var msg  = "Score: " + score;           // string 연산
    enemies.Where(e => e.IsAlive).ToList(); // LINQ
}

// ✅ 올바른 패턴
private readonly List<Enemy> _enemyBuffer = new();
private readonly StringBuilder _sb = new();

void Update() {
    _enemyBuffer.Clear();
    // 재사용
}
```

### MonoBehaviour 사용 원칙

- **비즈니스 로직을 MonoBehaviour에 작성 금지** — 순수 C# 클래스로 분리 후 MonoBehaviour에서 호출
- `Update()`에서 `GetComponent<T>()` 호출 금지 — `Awake()`/`Start()`에서 캐싱
- `FindObjectOfType<T>()` 런타임 호출 금지 — DI 또는 Service Locator 패턴 사용

### struct vs class 선택 기준

| 조건 | 선택 | 이유 |
|------|------|------|
| 데이터 크기 ≤ 16바이트, 불변(Immutable) | `struct` | 힙 할당 없음 → GC 압력 감소 |
| 단기 생명주기, 대량 생성·소멸 (투사체 정보, 히트 결과) | `struct` | GC 부담 감소 |
| 상속·다형성 필요, 공유 참조 필요 | `class` | 참조 타입 특성 활용 |
| MonoBehaviour 상속 또는 Unity 직렬화 필요 | `class` | Unity 제약 사항 |

```csharp
// 투사체 피격 결과 — struct 적합 (16바이트 이하, 단기 생명주기)
public readonly struct HitResult
{
    public readonly int Damage;
    public readonly Vector3 HitPoint;
    public readonly bool IsCritical;
}

// 캐릭터 상태 관리자 — class 적합 (상속, 공유 참조)
public class CharacterStateMachine { ... }
```

### sealed 클래스 사용 지침

확장이 불필요한 클래스는 `sealed` 선언 — JIT 컴파일러가 가상 함수 디스패치를 제거하여 성능 개선.
Update 루프에서 자주 호출되는 Component, State 클래스에 적용 권장.

```csharp
// ✅ 가상 함수 오버헤드 제거
public sealed class BulletMovement : MonoBehaviour { ... }
```

### Span\<T\> / Memory\<T\> 활용 (C# 8+, Unity 2021.2+)

대량 데이터 파싱(CSV, 바이너리 저장, 네트워크 패킷)에서 배열 복사 없이 슬라이싱:

```csharp
// ❌ 중간 배열 생성 → GC 유발
byte[] slice = buffer.Skip(4).Take(16).ToArray();

// ✅ 힙 할당 없음
ReadOnlySpan<byte> slice = buffer.AsSpan(4, 16);
int value = BitConverter.ToInt32(slice);
```

> `Span<T>`는 스택 전용이므로 async 메서드 내부 또는 클래스 필드로 저장 불가. 비동기 컨텍스트에서는 `Memory<T>` 사용.

---

## CL-RND-001: 렌더링 파이프라인

| 항목 | 선택 | 근거 |
|------|------|------|
| 렌더 파이프라인 | **URP** (Universal Render Pipeline) | 모바일 최적화, Unity 권장 |
| 컬러 스페이스 | **Linear** | PBR 정확도 |
| Shader 모델 | `[예: ES 3.0 이상]` | 타겟 디바이스 90% 커버 |
| HDR | PC: On / 모바일: Off | 성능 vs. 화질 균형 |
| Anti-Aliasing | PC: TAA / 모바일: FXAA | 성능 예산 기준 |
| Shadow Distance | 모바일 `[N]m` / PC `[N]m` | LOD 시스템 연동 |

---

## CL-INP-001: 입력 시스템

- **Unity Input System 패키지 사용** (Legacy Input Manager 미사용)
- **추상화 계층**: `IInputProvider` 인터페이스로 플랫폼 분기 처리
- **지원 입력**: Touch, Keyboard, Gamepad, Mouse
- **입력 처리 위치**: Application Layer (Gameplay 스크립트 직접 호출 금지)

```csharp
public interface IInputProvider
{
    Vector2 GetMoveInput();
    bool GetAttackInput();
    bool GetSkillInput(int skillIndex);
}
```

---

## CL-OPT-001: 최적화

### 성능 예산

> 📌 **DrawCall**: GPU에 렌더링을 지시하는 CPU 호출. Batching으로 최소화.
> 📌 **Object Pool**: 오브젝트를 생성/삭제 대신 풀에서 재사용하는 패턴.

| 항목 | 목표 | 한계 | 근거 |
|------|------|------|------|
| FPS | 60 | 30 이상 유지 | 액션 장르 표준 |
| DrawCall | ≤ 100 | ≤ 150 | 중저가 모바일 기준 |
| Tri Count (동시) | ≤ 500K | ≤ 800K | Adreno 640 기준 |
| 메모리 | ≤ 1.5GB | ≤ 2GB | iPhone 8 기준 |
| 로딩 시간 | ≤ 3초 | ≤ 5초 | 이탈률 증가 임계점 |
| 빌드 크기 | ≤ 150MB | ≤ 200MB | 모바일 다운로드 이탈률, App Annie 2024 |
| Update() 처리 시간 | ≤ 5ms (전체 합산) | ≤ 8ms | 60fps 기준 프레임 예산 |
| GC Alloc (매 프레임) | 0 KB | ≤ 1 KB | 스터터링 방지 |

> [WARNING] ART-CHR-001의 캐릭터 폴리곤이 증가할 경우 CL-OPT-001의 Tri Count 예산과 충돌 가능. 양 파트 리드 공동 리뷰 필수. → 참조: ART-CHR-001

### 최적화 기법 및 적용 기준

| 기법 | 적용 기준 | 비고 |
|------|----------|------|
| Occlusion Culling | 실내 맵 / 복잡한 씬 필수 | 씬 빌드 시 Bake |
| Static Batching | 움직이지 않는 환경 오브젝트 | Static 플래그 설정 필수 |
| Dynamic Batching | 소형 동적 오브젝트 (≤ 900 Tris) | 자동 적용 (설정 확인) |
| GPU Instancing | 동일 Mesh+Material 반복 오브젝트 (나무, 적 등) | 쉐이더 지원 확인 |
| Object Pool | 1초 내 10회 이상 생성/삭제 오브젝트 | 총알, 이펙트, 적, UI 카드 |
| Addressables | 씬 전환 시 교체 에셋, DLC 콘텐츠 | 메모리 스트리밍 |
| LOD | 모든 복잡한 3D 오브젝트 | ART-ENV-001 기준 준수 |
| Texture Compression | 플랫폼별 ASTC(iOS) / ETC2(Android) | Import Settings 확인 |

### Addressables / 에셋 로딩 전략

| 항목 | 결정사항 |
|------|---------|
| 번들 그루핑 기준 | `[씬 단위 / 카테고리 단위(캐릭터·맵·UI) / 혼합]` |
| 원격 번들 CDN | `[사용 — CDN 서비스 명시 / 미사용 — 로컬 번들만]` |
| 초기 다운로드 대상 | `[필수 번들 목록]` (빌드 크기 ≤150MB 기준으로 분리) |
| 지연 로드 대상 | `[선택 언어팩, 고해상도 에셋, DLC]` |
| 메모리 해제 정책 | `[씬 전환 시 Addressables.ReleaseInstance() 호출 규칙]` |

```csharp
// 에셋 로드 패턴 (UniTask 기반)
var handle = Addressables.LoadAssetAsync<GameObject>("Characters/Hero");
await handle.ToUniTask();

if (handle.Status != AsyncOperationStatus.Succeeded)
{
    Debug.LogError($"Addressables 로드 실패: {handle.OperationException}");
    Addressables.Release(handle);
    return;
}

_heroInstance = Instantiate(handle.Result);

// 씬 전환 시 반드시 해제
Addressables.ReleaseInstance(_heroInstance);
```

> **[WARNING]** Addressables 그루핑이 잘못되면 빌드 크기가 예상보다 커집니다. `Build & Analyze` 도구로 중복 번들 포함 에셋을 정기 점검하세요.

---

## CL-SAV-001: 저장 시스템

### 저장 방식 선택

| 방식 | 적합 조건 | 비고 |
|------|----------|------|
| `PlayerPrefs` | 경량 설정값 (볼륨, 언어 등) | 보안 취약 — 중요 데이터 저장 금지 |
| 파일 직렬화 (JSON/Binary) | 로컬 게임 저장 데이터 | `Application.persistentDataPath` 사용 |
| 클라우드 저장 (서버 API) | 멀티 디바이스 동기화 필요 시 | 오프라인 전용 게임은 해당 없음 |

- **선택**: `[PlayerPrefs / 파일 직렬화 / 클라우드 / 혼합]`
- **암호화 여부**: `[필요 — AES-256 / 불필요]` [근거: 오프라인 치트 방지 요구 수준]

### 저장 구조 원칙

- **저장 데이터와 설정 데이터 분리** (별도 클래스/파일)
- **버전 필드 필수 포함**: 스키마 변경 시 마이그레이션 대응
- **저장 실패 시 폴백 처리** 필수 (기존 파일 보존 후 덮어쓰기)

> **[WARNING]** 클라우드 저장 사용 시 SV-DB-001과 저장 스키마 동기화 필수. → 참조: 05_Server.md SV-DB-001

---

## CL-LOC-001: 로컬라이제이션

### 방식 선택

| 방식 | 적합 조건 | 비고 |
|------|----------|------|
| Unity Localization Package | 중소형 프로젝트, 팀 내 Unity 도구 선호 | Asset Store 무료 |
| 자체 CSV/JSON | 번역사 외주 협업, 단순한 구조 | 빌드 도구와 연동 필요 |
| 써드파티 (I2 Localize 등) | 복잡한 복수형·RTL 처리 필요 시 | 유료 |

- **본 프로젝트 선택**: `[Unity Localization / CSV / 써드파티]`
- **지원 언어**: `[KO / EN / JA / ZH-S / ZH-T / ...]`

### 구현 체크리스트

- [ ] **문자열 하드코딩 금지**: 모든 UI 텍스트는 Localization Key 참조
- [ ] **텍스트 확장 여백 확보**: 한국어 대비 독어/러시아어 30~40% 길이 증가 대응 (UI 레이아웃 Flexible 처리)
- [ ] **RTL 지원 여부 결정**: 아랍어·히브리어 출시 시 TextMeshPro RTL 옵션 활성화
- [ ] **폰트 번들 전략**: 언어별 폰트를 Addressables로 지연 로드 (초기 빌드 크기 절감) — CL-OPT-001 Addressables 참조
- [ ] **날짜·숫자·통화 포맷**: `CultureInfo` 또는 커스텀 포맷터 사용 (`1,000` vs `1.000`)
- [ ] **이미지/아이콘 현지화**: 문화권별 금기 이미지 점검 (색상, 손동작, 종교 상징)
- [ ] **번역 키 네이밍**: `[section].[component].[key]` 형식 준수 (예: `ui.mainmenu.play_button`)

> **[WARNING]** 폰트 파일은 라이센스 확인 필수. 상업적 사용 허가된 폰트만 사용 (예: Google Fonts OFL).

---

## CL-BLD-001: 빌드 파이프라인

### 브랜치 전략

> → 참조: COM-CVT-001 (브랜치 네이밍)

| 브랜치 | 용도 | 병합 규칙 |
|--------|------|----------|
| `main` | 배포용 안정 버전 | `release`만 허용. 직접 커밋 금지 |
| `develop` | 개발 통합 브랜치 | `feature` PR 후 병합. 코드 리뷰 1인 이상 |
| `feature/[파트]-[티켓]-[설명]` | 개인 기능 개발 | `develop`으로 PR |
| `release/v[버전]` | QA 및 출시 준비 | bugfix만 허용. `main` + `develop` 병합 |
| `hotfix/[파트]-[티켓]-[설명]` | 긴급 버그 수정 | `main` + `develop` 동시 병합 |

### 빌드 변형 (Build Variant)

| 환경 | 용도 | 설정 |
|------|------|------|
| **Dev** | 개발 중 디버깅 | 로그 활성화, Cheats 메뉴 활성화 |
| **Staging** | QA 테스트 | 스테이징 서버 연결, 로그 일부 |
| **Production** | 스토어 배포 | 로그 비활성화, 최적화 최대 |

### 버전 관리

```
[Major].[Minor].[Patch].[Build]
예시: 1.2.3.456

Major: 대규모 변경 (호환성 깨짐)
Minor: 기능 추가
Patch: 버그 수정
Build: 자동 증가 (CI/CD)
```

### CI/CD 파이프라인

> 📌 **CI/CD**: Continuous Integration/Delivery. 코드 푸시 시 자동 빌드·테스트·배포 수행.

| 단계 | 트리거 | 수행 작업 | 목표 시간 |
|------|--------|----------|----------|
| 코드 검사 | PR 생성 시 | 정적 분석(Roslyn), 컨벤션 체크 | ~2분 |
| 자동 빌드 | `develop` 병합 시 | 플랫폼별 빌드 (Android / iOS / PC) | ~15분 |
| 자동 테스트 | 빌드 성공 후 | Unit Test, Integration Test | ~5분 |
| QA 배포 | 수동 요청 시 | TestFlight / Firebase 배포 | ~10분 |
| 프로덕션 빌드 | `main` 병합 시 | 스토어 제출 빌드 생성 | ~20분 |

---

## CL-QA-001: 클라이언트 QA 체크리스트

> 빌드 제출 전 담당 개발자가 자체 확인 후 QA 파트로 전달합니다.

| 분류 | 체크 항목 | 기준 | 확인 |
|------|----------|------|------|
| 성능 | 목표 FPS 달성 | 플랫폼별 기준 참조 | ☐ |
| 성능 | 메모리 누수 없음 | 10분 플레이 후 메모리 증가 ≤ 10MB | ☐ |
| 충돌 | 크래시 없음 (콜드 스타트) | 3회 연속 실행 모두 정상 진입 | ☐ |
| 빌드 | 전 플랫폼 빌드 성공 | Android / iOS / PC 경고 0개 | ☐ |
| 데이터 | 에셋 누락 없음 | 콘솔 Missing Reference 0개 | ☐ |
| 네트워크 | API 오류 처리 정상 | 오프라인 / 타임아웃 시 안내 UI 표시 | ☐ |
| UI | 해상도 대응 정상 | 1280×720 ~ 2560×1440 범위 확인 | ☐ |
| UI | Safe Area 대응 | 노치 기기(iPhone 14~) 확인 | ☐ |
| 로컬라이징 | 텍스트 오버플로 없음 | KO / EN / JP 최소 3개 언어 | ☐ |
| 보안 | 치트 방지 기본 적용 | 로컬 데이터 무결성 검증 | ☐ |

---

## 장르 확장 포인트 (CL)

> 클라이언트 문서에서 장르·특성에 따라 추가 또는 변경이 필요한 항목:

| 장르 / 특성 | CL 추가 고려 항목 |
|-----------|----------------|
| 2D 게임 | CL-ARC-001에 Physics2D(Rigidbody2D, Collider2D) 설정 정책, Tilemap 사용 규칙 추가. 2D 카메라(Cinemachine 2D) 설정 |
| 횡스크롤 자동 달리기 | 무한 스크롤 레벨 생성기(Procedural Level Generator) 설계, 장애물 오브젝트 풀 설계 추가 |
| 실시간 멀티플레이어 | CL-ARC-001에 클라이언트 예측(Client Prediction) + 서버 재조정(Reconciliation) 아키텍처, Lag Compensation 전략 추가 |
| 그리드/전략 | 그리드 매니저 클래스 설계, A* 패스파인딩 구현 방침(자체 구현 / AstarProject), 타워 배치 UI 설계 추가 |
| 오프라인 전용 | CL-BLD-001 서버 관련 빌드 변형 생략. 로컬 저장소 암호화(`PlayerPrefs` 대신 암호화 JSON/SQLite) 설계 추가 |
| 콘솔 | CL-INP-001에 컨트롤러 진동(Haptics) 설계, 트로피/업적 SDK 연동(`PlayStation.Trophies`, `GameCenter`), 저장 슬롯 UI 추가 |
| AR/VR | CL-RND-001에 XR Render Pipeline 설정, Hand Tracking / Controller 입력 추상화 추가. CL-OPT-001 FPS 목표 72/90fps로 상향 |

---

## E 문서 작성 가이드 (CLIENT)

> 이 파트의 E 문서(실제 프로젝트 클라이언트 문서)를 작성할 때 아래 기준을 따르세요.

### 필수 포함 섹션

| 섹션 ID | 섹션명 | 완성 기준 |
|--------|--------|---------|
| CL-ARC-001 | 아키텍처 | 레이어 다이어그램 확정, 사용 패턴·라이브러리 확정 |
| CL-CON-001 | 코딩 컨벤션 | 네이밍 규칙·폴더 구조 팀 합의 완료 |
| CL-RND-001 | 렌더링 파이프라인 | URP/HDRP/BRP 선택 근거 명시, 카메라 설정 확정 |
| CL-INP-001 | 입력 시스템 | Input Action 목록 확정, 플랫폼별 대응 명시 |
| CL-OPT-001 | 최적화 | 타겟 기기 FPS·메모리 예산 수치 확정 |
| CL-BLD-001 | 빌드 파이프라인 | 빌드 타겟·배포 채널 확정 |
| CL-QA-001 | 클라이언트 QA 체크리스트 | 전 항목 ☑ 처리 (출시 기준) |
| CL-SAV-001 | 저장 시스템 | 저장 방식 선택·암호화 여부 확정 |
| CL-LOC-001 | 로컬라이제이션 | 지원 언어·방식·폰트 라이센스 확정, 체크리스트 ☑ 완료 |

### 최소 완성 기준 (Definition of Done)

- [ ] 모든 `[대괄호]` 플레이스홀더 제거됨
- [ ] `[TBD]` 항목에 이유 명시 (해결 시점 포함)
- [ ] 크로스 파트 의존 항목 `[WARNING]` 태그 확인
- [ ] CL-OPT-001 성능 예산 — 타겟 기기 FPS·드로우콜·메모리 수치 모두 확정
- [ ] CL-QA-001 체크리스트 — 전 항목 ☑ 처리

---

## 개정 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|------|------|--------|----------|
| 1.0.0 | 2026-04-16 | [작성자] | 초기 템플릿 작성 |
| 1.1.0 | 2026-04-17 | [작성자] | 기반 문서 표시, 장르 확장 포인트 추가 |
| 1.2.0 | 2026-04-17 | [작성자] | E 문서 작성 가이드 (CLIENT) 섹션 추가 |
| 1.3.0 | 2026-04-18 | [작성자] | CL-CON-001 심화 코딩 기준 추가 (Nullable/GC/struct/sealed/Span), Addressables 전략, CL-SAV-001, CL-LOC-001 섹션 추가 |
| 1.x.x | [날짜] | [작성자] | [변경 내용] |

---

*← 이전: [03_Art.md](./03_Art.md) | → 다음: [05_Server.md](./05_Server.md)*
