# wa-sound 에이전트 팀

> **Team**: wa-sound
> **진입점**: `wa-manager-sound-lead`

---

## 개요

게임 사운드 방향·스펙을 기획하는 AI 에이전트 팀입니다.
사운드팀은 **방향 기획 문서(`SND_Design.md`) 산출**에 집중하며, 실제 사운드 파일 제작이나 Unity 구현은 담당하지 않습니다.

Unity AudioMixer·트리거 코드 구현은 `wa-manager-sound-lead` → `wa-manager-client-lead` → 클라이언트 팀원 흐름으로 처리합니다.

---

## 에이전트 목록

| 에이전트 | 직군 | 담당 섹션 |
|---------|------|---------|
| **wa-manager-sound-lead** | 팀장/사운드 디렉터 | 방향 수립, 검수, 클라이언트 팀 스펙 전달 |
| **wa-sound-team-bgm** | 배경음악 기획자 | `SND_Design.md` BGM 섹션 (SND-BGM-001) |
| **wa-sound-team-sfx** | 효과음 기획자 | `SND_Design.md` SFX 섹션 (SND-SFX-001) |
| **wa-sound-team-voice** | 보이스 오버 기획자 | `SND_Design.md` Voice 섹션 (SND-VO-001) |

---

## 실행 방법

```
wa-manager-sound-lead를 호출하고 게임 설명 또는 기획 문서를 전달합니다.
```

팀장이 게임 컨셉을 파악 후 bgm/sfx/voice 팀원에게 병렬 위임합니다.

---

## 에이전트 실행 흐름

```
게임 설명 / Concept 문서
    ↓
wa-manager-sound-lead (방향 수립)
    ├── wa-sound-team-bgm  → SND-BGM-001 섹션
    ├── wa-sound-team-sfx  → SND-SFX-001 섹션
    └── wa-sound-team-voice → SND-VO-001 섹션
    ↓
wa-manager-sound-lead (통합 검수·승인)
    ↓
wa-manager-client-lead (구현 스펙 전달, 필요 시)
```

---

## 산출물

```
Projects/[ProjCode]/
└── [ProjCode]_SND_Design.md
    ├── SND-STY-001  (사운드 스타일 총괄 — 팀장 작성)
    ├── SND-BGM-001  (배경음악 방향 — bgm 담당)
    ├── SND-SFX-001  (효과음 방향 — sfx 담당)
    └── SND-VO-001   (보이스 오버 방향 — voice 담당)
```
