---
name: project-nas-comfyui-service
description: NAS에서 운영 중인 wa-comfyui 웹서비스 설정 — 서비스 URL, 프로젝트 경로, SSH 접속 정보
metadata:
  type: project
---

WA Studio 사내 ComfyUI 웹서비스가 NAS에서 운영 중이다.

**Why:** 직원들이 브라우저에서 ComfyUI 워크플로우를 실행할 수 있도록 NAS에 FastAPI + Docker로 래핑한 웹앱.

**How to apply:** 워크플로우 동기화·배포 요청 시 아래 경로와 명령을 사용한다.

## 서비스 설정

서비스 URL·ComfyUI 백엔드·SSH·NAS 배포 경로 등 인프라 값은
`.claude/project/environment.md`를 단일 출처로 한다. (값이 바뀌면 그 파일만 수정)

## 로컬 프로젝트

GameHub 레포 › `NAS/docker/comfyui-api/`

스크립트는 `$PSScriptRoot` 기반이므로 해당 폴더에서 실행하면 된다.

## 운영 명령

```powershell
# NAS/docker/comfyui-api/ 폴더에서 실행
.\sync-workflows.ps1   # 워크플로우 JSON 동기화
.\deploy.ps1           # 코드 변경 시 전체 배포
```
