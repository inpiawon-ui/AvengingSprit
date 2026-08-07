# 프롬프트 레지스트리 — 템플릿 (스튜디오 표준)

> **역할**: 이미지 생성 에셋의 **프롬프트·스펙을 툴 무관하게 영속 저장**하는 스튜디오 표준 포맷.
> 프롬프트가 진짜 자산이다. 툴(ComfyUI·GPT 등)·스타일이 프로젝트마다 바뀌어도 이 레지스트리는 재사용된다.
> **새 프로젝트 시작 시**: 이 템플릿을 `Projects/[ProjCode]/[ProjCode]_PromptRegistry.md`로 복사 → 아래 "프로젝트 스타일 base"만 그 게임에 맞게 교체 → 에셋 생성할 때마다 한 줄씩 추가.

---

## 0. 2-티어 생성 워크플로우 (핵심 철학)

두 툴은 "베이스→폴리시" 관계가 **아니라** 등급이 다른 두 티어다.

| 티어 | 툴(예) | 역할 |
|------|--------|------|
| **draft** | ComfyUI (무료·무한 반복) | 프로토타입·플레이스홀더·**구도/느낌 탐색**. 품질 무관 |
| **final** | GPT(gpt-image-1) 등 (유료·고품질) | 실제 게임에 들어갈 **최종 품질 에셋** |

### 규칙
1. **draft로 구도를 확정** → 그 구도를 **말로 정리해 final 프롬프트에 반영**한다.
2. **final은 프롬프트에서 새로(text2img) 생성**한다. ⚠️ **draft 이미지를 img2img/edit 레퍼런스로 넣지 않는다** — 입력의 품질/구조에 앵커링되어 final 툴의 네이티브 퀄 상한이 draft에 묶인다.
3. **세트 일관성**(단계·아이콘 세트)은 **final끼리(GPT→GPT) 레퍼런스**로 잡는다. 완성된 final 히어로 1장을 후속 final 생성의 톤 레퍼런스로 넣는다. (품질을 끌어올리는 용도가 아니라 톤을 맞추는 용도라 동급끼리가 맞다)
4. **draft 이미지는 final 프롬프트를 다듬을 때 "눈으로 보는 구도 참고"로만** 쓴다.

---

## 1. 프롬프트는 툴마다 다르다 → subject 1벌 + base 2벌

같은 프롬프트를 두 툴에 그대로 넣으면 결과가 딴판이다.

| 툴 | 프롬프트 성격 |
|----|--------------|
| ComfyUI | **태그/부스+가중치+네거티브** 스타일 (booru-ish). `(masterpiece:1.2), tag, tag, ...` + Negative |
| GPT | **자연어 서술 한 문단**. 네거티브·가중치 개념 약함. "A cute ... , isolated on transparent background" |

**중복을 피하는 구조**: 각 에셋의 **subject(주제)는 한 번만** 적고, **스타일 base만 툴별로 분리**한다.
- `comfy_prompt = comfy_style_base + subject + comfy_suffix` / `+ comfy_negative_base`
- `gpt_prompt   = gpt_style_base + subject (자연어로)`

---

## 2. 프로젝트 스타일 base (← 새 프로젝트가 교체하는 유일한 부분)

```
[comfy_positive_base]  (그 게임의 스타일 태그)
[comfy_negative_base]  (공통 네거티브)
[gpt_style_base]       (그 게임의 스타일을 자연어 한 문장으로)
```

> 스타일 락 먼저: "도트냐 페인터리냐" 등 최종 스타일을 base에 못박는다.
> 참고 — GPT 계열은 **정통 픽셀 도트에 약하고**(매끈해짐), 페인터리·일러스트에 강하다. 도트가 정체성이면 final도 ComfyUI+픽셀모델이 나을 수 있다. 스타일에 맞춰 티어 툴을 고른다.

---

## 3. 에셋 레지스트리 표 (에셋마다 한 줄)

| 필드 | 의미 |
|------|------|
| `id` | 에셋 ID = 파일명(확장자 제외) = (가능하면) UI 요소 이름 |
| `subject` | 주제 서술 (툴 공통 — comfy·gpt가 공유) |
| `size` | 최종 픽셀 규격 |
| `transparent` | 투명 PNG 여부(Y/N) |
| `tier` | `draft`(현재 소스) / `final`(고퀄 교체 완료) |
| `comfy_workflow` | 사용 워크플로우 체인 |
| `comfy_seed` | 재현용 seed |
| `gpt_ref` | final 생성 시 톤 레퍼런스로 쓸 에셋 id (일관성용, 없으면 —) |
| `path` | 결과 파일 경로 |
| `status` | OK / 재작업 필요 |

```
| id | subject | size | transp | tier | comfy_workflow | comfy_seed | gpt_ref | path | status |
|----|---------|------|:------:|:----:|----------------|-----------|---------|------|--------|
```

> 전체 comfy/gpt 프롬프트는 `base + subject`로 조립되므로 표에는 subject만 둔다.
> draft→final 교체 시 `tier`를 `final`로 바꾸고 `path`를 갱신한다. (Unity 임포트·슬롯 교체는 클라팀)

---

## 4. 새 프로젝트 적용 절차
1. 이 파일을 `Projects/[ProjCode]/[ProjCode]_PromptRegistry.md`로 복사.
2. §2 스타일 base 3종을 그 게임에 맞게 교체(스타일 락).
3. 에셋 생성할 때마다 §3 표에 한 줄 추가(생성=레지스트리 기록).
4. 폴리시: `tier=draft` 중 눈에 띄는 것만 골라 `gpt_prompt`(=gpt_base+subject)로 final 생성 → `tier=final`·`path` 갱신.

> 이 워크플로우·포맷은 **스튜디오 표준**이므로 프로젝트가 바뀌어도 그대로 상속된다. 바뀌는 건 §2 base와 §3 데이터뿐.
