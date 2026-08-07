# Memory Index

- [GameFramework 수정 금지 정책 위반 패턴](project_framework_modification_policy.md) — protect_framework.ps1 훅이 주석 처리된 상태에서 프레임워크 파일 대량 수정 발생
- [TestStart.cs 위치 규칙 위반 패턴](project_teststart_placement.md) — 임시 테스트 스크립트가 Assets/Game/ 루트에 배치되는 반복 패턴
- [ScoreModule 한글 주석 깨짐 기존 존재 확인](project_korean_comment_corruption.md) — 한글 깨짐이 이번 수정 이전부터 존재했던 기존 문제임
- [AutoRegisterModules 순서 보장 규칙 문서화 부재](feedback_auto_register_order_gap.md) — [Module] 어트리뷰트 도입 후 EventBus 우선 등록 규칙의 자동화 적용 방식이 문서에 없음
- [UIModule.Dispose fire-and-forget 주석 누락 패턴](feedback_forget_comment_missing.md) — .Forget() 호출부에 주석 필수 규칙이 반복적으로 누락됨 (2026-05-24 수정 확인 완료)
- [게임 레이어 모듈 Layer 미명시 반복 패턴](feedback_module_layer_not_specified.md) — [Module] 어트리뷰트에 Layer = ModuleLayer.Game 생략, 기본값 Core로 잘못 분류
- [GameBootstrap 상속 체인 업데이트 누락](feedback_game_bootstrap_inheritance_gap.md) — 프레임워크 신규 Bootstrap 계층 추가 시 게임 레이어 Bootstrap 연결 미갱신
- [Object 완전 한정 이름 미사용 반복 패턴](feedback_object_qualified_name.md) — using System; + using UnityEngine; 공존 시 Destroy/DontDestroyOnLoad 한정 이름 생략 (컴파일 통과 착각)
- [using 정렬 순서 문서 공백](feedback_using_sort_order_undocumented.md) — System.* 계열 연속 배치 규칙이 어떤 문서에도 없어 신규 파일에서 불일치 발생
