---
description: C# 네이밍, 접근 제한자, 선언 순서, MonoBehaviour 패턴, Unity null 체크, UniTask, var 사용, 주석, 코드 포맷
alwaysApply: true
---

# C# 코드 작성 규약

스크립트 작성 시 항상 참조하는 C# 네이밍·스타일·Unity·UniTask 규칙이다.
프레임워크 사용 패턴(ServiceLocator, EventBus 등)은 `project/07_framework_rules.md` 참조.

---

## 1. 네이밍 컨벤션

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스·구조체·열거형 | PascalCase | `CharacterManager` |
| 인터페이스 | `I` + PascalCase | `ICharacterManager` |
| 메서드 | PascalCase | `LoadCharacterAsync` |
| 프로퍼티 | PascalCase | `IsInitialized` |
| private 필드 | `_` + camelCase | `_characterTable` |
| static 필드 | `s_` + camelCase | `s_instance` |
| 파라미터·지역 변수 | camelCase | `characterId` |
| 상수 (`const`) | PascalCase | `MaxRetryCount` |
| bool 멤버 | `Is` / `Has` / `Can` / `Should` 접두사 | `IsLoaded`, `HasToken` |
| 비동기 메서드 | `Async` 접미사 | `LoadAsync`, `OpenAsync` |

- 약어·약칭 금지 (`plyrHlth` ✗ → `playerHealth` ✓)
- 클래스 내 중복 문맥 제거: `Player` 클래스 안에서 `Target` ✓, `PlayerTarget` ✗

### IEvent struct 규칙

**선언**: `public struct`로 선언하고 필드에 `readonly`를 붙이지 않는다.
`readonly struct`는 모든 필드가 `readonly`가 되어 객체 초기화 구문 `{ Field = value }` 사용 시 CS0191 오류가 발생한다.

**필드명**: 발행자 클래스의 public 프로퍼티명과 동일하게 짓지 않는다.
동일하면 발행 시 `{ Score = Score }` 형태가 되어 좌변(struct 필드)과 우변(프로퍼티)을 구분하기 어렵다.
이벤트의 문맥(변화 방향·결과)을 반영한 이름을 사용한다.

```csharp
// ❌ 금지 — CS0191 오류 + { Score = Score } 가독성 문제 동시 발생
public readonly struct ScoreChangedEvent : IEvent { public readonly int Score; }

// ✅ 올바른 패턴 — 프레임워크 내장 이벤트(OnSceneLoaded 등)와 동일
public struct ScoreChangedEvent : IEvent { public int NewScore; }
// 발행: new ScoreChangedEvent { NewScore = _score }  — 명확
```

---

## 2. 접근 제한자

