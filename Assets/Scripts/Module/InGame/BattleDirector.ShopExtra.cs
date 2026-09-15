using System.Collections.Generic;
using Game.Character;
using Game.Module.Events;
using UnityEngine;
using Localize = Game.Module.Common.Localize;

namespace Game.Module.InGame
{
    /// <summary>
    /// 상점에 **몸과 소모품**을 더한다 (기획 2026-09-08 §6).
    ///
    /// 007 은 보스까지 세 방 전이다. 그때까지 카드만 팔면 「무엇을 살까」가
    /// 곧 「어떤 배수를 올릴까」로 끝난다 — **어떤 몸으로 보스에 들어갈까**가
    /// 결정이 되어야 그 자리가 보스전의 일부가 된다.
    ///
    /// 소모품 셋은 전부 **다음 전투방이 열릴 때** 터진다. 상점 방에는 적이 없어서
    /// 그 자리에서 쓸 수가 없고, 「사 두었다가 다음 방에서 열린다」가
    /// 이 방을 보스 준비로 만든다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        // ── 몸 구매 ──────────────────────────────────────────────
        //
        // 이 판에서 **한 번이라도 만난 몸**만 판다. 처음 보는 이름을 파는 것은
        // 상점이 아니라 뽑기다 — 아까 못 뺏고 지나친 그 몸이 여기 있어야
        // "그때 놓친 것을 돈으로 되산다" 가 된다.

        private readonly HashSet<string> _metHostKeys = new();
        private readonly List<HostEntry> _hostOffers = new(2);

        /// <summary>
        /// 방에 선 몸을 기록해 둔다.
        ///
        /// ⚠ **잡몹은 안 센다.** 해골·박쥐도 `HostEntry` 로 만들어져 있어서
        ///   그냥 담으면 상점이 해골을 판다. 로비 명부(`HostTable`)에 있는 것만 —
        ///   즉 **빼앗아 탈 수 있는 몸**만 목록에 들어간다.
        /// </summary>
        private void NoteMetHost(HostEntry entry)
        {
            if (entry == null || entry.IsGhost || _player == null) return;
            if (_player.GetHost(entry.HostKey) == null) return;
            _metHostKeys.Add(entry.HostKey);
        }

        /// <summary>
        /// 몸값 — **그 챕터 표에서 가장 비싼 카드와 같은 줄**(정본 CH1 66 · CH2 104 · CH3 157).
        ///
        /// ⚠ 「지금 진열된 셋 중 최고」로 재면 안 된다. 셋은 매번 다르게 뽑히므로
        ///   싼 것만 걸린 날에는 몸이 카드보다 싸진다 — 값이 그날 운에 흔들린다.
        ///   표 전체에서 재야 「이 챕터에서 몸은 이만큼」이 고정된다.
        /// </summary>
        private int HostPriceOf()
        {
            int top = 0;
            if (_shopTable != null)
            {
                int ch = Mathf.Clamp(_runChapter, 1, 3);
                var all = _shopTable.Offers;
                for (int i = 0; i < all.Count; i++)
                    if (all[i].Chapter == ch && all[i].Price > top) top = all[i].Price;
            }
            // 표를 못 읽었을 때를 위한 대비값 — 회복값의 1.5배쯤이 그 줄이다.
            if (top <= 0) top = Mathf.Max(40, Mathf.RoundToInt(_shopRules.HealPrice * 1.5f));
            return top;
        }

        // ── 소모품 ───────────────────────────────────────────────

        /// <summary>산 소모품. 다음 전투방이 열릴 때 하나가 터진다.</summary>
        public enum Consumable { None, Bomb, Freeze, Ally }

        // ⚠ 폭탄은 **방을 비우는 물건이 아니다.**
        //   처음에 60 · 반경 4.5 m 로 뒀더니 CH1 방이 통째로 지워졌다(잡몹 HP 9~31).
        //   그러면 14 골드로 방 하나를 사는 셈이라 전투가 사라진다.
        //   약한 것은 죽고 센 것은 반피가 되는 줄 — 그게 「한 방 먹인다」다.
        private const int BombDamage = 18;          // CH1 기준. 아래에서 챕터 성장이 곱해진다
        private const float BombRadiusMeters = 3.5f;
        private const float ConsumableFreezeSeconds = 5f;

        private Consumable _pendingConsumable;
        private Consumable _shopConsumable;         // 이번 상점이 진열한 것


        private static string ConsumableNameOf(Consumable c) => c switch
        {
            Consumable.Bomb   => Localize.Get("ui.consumable.bomb.name"),
            Consumable.Freeze => Localize.Get("ui.consumable.freeze.name"),
            Consumable.Ally   => Localize.Get("ui.consumable.ally.name"),
            _                 => string.Empty,
        };

