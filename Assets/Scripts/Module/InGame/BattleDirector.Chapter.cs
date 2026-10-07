using System.Collections.Generic;
using Game.Character;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 챕터 표(`GameConfig.ChapterOf`)를 전투 쪽에서 읽는 자리.
    ///
    /// **코드에 챕터 번호를 적지 않는다**(목표 100챕터, 기획 2026-10-01). 예전에는 챕터 수 6 과
    /// 6칸 배열이 이 클래스 여기저기에 박혀 있었다 — 잡몹 목록 · 클리어 골드 · 상자 · 이벤트 골드 배율 ·
    /// 바닥 · 상점 값. 하나라도 빠뜨리면 그 챕터만 조용히 6챕터 값으로 떨어졌다.
    /// 이제 전부 표 한 줄에서 온다. 표가 비어 있으면(임포트 전) 예전 값으로 떨어진다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>챕터 수 — 챕터 표의 줄 수.</summary>
        private int ChapterCount => _config != null ? _config.ChapterCount : 6;

        private bool HasChapterTable => _config != null && _config.HasChapterTable;

        /// <summary>그 챕터의 줄. 표가 없으면 기본값(전부 0 · null).</summary>
        private GameConfig.ChapterDef ChapterRow(int chapter)
            => _config != null ? _config.ChapterOf(chapter) : default;

        // ── 잡몹 ────────────────────────────────────────────────

        private readonly Dictionary<int, string[]> _trashKeysByChapter = new();
        private readonly Dictionary<int, HostEntry[]> _trashPoolByChapter = new();

        /// <summary>잡몹 키 → 잡몹. 잡몹은 `HostTable` 에 없고 코드가 만든다.</summary>
        private static HostEntry TrashEntryOf(string key)
            => key == TrashSkeletonKey ? Skeleton
             : key == TrashBatKey      ? Bat
             : key == TrashGunnerKey   ? Scrapgunner
             : key == TrashEnforcerKey ? Enforcer
             : key == TrashWardenKey   ? Roadwarden
             : key == TrashCoilKey     ? Coilwalker
             : key == TrashCrossKey    ? Cross
             : null;

        /// <summary>
        /// 이 챕터에 나오는 잡몹 목록 — 챕터 표의 `trash` 칸.
        /// 방 배치 검사(`rooms90_build.py`)가 같은 칸을 읽어, 목록에 없는 잡몹이 방에 서면 굽기 전에 막는다.
        /// </summary>
        private string[] TrashKeysFor(int chapter)
        {
            if (_trashKeysByChapter.TryGetValue(chapter, out var cached)) return cached;

            string[] keys;
            var row = ChapterRow(chapter);
            if (row.Trash != null && row.Trash.Length > 0)
            {
                keys = new string[row.Trash.Length];
                for (int i = 0; i < keys.Length; i++) keys[i] = TrashKeyAlias(row.Trash[i]);
            }
            else keys = LegacyTrashKeysFor(chapter);

            _trashKeysByChapter[chapter] = keys;
            return keys;
        }

        /// <summary>회전 목록 · 역할 대체가 쓰는 잡몹 통. 손배치가 없는 방(절차 생성)만 여기로 온다.</summary>
        private HostEntry[] TrashPool(int chapter)
        {
            if (_trashPoolByChapter.TryGetValue(chapter, out var cached)) return cached;

            var keys = TrashKeysFor(chapter);
            var list = new List<HostEntry>(keys.Length);
            for (int i = 0; i < keys.Length; i++)
            {
                var e = TrashEntryOf(keys[i]);
                if (e != null) list.Add(e);
            }
            if (list.Count == 0) list.Add(Skeleton);
            var pool = list.ToArray();
            _trashPoolByChapter[chapter] = pool;
            return pool;
        }

        /// <summary>
        /// 잡몹 패턴 단계. 같은 잡몹도 단계가 오르면 다르게 싸운다(`BattleDirector.Patterns2`).
        /// 표의 `pattern` 칸 — 1~6 챕터는 제 번호, 7챕터부터는 7(집행자 돌진, 2026-10-07).
        /// </summary>
        private int PatternStageOf(int chapter)
        {
            int p = ChapterRow(chapter).Pattern;
            return p > 0 ? p : chapter;
        }

        // ── 값 ──────────────────────────────────────────────────

        /// <summary>판 안 값(상점 · 이벤트)의 배율. 방이 떨구는 골드가 오르는 만큼 값도 오른다.</summary>
        private float RunPriceMul
        {
            get
            {
                float m = ChapterRow(_runChapter).PriceMul;
                return m > 0f ? m : 1f;
            }
        }

        /// <summary>
        /// 상점이 쓰는 표의 챕터.
        ///
        /// 상점 표는 3챕터분뿐이다. 챕터 표가 있으면 **1챕터 진열을 기준으로 값만 곱한다** —
        /// 챕터마다 진열표를 따로 적으면 100챕터를 못 간다. 표가 없으면 예전대로 1~3 으로 자른다.
        /// </summary>
        private int ShopChapter => HasChapterTable ? 1 : Mathf.Clamp(_runChapter, 1, 3);

        /// <summary>상점 표의 값(1챕터 기준)을 지금 챕터 값으로.</summary>
        private int ShopPrice(int basePrice)
            => HasChapterTable ? Mathf.Max(1, Mathf.RoundToInt(basePrice * RunPriceMul)) : basePrice;

        private int ShopHealPrice => _shopRules != null ? ShopPrice(_shopRules.HealPrice) : 0;
    }
}
