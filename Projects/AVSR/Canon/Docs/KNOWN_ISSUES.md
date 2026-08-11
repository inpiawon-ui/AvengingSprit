# Known Issues / Developer Decisions

- Unity 세부 버전과 Navigation 패키지는 기존 개발 환경 확인 후 확정한다. Data Contract에는 영향 없음.
- Attack/Projectile/Collider 수치는 Prototype first-pass이며 P0/P1 플레이 테스트에서 조정 가능하다. 변경은 Balance Change Log에 기록한다.
- Addressables는 optional. 미사용 시 Registry direct reference를 쓴다.
- 최종 VFX/SFX/Animator는 art delivery 전 Placeholder hook을 사용한다.
- 이 패키지는 Unity 실행 증거를 포함하지 않으므로 Compile/Test PASS로 판정하지 않는다.