        private static string ConsumableDescOf(Consumable c) => c switch
        {
            Consumable.Bomb   => Localize.Get("ui.consumable.bomb.desc"),
            Consumable.Freeze => Localize.Format("ui.consumable.freeze.desc", ConsumableFreezeSeconds),
            Consumable.Ally   => Localize.Get("ui.consumable.ally.desc"),
            _                 => string.Empty,
        };

        private static string ConsumableIconOf(Consumable c) => c switch
        {
            Consumable.Bomb   => "buffcard_c013",   // 폭발 메아리
            Consumable.Freeze => "buffcard_c025",   // 냉기 각인
            _                 => "buffcard_c030",   // 동료 — 유령 포대 그림을 빌린다
        };

        /// <summary>공용 카드의 1/3. 최소 10 골드는 받는다.</summary>
        private int ConsumablePrice()
        {
            int sum = 0;
            for (int i = 0; i < _shopOffers.Count; i++) sum += _shopOffers[i].Price;
            int avg = _shopOffers.Count > 0 ? sum / _shopOffers.Count : 42;
            return Mathf.Max(10, avg / 3);
        }

        /// <summary>
        /// 이번 상점이 내놓을 소모품 하나를 정한다.
        ///
        /// ⚠ 셋을 다 진열하지 않는다. 액자는 여섯 칸뿐이고, 카드 셋·몸·회복까지
        ///   앉히면 자리가 없다. 한 판에 상점을 여섯 번 들르므로 셋을 다 만난다.
        /// </summary>
        private void RollShopConsumable()
            => _shopConsumable = (Consumable)(_rng.Next(3) + 1);

        /// <summary>
        /// **적이 설 때까지 기다렸다가** 터진다.
        ///
        /// ⚠ 처음엔 방에 들어선 그 프레임에 터뜨렸다가 아무 일도 안 일어났다.
        ///   적이 그 프레임에 다 서 있지 않기 때문이다 — 자리마다 트리거가 다르다
        ///   (`ROOM_ENTER` 와 `POSSESSION_TARGET`).
        /// ⚠ 그래서 0.2초를 기다리게 고쳤더니 이번엔 **그 0.2초에도 비어 있는 방**이
        ///   있어 또 놓쳤다. 시간을 재는 대신 **조건을 본다** — 산 것이 하나라도
        ///   서면 그때 터진다. 방을 잘못 만나 그냥 사라지는 일이 없다.
        /// </summary>
        private void TickConsumable(float dt)
        {
            if (_pendingConsumable == Consumable.None) return;
            if (_enemies.Count == 0) return;
            FireConsumableOnRoomStart();
        }

        /// <summary>전투방에서만 — 상점·회복·이벤트 방에는 적이 없다.</summary>
        private void FireConsumableOnRoomStart()
        {
            if (_pendingConsumable == Consumable.None) return;
            // 적이 아직 없으면 **쓰지 않고 들고 간다.** 빈 방에 쓰고 사라지면
            // 산 것이 조용히 없어진 셈이다.
            if (_enemies.Count == 0) return;

            var used = _pendingConsumable;
            _pendingConsumable = Consumable.None;

            switch (used)
            {
                case Consumable.Bomb:
                {
                    var at = new Vector2(_roomSize.x * 0.5f, -_roomSize.y * 0.45f);
                    float r = Meters(BombRadiusMeters);
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(BombDamage * EnemyGrowth()));
                    PlayFx("goo_burst", at, r * 2f, loop: false);
                    for (int i = _enemies.Count - 1; i >= 0; i--)
                    {
                        var e = _enemies[i];
                        if (e == null || !e.IsAlive || e.IsDying) continue;
                        if (Vector2.Distance(e.Position, at) > r) continue;
                        HitEnemyWith(e, dmg, null);
                    }
                    break;
                }

                case Consumable.Freeze:
                    for (int i = 0; i < _enemies.Count; i++)
                    {
                        var e = _enemies[i];
                        if (e == null || !e.IsAlive || e.IsDying) continue;
                        e.ApplyStun(ConsumableFreezeSeconds);
                    }
                    break;

                default:
                    // 동료 — 같이 싸우는 몸 하나를 세운다 (`BattleDirector.Ally.cs`).
                    SpawnAlly();
                    break;
            }
        }

        /// <summary>산 소모품이 터질 때가 됐는지 본다. 예전 「미끼」 자리다.</summary>
        private void TickBait(float dt) => TickConsumable(dt);

        // ── 판이 시작하면 전부 지운다 ────────────────────────────
        private void ClearShopExtras()
        {
            _metHostKeys.Clear();
            _hostOffers.Clear();
            _pendingConsumable = Consumable.None;
            _shopConsumable = Consumable.None;
        }
    }
}
