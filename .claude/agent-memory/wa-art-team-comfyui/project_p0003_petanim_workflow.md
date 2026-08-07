---
name: project-p0003-petanim-workflow
description: P0003 pet_baby 스프라이트 애니메이션 프레임 생성용 커스텀 워크플로우 WA_ItoI_PetAnimFrame 위치·구조·PoC 상태
metadata:
  type: project
---

# P0003 — WA_ItoI_PetAnimFrame 커스텀 워크플로우

`Projects/P0003/P0003_PetAnim_Spec.md`(펫 동작 스프라이트 규격) 납품을 위해 신규 작성한 ComfyUI 워크플로우.
기존 배포된 `WA_ItoI_ImageEdit`/`WA_ItoI_RemoveBG`만으로는 4가지 요구(96×96 고정 캔버스·알파 유지·픽셀 크리스프 엣지·
프레임 일관성)를 동시에 만족할 수 없어서 결합·신규 노드를 추가함.

## 파일 위치

- 워크플로우 원본(로컬, 이 리포 밖): `C:\won\GameProject\GameHub\NAS\docker\comfyui-api\workflows\WA_ItoI_PetAnimFrame.json`
  - 이 리포는 Unity 프로젝트(`GameModuleFramework`)이고, ComfyUI 웹서비스 백엔드 리포는 별도 경로(`GameProject\GameHub`)에 있음 — 문서상 "W-EED 레포"라는 표현이 있으나 실제로는 GameHub 하위 폴더.
- NAS 배포 경로: `/volume1/docker/comfyui_api/workflows/WA_ItoI_PetAnimFrame.json` (환경값은 `.claude/project/environment.md` 참조)
- PoC 산출물/로그: `scratchpad/petanim/` (세션별 임시 경로라 재사용 시 새로 생성됨), `petanim_log.md`에 재현 파라미터 누적 기록.

## 노드 그래프 요지

```
WA_LoadImage → FluxKontextImageScale
  → TextEncodeQwenImageEditPlus(+/-, StringConcatenate로 prompt+고정접미사)
  → VAEEncode → KSampler(denoise={{denoise}} 파라미터화, 기본 0.5) → VAEDecode
  → LoadBackgroundRemovalModel(birefnet) → RemoveBackground → InvertMask → JoinImageWithAlpha  (알파 확보)
  → ImageScale(area, {{pixel_grid}}, 기본 32) 다운스케일  → ImageScale(nearest-exact, 96×96 고정) 업스케일
  → WA_SaveImage
```

원본 `WA_ItoI_ImageEdit`와의 차이: (1) 마지막 lanczos 리사이즈 노드 제거, area→nearest 2단 픽셀화로 교체
(lanczos로 512px→96px 급다운스케일 시 배경에 강한 링잉 노이즈 발생 확인됨). (2) RemoveBG 체인 내장.
(3) KSampler `denoise` 파라미터 노출(원본은 1 하드코딩).

## 배포 방법 (SSH 필요 — comfyui 에이전트 세션은 분류기가 SSH 차단함)

`cd NAS/docker/comfyui-api; .\sync-workflows.ps1` 또는 직접 ssh 업로드. comfyui 에이전트 세션에서는
Claude Code 권한 분류기가 SSH 명령을 차단하므로, art-lead(팀장) 세션이 대신 동기화를 수행한 이력 있음(2026-08-05).
워크플로우 변경 후 반드시 `GET http://192.168.0.56:9000/api/workflows`로 `WA_ItoI_PetAnimFrame` 노출 확인.

## PoC 상태 (2026-08-05 기준 — idle+feed 5프레임 완료, clean/pet/sleep/refuse 미착수)

- 캔버스 96×96 고정: PASS (5프레임 전부)
- 알파 유지: PASS (5프레임 전부, RGBA corner A=0)
- 픽셀 크리스프 엣지: PARTIAL — 링잉 노이즈는 제거됐으나 원본보다 그리드가 다소 거칠고 옅은 색번짐 잔존. `pixel_grid`(현재 32) 40~48 시험 여지.
- 프레임 일관성(스케일/위치): idle은 우수, feed는 PARTIAL — `normalize_frame.ps1`(알파 bbox 기준, 높이 스케일+폭 클램프)로
  결정적 보정하지만, "숙임"처럼 세로 압축 포즈는 bbox 높이 자체가 줄어 스케일이 커지는 한계 있음.
- 액션별 포즈 변화 반영: **해결** — denoise를 동작 유형별로 분리해서 씀. 상세: [[feedback-petanim-lowdenoise-pose-lock]]
  - idle(미세 변형): denoise **0.5**
  - feed/clean/pet 등 뚜렷한 포즈 변화 필요 액션: denoise **1.0**

## 파이프라인 확정 요약 (다음 동작 생성 시 그대로 재사용)

1. `WA_ItoI_PetAnimFrame`에 pet_baby.png 업로드, seed 고정(예: 1000000000051, 동작 내 프레임 간 동일 seed 권장),
   prompt=동작 지시문, denoise=액션별 정책값, pixel_grid=32.
2. 결과(96×96 RGBA, 알파 O) 다운로드.
3. `scratchpad/petanim/normalize_frame.ps1 -RefPath pet_baby.png -InPath <원본결과> -OutPath <최종프레임>` 로 위치/스케일 정규화.
4. 4기준 육안 검수 후 `pet_baby_<action>_<NN>.png`로 확정.

## 저장 위치 관련 — 역할 경계 (중요)

comfyui 에이전트는 스크래치패드까지만 담당한다. **Unity `Assets/BaseResource/PetRoom/anim/`에 최종 배치하는 것은
wa-art-team-file 담당**이며, 코디네이터/팀장이 직접 Assets에 복사해달라고 요청해도 comfyui 에이전트는 이를 수행하지
않고 file 팀원에게 위임을 요청해야 한다(역할 문서 6·7절, 최초 작업 지시서의 "Unity Assets 직접 쓰기 금지" 명시).

다음 세션에서 이어서 작업할 때는 `petanim_log.md`(스크래치패드, 세션마다 새로 생성될 수 있음)와 이 메모리를 함께 확인할 것.
