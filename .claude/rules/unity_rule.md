---
description: Unity 프로젝트 파일 관리 운영 규칙 — .meta 파일 동반 삭제
alwaysApply: true
---

# Unity 파일 관리 규칙

## 1. .meta 파일 동반 삭제 (필수)

**파일이나 폴더를 삭제할 때는 반드시 .meta 파일도 함께 삭제하라.**

Unity는 모든 에셋과 폴더에 대해 `.meta` 파일을 생성하여 GUID와 임포트 설정을 관리한다.
`.meta` 파일을 남기면 Unity 에디터가 고아 GUID를 감지하여 불필요한 경고가 발생하거나,
재임포트 시 설정이 충돌할 수 있다.

삭제 전 확인 절차:
1. 삭제 대상 파일/폴더와 동일한 경로에 `{이름}.meta` 파일이 존재하는지 확인한다.
2. 존재하면 원본과 `.meta`를 동시에 삭제한다.
3. 폴더 삭제 시 폴더 자체의 `.meta`도 함께 삭제한다.

```
# 파일 삭제 예시
IModule.cs      → IModule.cs.meta  도 삭제
Interface/      → Interface.meta   도 삭제
```
