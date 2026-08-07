# Security Policy

## Supported Versions

이 프로젝트는 게임 개발 공용 프레임워크 템플릿입니다.
보안 정책은 이 템플릿을 기반으로 한 실제 게임 프로젝트에서 별도로 정의합니다.

| Version | Supported |
|---------|-----------|
| main    | ✓         |

## Reporting a Vulnerability

보안 취약점을 발견한 경우 공개 이슈 대신 아래 방법으로 보고해주세요:

1. GitHub의 **Security > Report a vulnerability** 기능을 사용합니다.
2. 또는 프로젝트 관리자에게 직접 연락합니다.

## Security Guidelines

- `Assets/GameFramework/` 코드는 절대 외부에 하드코딩된 시크릿을 포함하지 않습니다.
- `DataModule` 암호화키는 환경변수 또는 런타임 주입만 사용합니다.
- `.env`, `secrets.json` 파일은 `.gitignore`에 포함되어 커밋 금지입니다.
