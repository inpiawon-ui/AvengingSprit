---
name: feedback-petanim-lowdenoise-pose-lock
description: Qwen-Image-Edit 기반 저-denoise img2img에서 "동일 실루엣 유지" 프롬프트가 요청한 포즈 변화 자체를 억제하는 현상과 대응
metadata:
  type: feedback
---

# 저-denoise + "동일 실루엣 유지" 프롬프트 = 포즈 변화 억제

P0003 pet_baby 애니메이션 프레임 PoC(`Projects/P0003/P0003_PetAnim_Spec.md`)에서 발견.

## 증상

`WA_ItoI_ImageEdit` 계열(Qwen-Image-Edit-2509 + Lightning 4-step LoRA, `Assets/Scripts`가 아닌 NAS ComfyUI 워크플로우)로
KSampler `denoise=0.5` + 고정 접미사 프롬프트에 `"same silhouette, same camera framing and same scale"`를 넣고
"그릇 쪽으로 숙임", "냠냠", "만족" 등 뚜렷한 포즈 변화를 요청했으나, **결과 이미지가 전부 원본과 동일한 정자세로 나옴.**
seed 고정 + 프롬프트만 다르게 하면 픽셀 diff는 20~30% 발생하지만 색·음영 노이즈 수준일 뿐 구조적 포즈 변화는 없었음.

## Why

KSampler `denoise<1`은 원본 latent(=원본 포즈)를 일부 보존한다. 여기에 "실루엣/카메라 프레이밍/스케일을 유지하라"는
프롬프트까지 더해지면 두 신호가 겹쳐 "포즈 보존" 압박이 "포즈 변화 지시"를 압도해버린다.
캔버스 스케일·위치 드리프트를 프롬프트로 잡으려던 시도가 부작용으로 동작 자체를 죽인 사례.

## How to apply

- **스케일/위치 고정은 프롬프트가 아니라 후처리로 해결한다.** 생성 결과의 알파 bbox를 계산해 레퍼런스 알파 bbox와
  높이 기준 스케일 맞추고 하단중앙(bottom-center pivot) 정렬하는 결정적(deterministic) 스크립트를 쓴다
  (`scratchpad/petanim/normalize_frame.ps1` 참고 — PowerShell + System.Drawing, nearest-neighbor 리샘플로 픽셀 크리스프 유지).
- 고정 접미사 프롬프트에는 **identity/스타일 보존만** 넣는다 (얼굴형·색 팔레트·픽셀아트 스타일).
  `"same silhouette"`, `"same pose"`, `"same camera framing/scale"` 류 문구는 넣지 않는다 — 이건 후처리가 담당.
- 뚜렷한 포즈 변화가 필요한 액션 프레임(feed/clean/pet 등)은 denoise를 idle보다 높여야 할 가능성이 큼
  (idle은 미세 변형만 필요하므로 낮은 denoise 유지, action은 0.7~1.0 재검증 필요 — PoC 시점에 미확정).
- denoise 값 선택 시 얼굴 디테일(눈·코 형태 뭉개짐 여부)을 기준으로 판단: 0.5 vs 1.0 비교에서 1.0은
  이목구비가 흐려지는 경향 확인됨 → 가능한 낮은 denoise를 쓰되, 포즈 변화가 실제로 나타나는 최저값을 찾는 방식 권장.

## 후속 확정 (2026-08-05, feed 액션 재검증)

접미사만 완화(위 조치)하고 denoise=0.5/0.8을 유지한 채 재테스트해도 **여전히 포즈 변화가 나타나지 않았다.**
denoise=1.0에서야 비로소 실제 포즈 변화(그릇 숙임 등)가 나타남. 원인은 프롬프트뿐 아니라
**Lightning 4-step LoRA(KSampler steps=4 고정)** 자체에 있었다: denoise<1이면 실질 디노이즈 스텝 수가
`steps × denoise`로 줄어들어(예: denoise=0.5 → 약 2스텝) 모델이 새 포즈를 그릴 여력이 부족해진다.

**최종 정책**: 동작 유형에 따라 denoise를 다르게 쓴다.
- **idle처럼 미세 변형만 필요한 프레임**: denoise 0.5 (얼굴 디테일 보존 우선)
- **feed/clean/pet처럼 뚜렷한 포즈 변화가 필요한 액션 프레임**: denoise 1.0 (포즈 반영 우선, 이목구비 디테일은 다소 손해)

또한 denoise=1.0으로 포즈가 크게 바뀌면 알파 bbox가 원본(직립)보다 훨씬 넓어지는 경우가 생김(예: 숙이는 포즈).
`normalize_frame.ps1`을 **높이 기준 스케일 + 폭 상한 클램프(canvas×0.95/bboxW)** 로 개선해 좌우 잘림을 방지했다.
단, bbox 높이 자체가 포즈에 따라 줄어들 수 있어(웅크림 등) 높이 기준 정규화만으로는 프레임 간 스케일이
완벽히 고정되지 않는 한계가 남아있음 — 더 견고히 하려면 "머리 폭" 등 포즈 불변 기준으로 바꾸는 것을 고려할 것.

관련: [[project-p0003-petanim-workflow]]
