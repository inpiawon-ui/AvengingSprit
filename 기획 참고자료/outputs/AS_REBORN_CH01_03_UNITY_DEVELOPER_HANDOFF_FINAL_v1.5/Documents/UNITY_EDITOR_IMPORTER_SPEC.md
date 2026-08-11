# Unity Editor Importer Spec v1.5

Menu: Import Game Data / Validate Data / Generate Placeholder Prefabs / Generate Room Prefabs / Bake Navigation / Run Smoke Test / Export Validation Report.

Pipeline: JSON schema → ID/reference validation → Runtime assets → Registry resolve → Room hierarchy → spawn/object/trigger/camera → nav/collider → report.

Spawn Runtime Source는 layout.enemySpawns 하나뿐이다. 최상위 spawns를 탐색하거나 fallback으로 사용하면 Import Error 처리한다. Candidate HostRefID는 HostID 또는 HostPoolID만 허용하며 DisplayName과 null을 금지한다. 모든 positional array는 DATA_CONTRACT_v1.5 collectionSchemas의 Field Order/Type/Required/Default로 역직렬화한다.

Importer는 idempotent해야 하며 같은 v1.5 JSON 재수입 시 사용자 ViewRoot와 수동 art reference를 덮어쓰지 않는다. 생성 자산에는 SourceHash와 ContractVersion을 기록한다. Error는 생성 중단, Warning은 리포트 후 fallback 허용.