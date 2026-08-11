# Room Prefab Structure

PF_ROOM_[RoomID]
- RoomController
- Geometry
- Obstacles
- Hazards
- PlayerSpawn
- EnemySpawnPoints
- PossessionCandidatePoints
- WaveTriggers
- SafeZones_Debug
- AttackLanes_Debug
- EntryGate
- ExitGate
- CameraBounds
- NavRoot

Generated root와 DesignerOverride root를 분리한다. 재생성 시 DesignerOverride는 보존한다.