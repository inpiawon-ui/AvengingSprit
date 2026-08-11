# Character Resource Spec

공통 필수: View Prefab, Idle/Move/Attack/Hit/Death, Host는 Ultimate/PossessionIn/PossessionOut, Pivot/Scale/Feet, Weapon/Projectile/VFX sockets, Hitbox/Hurtbox reference, Sorting Layer, Shadow. LogicRoot 수정 금지.

Host signature assets:
- H01 Gangster: Bullet, MuzzleFlash, Mark FX
- H02 Fighter: Punch arc, Dash trail, Finisher FX
- H03 Salamander: Breath cone, Burn, Armor melt
- H05 Grenadier: Grenade, Mine, Explosion zone
- H08 Robot: Drone, Mine, Laser, Turret, Overheat
- H09 Guru: Guard aura, Heal/Guard pulse
- H10 White Wizard: Orb, Slow/Freeze zone
- H11 Ninja: Shuriken, Blink, Execution
- H12 Medium: Curse node/circuit
- H15 Vampire: Leech mark, Heal transfer
- H16 Baseball Player: Ball, bounce path, impact
- H20 Dragoon: melee trail, inherited effect, finisher

AnimatorBridge parameter names and sockets must follow Registry IDs; resource swap acceptance requires no Gameplay Logic modification.