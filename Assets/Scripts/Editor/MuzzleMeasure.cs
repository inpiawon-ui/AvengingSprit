using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Module.InGame;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// 캐릭터 그림에서 **총구 위치를 재서** <c>MuzzleTable.cs</c> 를 다시 쓴다.
    ///
    /// 예전에는 람보 하나를 손으로 재서 나온 값을 23종 전부에 썼다. 그런데 총구는
    /// 캐릭터마다 다른 데 있다 — 람보 <c>se</c> 는 몸 폭의 0.47 만큼 옆이었는데
    /// 코만도(기관총)는 0.24 였다. **두 배 밖에서 탄이 나왔다.**
    ///
    /// 재는 법: <c>atk1</c>(발사 프레임)에서 idle 에 없던 **밝은 픽셀**을 모아
    /// 그 무게중심을 잡는다. 그게 총구 화염이다.
    ///
    /// ⚠ 근접 캐릭터도 같이 재진다. 무기 반짝임을 화염으로 잘못 잡을 수 있지만,
    ///   그쪽 값은 탄을 안 쏘므로 쓰이지 않는다.
    /// </summary>
    public static class MuzzleMeasure
    {
        private const string UnitRoot = "Assets/BaseResource/Unit";
        private const string OutPath = "Assets/Scripts/Module/InGame/MuzzleTable.cs";

        /// <summary>화염으로 칠 밝기 문턱. RGB 합이 이보다 크면 흰빛으로 본다.</summary>
        private const int BrightSum = 620;

        [MenuItem("Tools/Game/총구 위치 다시 재기")]
        public static void Measure()
        {
            var rows = new List<string>();
            int done = 0, skipped = 0;

            foreach (var dir in Directory.GetDirectories(UnitRoot))
            {
                string key = Path.GetFileName(dir);
                var set = MeasureOne(dir, key);
                if (set == null) { skipped++; continue; }

                var sb = new StringBuilder();
                for (int i = 0; i < set.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append($"new({set[i].x:+0.000;-0.000}f, {set[i].y:+0.000;-0.000}f)");
                }
                rows.Add($"            [\"{key}\"] = new Vector2[] {{ {sb} }},");
                done++;
            }

            File.WriteAllText(OutPath, Head + string.Join("\n", rows) + "\n" + Tail, Encoding.UTF8);
            AssetDatabase.ImportAsset(OutPath);
            Debug.Log($"[총구] {done}종 측정 · {skipped}종 건너뜀(atk1 없음) → {OutPath}");
        }

        /// <summary>한 캐릭터의 방향 5개. 하나라도 못 재면 null — 반쪽은 안 쓴다.</summary>
        private static Vector2[] MeasureOne(string dir, string key)
        {
            var set = new Vector2[Unit.FacingSuffix.Length];
            for (int i = 0; i < set.Length; i++)
            {
                string face = Unit.FacingSuffix[i];
                var fire = Load($"{dir}/unit_{key}_{face}_atk1.png");
                var idle = Load($"{dir}/unit_{key}_{face}.png");
                if (fire == null || idle == null) return null;
                if (fire.width != idle.width || fire.height != idle.height) return null;

                int w = fire.width, h = fire.height;
                double sx = 0, sy = 0;
                int n = 0;
                var a = fire.GetPixels32();
                var b = idle.GetPixels32();
                for (int p = 0; p < a.Length; p++)
                {
                    if (a[p].a == 0) continue;
                    if (a[p].r + a[p].g + a[p].b < BrightSum) continue;
                    if (a[p].r == b[p].r && a[p].g == b[p].g && a[p].b == b[p].b && a[p].a == b[p].a)
                        continue;   // idle 에도 있던 밝은 곳 — 화염이 아니다

                    int x = p % w;
                    // GetPixels32 는 아래에서 위로 담긴다. 우리 좌표는 위가 +y 라 그대로 쓴다.
                    int yUp = p / w;
                    sx += x; sy += yUp;
                    n++;
                }
                if (n == 0) return null;

                set[i] = new Vector2((float)(sx / n - w / 2.0) / w,
                                     (float)(sy / n - h / 2.0) / h);
            }
            return set;
        }

        private static Texture2D Load(string path)
        {
            if (!File.Exists(path)) return null;
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null && !ti.isReadable)
            {
                ti.isReadable = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private const string Head =
@"using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 캐릭터별 **총구 위치**. 몸 중심 기준이고 캔버스 크기로 나눠 둬서
    /// 표시 크기가 달라도 따라간다. 순서는 <see cref=""Unit.FacingSuffix""/> 와 같다.
    ///
    /// ⚠ **이 파일은 손으로 고치지 않는다.** `Tools/Game/총구 위치 다시 재기` 가 다시 쓴다.
    ///   그림이 바뀌면 그 메뉴를 돌리면 된다.
    /// </summary>
    public static class MuzzleTable
    {
        /// <summary>표에 없는 캐릭터가 쓰는 값. **몸 중심**이다.</summary>
        private static readonly Vector2[] Fallback =
        {
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
        };

        private static readonly Dictionary<string, Vector2[]> Table = new()
        {
";

        private const string Tail =
@"        };

        /// <summary>
        /// <paramref name=""key""/> 의 <paramref name=""facingIndex""/> 방향 총구 오프셋.
        /// 몸 중심 기준 비율이다 — 부르는 쪽이 표시 크기를 곱한다.
        /// </summary>
        public static Vector2 Get(string key, int facingIndex)
        {
            if (facingIndex < 0 || facingIndex >= Unit.FacingSuffix.Length) return Vector2.zero;
            if (key != null && Table.TryGetValue(key, out var set)) return set[facingIndex];
            return Fallback[facingIndex];
        }
    }
}
";
    }
}
