---
description: 스튜디오/인프라 고유 값 단일 출처 — ComfyUI 서비스·NAS 경로·SSH 접속. 다른 스튜디오 환경으로 옮길 때 이 파일만 수정한다.
---

# 스튜디오/인프라 값 (Environment)

이 파일은 **스튜디오 환경마다 바뀌는 인프라 값의 단일 출처**다.
다른 문서·에이전트 파일은 IP/경로/계정을 인라인으로 박지 않고 이 파일을 참조한다.
다른 스튜디오·네트워크로 옮길 때는 **이 파일만 수정**한다. (게임별 설계값은 [`constants.md`](constants.md) 참조)

> 변경 영향 범위: 이 파일의 값만 고치면 `project/wa-comfyui-service.md`, `agents/art/wa-art-team-comfyui.md`가 갱신된 값을 참조한다.

---

## 1. ComfyUI 웹서비스 (NAS 운영)

WA Studio 사내 ComfyUI 웹서비스. NAS에서 FastAPI + Docker로 실행된다.

```
직원 브라우저 → NAS:9000 (FastAPI/Docker) → Art PC:8000 (ComfyUI GPU)
```

| 항목 | 값 |
|------|-----|
| 서비스 URL | `http://192.168.0.56:9000` |
| ComfyUI 백엔드 | `http://192.168.0.247:8000` |
| Docker 컨테이너 | `comfyui-api` |

## 2. NAS / 배포 경로

| 항목 | 값 |
|------|-----|
| 프로젝트 경로 | W-EED 레포 › `NAS/docker/comfyui-api/` |
| NAS 배포 경로 | `/volume1/docker/comfyui_api/` |

## 3. SSH 접속

| 항목 | 값 |
|------|-----|
| SSH 사용자 | `ntriple` |
| SSH 키 | `$env:USERPROFILE\.ssh\nas_key` |

## 4. 서버 인프라 (Server) — 플레이스홀더

서버 백엔드의 스튜디오/인프라 값. 스택·경로 확정 시 채운다. (`wa-server-team-infra` 관리)

| 항목 | 값 |
|------|-----|
| 백엔드 코드 경로 | `(미정 — 별도 repo/폴더, 스택 확정 후 지정)` |
| 기술 스택 | `(미정 — C#/.NET · Node.js/TS · Go 등)` |
| dev 환경 URL | `(미정)` |
| staging 환경 URL | `(미정)` |
| prod 환경 URL | `(미정)` |
| 배포 타겟 | `(미정 — 컨테이너/오케스트레이션)` |

> **시크릿 금지**: API 키·DB 비밀번호·서명 키 등 실제 시크릿 값은 이 파일에 적지 않는다.
> 시크릿은 환경변수/시크릿 매니저로 주입하며, 주입 "방식"만 문서화한다. (`server/coding_rules.md` 4절)

---

## 인덱스 — 이 파일에 합칠 수 없는 인프라 값

아래 값은 **PowerShell이 실행하는 코드**라 마크다운으로 분리할 수 없다. 위치만 안내한다.

| 항목 | 실제 수정 위치 |
|------|---------------|
| 프레임워크 수정 허용 이메일 (스튜디오/오너 계정) | `.claude/hooks/protect_framework.ps1` 최상단 `$FrameworkOwnerEmails` 블록 — `git config user.email` 대조 |
