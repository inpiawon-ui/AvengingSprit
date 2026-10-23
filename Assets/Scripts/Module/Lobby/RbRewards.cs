using System.Collections.Generic;
using System.Text;
using Game.Module.Common;
using Game.Module.Common.Store;
using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 화면 공용 — 보상 문자열(「gem:120;ticket:5;chest:platinum*2」) 풀이 · 아이콘 · 이름 · 받은 것 한 줄.
    /// 상점 · 미션 · 패스 · 유령 성장이 같이 쓴다(한 곳에서만 고친다).
    /// </summary>
    public static class RbRewards
    {
        public static List<(string kind, string key, int amount)> Parse(string rewards)
        {
            var list = new List<(string, string, int)>();
            if (string.IsNullOrEmpty(rewards)) return list;
            foreach (var part in rewards.Split(';'))
            {
                var kv = part.Split(':');
                if (kv.Length < 2) continue;
                string kind = kv[0].Trim(), val = kv[1].Trim();
                int amount = 1;
                string key = null;
                if (kind == "chest")
                {
                    var c = val.Split('*');
                    key = c[0];
                    if (c.Length > 1) int.TryParse(c[1], out amount);
                }
                else if (!int.TryParse(val, out amount)) { key = val; amount = 1; }
                list.Add((kind, key, amount));
            }
            return list;
        }

        /// <summary>아이콘 이름(키트 rb_icon_*_96) — 화면이 가진 아이콘 목록에서 이름으로 찾는다.</summary>
        public static Sprite IconOf(Sprite[] icons, string kind, string key) => Find(icons, kind switch
        {
            "gem" => "rb_icon_currency_gem_96",
            "gold" or "goldrun" => "rb_icon_currency_gold_96",
            "ticket" => "rb_icon_summon_ticket_96",
            "essence" or "core" => "rb_icon_essence_96",
            "stamina" => "rb_icon_currency_energy_96",
            "chest" => $"rb_icon_chest_{key}_96",
            "passxp" => "rb_icon_pass_xp_96",
            "shard" => "rb_icon_shard_96",
            _ => "rb_icon_reward_chest_96",
        });

        public static string LabelOf(string kind, string key) => kind switch
        {
            "gem" => Localize.FromTable("ui.rb.shop.r.gem", "젬"),
            "gold" or "goldrun" => Localize.FromTable("ui.rb.shop.r.gold", "골드"),
            "ticket" => Localize.FromTable("ui.store.r.ticket", "소환권"),
            "essence" or "core" => Localize.FromTable("ui.rb.shop.r.essence", "영혼 정수"),
            "stamina" => Localize.FromTable("ui.rb.shop.r.stamina", "행동력"),
            "chest" => Localize.Get($"store.chest_{key}.name"),
            "passxp" => Localize.FromTable("ui.rb.shop.r.passxp", "패스 경험치"),
            "shard" => Localize.FromTable("ui.rb.shop.r.shard", "조각"),
            _ => key ?? kind,
        };

        public static string Received(RewardResult r)
        {
            var sb = new StringBuilder(Localize.FromTable("ui.rb.shop.received", "받았어요"));
            if (r == null) return sb.ToString();
            sb.Append('\n');
            for (int i = 0; i < r.Lines.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(LabelOf(r.Lines[i].Kind.ToString().ToLowerInvariant(), r.Lines[i].Key)).Append(' ').Append(r.Lines[i].Amount.ToString("N0"));
            }
            return sb.ToString();
        }

        public static Sprite Find(Sprite[] list, string name)
        {
            if (list == null) return null;
            for (int i = 0; i < list.Length; i++) if (list[i] != null && list[i].name == name) return list[i];
            return null;
        }
    }
}
