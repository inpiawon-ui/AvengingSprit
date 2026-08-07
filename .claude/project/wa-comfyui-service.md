# wa-comfyui 서비스 참조

WA Studio 사내 ComfyUI 웹서비스 (NAS 운영).

## 아키텍처

```
직원 브라우저 → NAS:9000 (FastAPI/Docker) → Art PC:8000 (ComfyUI GPU)
```

## 핵심 정보

URL·경로·SSH 등 인프라 값은 [`environment.md`](environment.md)를 단일 출처로 한다.
(서비스 URL·ComfyUI 백엔드·NAS 배포 경로·Docker 컨테이너·SSH 사용자/키)

## 담당 에이전트

`wa-art-team-comfyui`
