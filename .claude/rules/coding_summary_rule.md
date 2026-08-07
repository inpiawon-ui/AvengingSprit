---
description: 실제 작업 중 발생한 실수 패턴 요약 — 재발 방지를 위한 주의 규칙 모음
alwaysApply: true
---

# 코딩 실수 패턴 요약

실제 작업 중 발생한 실수와 그 원인을 기록한다.
새 작업 전 이 파일을 참조하여 동일한 실수를 반복하지 않는다.

---

## 1. PowerShell Set-Content 인코딩 — 한글 깨짐

**PowerShell로 텍스트 파일을 수정할 때는 반드시 `-Encoding utf8`을 지정하라.**

**발생 경위:**
`.cs` 파일 28개에 `using` 구문을 일괄 삽입할 때 `Set-Content`를 사용했다.
CLAUDE.md 수정 시 동일한 실수를 이미 경험하고 수정했음에도, `-Encoding utf8` 옵션을 누락해 재발했다.
13개 파일의 한글 주석이 `?` 문자로 손상됐다.

**원인:**
Windows PowerShell 5.1의 `Set-Content` 기본 인코딩은 UTF-16 LE다.
원본 파일이 UTF-8이면 덮어쓸 때 한글 멀티바이트 문자가 깨진다.

```powershell
# ❌ 기본값(UTF-16 LE) — 한글 깨짐
Set-Content -Path $file -Value $content

# ✅ UTF-8 명시 — 한글 보존
Set-Content -Encoding utf8 -Path $file -Value $content
```

**체크 포인트:**
- PowerShell로 파일을 수정한 직후 Read 툴로 한글이 포함된 줄을 직접 확인한다.
- 한글이 `?` 또는 알 수 없는 문자로 보이면 즉시 `git checkout -- <파일>`로 복구한다.

---

## 2. Path.IsPathRooted — REST 경로 차단 버그

**파일 경로 방어 코드에서 `Path.IsPathRooted` 체크를 `TrimStart` 이전에 수행하면 REST API 관례인 `/` 시작 경로를 모두 차단한다.**

**발생 경위:**
`JsonFileApiStore.BuildFilePath`에서 경로 순회 방지를 위해 `Path.IsPathRooted` 체크를 먼저 수행했는데,
Windows에서 `Path.IsPathRooted("/users/me")` → `true`를 반환해
정상 API 경로를 모두 `ArgumentException`으로 차단했다.
구현 후 런타임 테스트 없이 사용 예시를 제공한 것이 직접적인 원인이었다.

**원인:**
Windows의 `Path.IsPathRooted`는 `/`로 시작하는 경로를 "현재 드라이브 루트"로 해석해 `true`를 반환한다.
`TrimStart`로 선행 `/`를 제거하는 코드가 있었지만 체크 순서가 잘못돼 의미가 없었다.

```csharp
// ❌ 잘못된 순서 — /users/me를 차단
if (Path.IsPathRooted(path)) throw new ArgumentException(...);  // 체크 먼저
var sanitized = path.TrimStart('/', '\\');                       // 정규화 나중

// ✅ 올바른 순서 — /users/me 허용, C:\... 차단
var sanitized = path.TrimStart('/', '\\');                       // 정규화 먼저
if (Path.IsPathRooted(sanitized)) throw new ArgumentException(...); // 체크 나중
```

결과:
- `/users/me` → TrimStart → `users/me` → `IsPathRooted=false` → ✅ 허용
- `C:\malicious` → TrimStart 변화 없음 → `IsPathRooted=true` → ❌ 차단
- `../etc/passwd` → `Contains("..")=true` → ❌ 차단

**체크 포인트:**
- 경로 검증 로직 작성 시 `/`로 시작하는 경로로 반드시 테스트한다.
- REST 경로(`/users/me` 등)와 OS 절대 경로(`C:\...`) 방어를 함께 구현할 때 정규화(`TrimStart`) → 검증(`IsPathRooted`) 순서를 지킨다.
- 새 유틸리티 구현 후 대표 경로 3종(`/path`, `path`, `../path`)으로 직접 실행 확인 후 사용 예시를 제공한다.

---

## 3. Hot Path ToArray() — 반복 힙 할당 (GC spike)

**매 프레임·이벤트마다 호출되는 경로(hot path)에서 `ToArray()`·`new List()`·LINQ로 콜백 목록을 복사하지 마라. 재사용 버퍼 필드를 사용하라.**

**발생 경위:**
Unity Statistics 창의 Total Allocated Memory가 지속 증가하는 현상을 분석한 결과,
콜백 발화 경로에서 발화 중 구독/해제 대비 스냅샷을 만들 때 매번 `list.ToArray()`를 호출해
매 프레임(또는 이벤트마다) 힙 할당이 발생하고 있었다.