- 기본값 `private` — 항상 명시적으로 선언
- Inspector 연결 필드: `[SerializeField] private`
- `public` 필드 선언 금지 (순수 C# struct의 읽기 전용 데이터는 예외)
- 외부 노출은 인터페이스 또는 읽기 전용 프로퍼티로만

```csharp
// ❌ 금지
public float speed = 5f;

// ✅ 올바른 패턴
[SerializeField] private float _speed = 5f;
public float Speed => _speed;
```

---

## 3. 클래스 내 선언 순서

```
1. [SerializeField] private 필드
2. private 필드
3. 프로퍼티
4. Unity 라이프사이클 메서드
   Awake → OnEnable → Start → Update → LateUpdate → OnDisable → OnDestroy
5. public 메서드
6. private 메서드
```

---

## 4. Unity MonoBehaviour 패턴

### GetComponent 캐싱

`GetComponent<T>()` · `FindObjectOfType<T>()`는 반드시 `Awake`에서 캐싱한다.
Update 등 매 프레임 메서드에서 직접 호출 금지.

```csharp
private Rigidbody _rigidbody;

private void Awake() {
    _rigidbody = GetComponent<Rigidbody>();
}
```

존재 여부가 불확실한 경우 `TryGetComponent<T>()` 사용.

### 기타 규칙

- MonoBehaviour에서 생성자 사용 금지 → `Awake` 사용
- `Update` 등 매 프레임 메서드에서 `new` 키워드로 객체 생성 금지 (GC 압박) — 상세: 10절 hot path GC 회피
- 이벤트·구독 등록: `OnEnable`, 해제: `OnDisable`

---

## 5. Unity Object null 체크

Unity Object(`GameObject`, `MonoBehaviour` 등)는 `Destroy()` 후에도 C# 참조가 남아 "Fake Null" 상태가 된다.
Unity의 `==` 오버로드만 이를 올바르게 감지한다.

```csharp
// ✅ Unity Object — == 연산자 사용
if (target == null) { }
if (target != null) { }

// ❌ 금지 — Unity 오버로드를 우회하므로 Fake Null 감지 불가
if (target is null) { }
target?.DoSomething();
target ?? fallback;
```

> 순수 C# 클래스(Unity Object 미상속)에는 `?.` · `??` 사용 허용.

---

## 6. UniTask 비동기 패턴

`Task` · `Coroutine` 사용 금지. 반드시 `UniTask` 사용 (`07_framework_rules.md` 참조).

### 메서드명 및 취소

```csharp
// Async 접미사 필수
public async UniTask LoadAsync(CancellationToken cancellationToken = default) { }

// GameObject 생명주기 연동
await LoadAsync(this.GetCancellationTokenOnDestroy());
```

취소 지원이 필요한 메서드는 마지막 파라미터로 `CancellationToken` 추가.

### Fire-and-Forget

결과가 필요 없는 비동기 호출은 아래 두 방식 중 하나를 사용한다.

```csharp
// 기존 async UniTask 메서드를 fire-and-forget으로 호출할 때
DoAsync().Forget(); // fire-and-forget: [이유]

// 처음부터 반환값 없는 비동기 메서드로 선언할 때
async UniTaskVoid DoAsync() { ... }
```

`.Forget()` 사용 시 호출부에 `// fire-and-forget: [이유]` 주석 필수.

---

## 7. var 사용 기준

```csharp
// ✅ 타입이 명확한 경우 var 권장
var list = new List<string>();
var manager = CoreModule.Get<ICharacterManager>();

// ❌ 타입이 불명확한 경우 명시 선언
string input = Console.ReadLine();

// 원시 타입 리터럴은 명시 타입 사용
int count = 0;
float speed = 5f;
bool isValid = true;
```

---

## 8. 주석 작성

- 주석은 **한국어**로 작성
- "무엇"이 아닌 "왜"를 설명 (코드 자체가 무엇인지 말한다)
- XML doc (`///`): public 인터페이스 메서드에만 작성
- 자명한 코드에 주석 금지
- 섹션 구분: `// ─────────────────────────────────────────` 구분선 사용

```csharp
/// <summary>
/// 지정한 캐릭터를 로드하고 씬에 배치한다.
/// </summary>
public async UniTask<Character> LoadCharacterAsync(string characterId) { }

// Awake 순서가 보장되지 않으므로 Start에서 초기화
private void Start() { }
```

---

## 9. 코드 포맷

- 중괄호: **K&R 스타일** (여는 중괄호를 줄 끝에)
- 한 줄 최대 **120자**
- 들여쓰기: **4칸 공백** (탭 금지)
- 빈 줄: 메서드 간 1줄, 연속 2줄 공백 금지

```csharp
public sealed class CharacterManager : ICharacterManager {
    private CharacterTable _table;

    public async UniTask<Character> LoadAsync(string id, CancellationToken ct = default) {
        // 구현
    }
}
```

---

## 10. Hot Path GC 회피

매 프레임 또는 고빈도로 호출되는 경로(hot path)에서는 힙 할당을 만들지 않는다.
반복 할당은 GC 압박을 유발해 프레임 드롭(GC spike)과 Total Allocated Memory 지속 증가의 원인이 된다.

### 적용 범위 (hot path)

`Update()` · `LateUpdate()` · `FixedUpdate()` · `Tick()`, 이벤트 발행(`Publish`),
리액티브 변수 알림(`Notify`), 오디오 콜백 등 **매 프레임 또는 이벤트마다 호출되는 모든 메서드**.

### 금지 패턴

hot path에서 `ToArray()` · `new List<>()` · LINQ(`Where`/`Select`/`ToList` 등) 사용 금지.
모두 호출마다 새 배열·리스트를 할당한다.

```csharp
// ❌ hot path 금지 패턴
var snapshot = list.ToArray();           // 매 호출마다 새 배열 할당
var copy     = new List<Action>(list);   // 매 호출마다 새 리스트 할당
var result   = list.Where(...).ToList(); // LINQ — 내부적으로 다수 할당
```

### 올바른 패턴 — 재사용 버퍼 필드

콜백 목록을 복사해야 할 때(발화 중 구독/해제 대비 스냅샷 등)는 **재사용 가능한 버퍼 필드**를 사용한다.
`Clear()` → `AddRange()` → 순회 → `Clear()` 패턴은 capacity 확장 이후 힙 할당이 발생하지 않는다.

```csharp
// 필드 선언
private readonly List<Action> _buffer = new();

// 사용
_buffer.Clear();
_buffer.AddRange(list);
for (int i = 0; i < _buffer.Count; i++)
    _buffer[i]?.Invoke();
_buffer.Clear();
// → capacity 확장 이후 힙 할당 없음
```

> `foreach` 대신 `for` 인덱스 순회를 사용하면 `List<T>` 열거자 박싱 가능성도 함께 제거된다.

### 감지 방법

- Unity Profiler → CPU Usage에서 `GC.Alloc` 항목이 매 프레임 발생하는지 확인.
- Statistics 창의 Total Allocated Memory가 지속 증가하는 패턴.

> 실제 발생 사례·수정 이력: [`coding_summary_rule.md`](coding_summary_rule.md) 3절.
