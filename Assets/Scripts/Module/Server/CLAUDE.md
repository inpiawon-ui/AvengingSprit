# Scripts/Module/Server — 프로젝트 전용 서버 통신

이 폴더는 **이 프로젝트 전용 서버 통신 코드**를 담는다.

## 역할

- `Assets/GameFramework/Game/Module/Server/`(범용 API·URL 클라이언트)를 **참조**하여,
  이 게임에서만 사용하는 서버 호출·응답 처리·이벤트 발행을 작성한다.
- 네임스페이스: `Game.Module.Server` (`Assets/Scripts/`는 `Game.*` 유지 — `GameFramework.Game.*`과 구분)

## 작업 주체

- 이 폴더는 **클라이언트 팀**(`wa-client-team-game` / `wa-client-team-network`)이 작성한다.
- 작성 근거는 **서버팀**(`wa-manager-server-lead`)이 산출한 계약 스펙(`SRV_Design.md`)이며,
  `wa-manager-client-lead` 핸드오프를 통해 전달된다.

## 규칙

- 서버 응답 → 게임 상태 반영은 직접 호출이 아니라 `IEventBus.Publish()`로 이벤트 발행 후 처리한다.
- 서버 주소·엔드포인트를 하드코딩하지 않는다. 범용 레이어와 `constants.md` 서버 API 섹션을 참조한다.
- 상세 규칙: [`.claude/rules/project/server/coding_rules.md`](../../../../.claude/rules/project/server/coding_rules.md)

> 현재 비어 있음. 서버 계약 확정 후 코드가 추가된다.