**원인:**
`ToArray()`·`new List<>()`·LINQ는 호출마다 새 배열·리스트를 할당한다.
hot path에서는 이 할당이 누적되어 GC 압박 → 프레임 드롭(GC spike)을 유발한다.

수정한 파일 3곳 (모두 동일 패턴 — 재사용 버퍼 필드로 교체):

| 파일 | 메서드 | 버퍼 필드 |
|------|--------|-----------|
| `Assets/GameFramework/Core/Module/EventBus/EventBus.cs` | `Publish` (이벤트 발행) | `_snapshot` |
| `Assets/GameFramework/Core/Common/RefVar.cs` | `Notify` (값 변경 알림) | `_notifyBuffer` |
| `Assets/GameFramework/Core/Module/Input/InputManager.cs` | `InvokeCallbacks` (매 프레임 Tick) | `_callbackBuffer` |

```csharp
// ❌ 수정 전 — 발화마다 새 배열 할당
var snapshot = list.ToArray();
foreach (var cb in snapshot) cb?.Invoke();

// ✅ 수정 후 — 재사용 버퍼 (capacity 확장 이후 할당 없음)
private readonly List<Action> _buffer = new();

_buffer.Clear();
_buffer.AddRange(list);
for (int i = 0; i < _buffer.Count; i++)
    _buffer[i]?.Invoke();
_buffer.Clear();
```

**체크 포인트:**
- `Update`/`LateUpdate`/`FixedUpdate`/`Tick`/`Publish`/`Notify`/오디오 콜백 작성 시 `ToArray()`·`new List()`·LINQ 사용 여부를 점검한다.
- Unity Profiler → CPU Usage에서 `GC.Alloc`이 매 프레임 발생하면 hot path 할당을 의심한다.
- `foreach` 대신 `for` 인덱스 순회를 사용해 `List<T>` 열거자 박싱도 함께 제거한다.

> 규칙 본문: [`coding_conventions.md`](coding_conventions.md) 10절 (Hot Path GC 회피).

---

## 4. SortedList foreach — 열거자 박싱 (GC spike)

**`SortedList<K,V>`에서 `foreach`를 사용하면 열거자가 IEnumerator<> 인터페이스로 박싱되어 매 호출마다 힙 할당이 발생한다. `Values[i]` 인덱스 접근으로 대체하라.**

**발생 경위:**
`EventBus.EventChannel<T>.Publish()`에서 핸들러 우선순위 목록을 순회할 때
`foreach (var (_, list) in _handlers)`를 사용했다.
3절(Hot Path ToArray()) 수정 세션에서 `ToArray()` → 재사용 버퍼로 올바르게 교체했지만
SortedList 열거자 박싱 문제는 그대로 남아 Total Allocated Memory가 계속 증가했다.

**원인:**
`List<T>`, `Dictionary<K,V>`는 `GetEnumerator()`가 struct를 직접 반환 → foreach에서 박싱 없음.
`SortedList<K,V>`는 `GetEnumerator()`가 `IEnumerator<KeyValuePair<K,V>>`(인터페이스)를 반환 → struct 열거자가 인터페이스로 박싱 → 매 호출 GC 할당.

수정한 파일:

| 파일 | 메서드 | 교체 방식 |
|------|--------|----------|
| `Assets/GameFramework/Core/Module/EventBus/EventBus.cs` | `EventChannel<T>.Publish` | `foreach(_handlers)` → `Values[i]` 인덱스 루프 |
| `Assets/GameFramework/Core/Module/EventBus/EventBus.cs` | `EventChannel<T>.Unsubscribe` | `foreach(_handlers.Values)` → `Values[i]` 인덱스 루프 |

```csharp
// 수정 전 — SortedList 열거자 박싱, 매 호출 GC 할당
foreach (var (_, list) in _handlers) { ... }
foreach (var list in _handlers.Values) { ... }

// 수정 후 — 인덱스 접근, 박싱 없음
var lists = _handlers.Values; // Values 자체는 내부적으로 캐시됨 (new 없음)
for (int i = 0; i < lists.Count; i++) { var list = lists[i]; ... }
```

**체크 포인트:**
- hot path에서 컬렉션을 `foreach`로 순회하기 전에 해당 컬렉션이 struct enumerator를 직접 노출하는지 확인한다.
  - struct enumerator (박싱 없음): `List<T>`, `Dictionary<K,V>`, `HashSet<T>`, `Queue<T>`, 배열
  - 인터페이스 반환 (박싱 발생): `SortedList<K,V>`, `LinkedList<T>`, `Stack<T>` 등
- `SortedList<K,V>` 순회는 반드시 `for (int i = 0; i < list.Values.Count; i++)` 패턴을 사용한다.
- `SortedList.Values`는 내부적으로 캐시된 `IList<V>` 객체를 반환하므로 루프 앞에 지역 변수로 한 번만 꺼내 쓴다.
