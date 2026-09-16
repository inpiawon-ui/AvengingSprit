using System.IO;
using Game.Module.Common.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 그림을 프리팹 칸에 꽂는다 (2026-09-16 개편).
    ///
    /// 레이아웃(`UILayoutApplier`)은 **자리**만 잡는다. 어느 칸에 무슨 그림이 들어가는지는
    /// 여기가 단일 출처다 — 프리팹을 손으로 고치면 다음 레이아웃 적용 때 어디가
    /// 달라졌는지 알 길이 없다.
    ///
    /// 돌리는 순서: `Apply Mockup Layout` → **이것** → `ScreenFit 달기`
    /// </summary>
    public static class LobbyArtBinder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Res = "Assets/BaseResource/LobbyMainUI";
        private const string InDir = "Projects/AVSR/_exchange/in";

        /// <summary>납품 폴더에서 끌어올 그림과 9-slice 보더 (Vector4 = 왼·아래·오른·위).</summary>
        private static readonly (string file, Vector4 border)[] Incoming =
        {
            ("panelframe", new Vector4(64, 64, 64, 64)),
            ("chestslotframe", new Vector4(64, 64, 64, 64)),
            ("buttonblue", new Vector4(64, 28, 64, 28)),
            ("buttongold", new Vector4(64, 26, 64, 26)),
            ("chestreadybanner", Vector4.zero),
            ("chest_wood", Vector4.zero),
            ("chest_silver", Vector4.zero),
            ("chest_gold", Vector4.zero),
            ("chest_magic", Vector4.zero),
            ("hostbuttonart", Vector4.zero),
            ("chapterbuttonart", Vector4.zero),
            ("shopbuttonart", Vector4.zero),
            ("ghostsearchicon", Vector4.zero),
            ("chesttimeicon", Vector4.zero),
            ("modecentericon", Vector4.zero),
            ("ghostsearchart", Vector4.zero),
            ("lobbybackground", Vector4.zero),
            // 겹쳐 얹는 테두리 — 속이 비어 있다
            ("hudpill", new Vector4(72, 26, 72, 26)),
            ("actionframe_blue", new Vector4(72, 48, 72, 48)),
            ("actionframe_gold", new Vector4(72, 48, 72, 48)),
            ("modecardframe_center", new Vector4(72, 72, 72, 72)),
            ("modecardframe_side", Vector4.zero),
            // 속이 찬 모드 카드 판 — 테두리·속·글자판이 한 장에 다 있다
            ("modecard_side", Vector4.zero),
            ("modecard_side_l", Vector4.zero),
            ("modecard_center", Vector4.zero),
            ("ghostsearchbig", Vector4.zero),
            ("goldicon", Vector4.zero),
            ("gemicon", Vector4.zero),
            ("plusbutton", Vector4.zero),
            ("mailbutton", Vector4.zero),
            ("settingsbutton", Vector4.zero),
            ("notifybadge", Vector4.zero),
            // 좌우 모드 칸은 기운 방향이 반대다 — 납품본을 뒤집어 만들어 쓴다
            ("modecardframe_side_l", Vector4.zero),
        };

        /// <summary>「노드 이름 : 그림 파일」. 칸 안쪽 노드는 `칸/노드` 로 적는다.</summary>
        private static readonly (string node, string file)[] Bind =
        {
            ("LobbyBackground", "lobbybackground"),

            // 상단 HUD — 재화 칸은 테두리만 얹고 속은 배경이 비친다
            ("SettingsButton", "settingsbutton"),
            ("GoldCounter", "hudpill"),
            ("GoldIcon", "goldicon"),
            ("GemCounter", "hudpill"),
            ("GemIcon", "gemicon"),
            ("MailButton", "mailbutton"),

            // 유령 수색 — 그림 위에 **속 빈** 테두리를 얹는다
            ("GhostSearchArt", "ghostsearchart"),
            ("GhostSearchFrame", "panelframe"),
            ("GhostSearchIcon", "ghostsearchicon"),
            ("GhostSearchGoldIcon", "goldicon"),
            ("GhostSearchClaimIcon", "chapterbuttonart"),
            ("GhostSearchClaimButton", "buttongold"),
            ("GhostSearchHelpButton", "buttonblue"),

            ("ModeCenterIcon", "modecentericon"),
            ("ModeCardCenter", "modecard_center"),
            ("GhostSearchBigGhost", "ghostsearchbig"),

            // 하단 바 — 칸 테두리는 속을 비우고 그림·글자가 그 안에 보인다
            ("HostButton", "actionframe_blue"),
            ("HostButtonArt", "hostbuttonart"),
            ("ChapterButton", "actionframe_gold"),
            ("ChapterButtonArt", "chapterbuttonart"),
            ("ShopButton", "actionframe_blue"),
            ("ShopButtonArt", "shopbuttonart"),
        };

        /// <summary>같은 이름이 여러 곳에 있는 것 — 전부에 꽂는다.</summary>
        private static readonly (string node, string file)[] BindAll =
        {
            ("NotifyBadge", "notifybadge"),
            ("PlusButton", "plusbutton"),
        };

        /// <summary>상자 세 칸 안쪽은 이름이 같다 — 칸을 훑으며 같은 것을 꽂는다.</summary>
        private static readonly (string node, string file)[] SlotBind =
        {
            ("ChestSlotFrame", "chestslotframe"),
            ("ChestReadyBanner", "chestreadybanner"),
            ("ChestActionButton", "buttonblue"),
            ("ChestActionGemIcon", "gemicon"),
            ("ChestTimeIcon", "chesttimeicon"),
        };

        private static readonly string[] ChestKeys = { "wood", "silver", "gold", "magic" };

        [MenuItem("Tools/Game/로비 그림 꽂기")]
        public static void Run()
        {
            int pulled = ImportIncoming();

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            if (root == null) { Debug.LogError($"[로비] 프리팹 로드 실패: {Prefab}"); return; }

            int bound = 0, missing = 0;
            foreach (var (node, file) in Bind)
                if (Put(root.transform, node, file)) bound++; else missing++;

            for (int i = 1; i <= 3; i++)
            {
                var slot = Find(root.transform, $"ChestSlot{i}");
                if (slot == null) { missing++; continue; }
                foreach (var (node, file) in SlotBind)
                    if (Put(slot, node, file)) bound++; else missing++;
            }

            // 두 칸 모두 **가운데를 향해** 기운다. 납품본은 왼쪽 변이 긴 모양이라
            // 그대로가 오른쪽 칸이고, 왼쪽 칸은 좌우를 뒤집은 그림을 쓴다.
            //
            // ⚠ `localScale.x = -1` 로 뒤집지 마라. 적용기가 노드를 **왼쪽 위 pivot** 으로
            //   정규화하므로, 뒤집으면 칸 왼쪽 바깥으로 통째로 밀려난다(2026-09-16 실제로 그랬다).
            foreach (var (card, file) in new[]
                     { ("ModeCardLeft", "modecard_side_l"), ("ModeCardRight", "modecard_side") })
                if (Put(root.transform, card, file)) bound++; else missing++;

            // ⚠ 코드가 나중에 채우는 칸은 **비워 둔다.** 그림 없이 색만 남으면
            //   시커먼 네모가 그려져 「상자가 안 보인다」로 읽힌다(2026-09-16).
            foreach (var node in new[] { "ChestArt", "ModeCardArt" })
            {
                var hits = new System.Collections.Generic.List<Transform>();
                Collect(root.transform, node, hits);
                foreach (var t in hits)
                {
                    var img = t.GetComponent<Image>();
                    if (img != null && img.sprite == null) img.enabled = false;
                }
            }

            foreach (var (node, file) in BindAll)
            {
                var hits = new System.Collections.Generic.List<Transform>();
                Collect(root.transform, node, hits);
                foreach (var t in hits) if (Put(t, node, file)) bound++; else missing++;
            }

            // 등급별 상자 그림은 코드가 갈아 끼운다 — 짝만 여기서 채워 준다
            var ui = root.GetComponent<Game.Module.Lobby.LobbyMainUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                SetSprite(so, "_chestButtonBlue", "buttonblue");
                SetSprite(so, "_chestButtonGold", "buttongold");

                var arts = so.FindProperty("_chestArts");
                arts.arraySize = ChestKeys.Length;
                for (int i = 0; i < ChestKeys.Length; i++)
                {
                    var e = arts.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("Key").stringValue = ChestKeys[i];
                    e.FindPropertyRelative("Sprite").objectReferenceValue = Sprite($"chest_{ChestKeys[i]}");
                }

                var modes = so.FindProperty("_modeArts");
                modes.arraySize = 3;
                modes.GetArrayElementAtIndex(0).objectReferenceValue = Sprite("modeart_survival");
                modes.GetArrayElementAtIndex(1).objectReferenceValue = Sprite("modeart_scenario");
                modes.GetArrayElementAtIndex(2).objectReferenceValue = Sprite("modeart_defense");

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            int fonts = ApplyGothic(root.transform);

            // 호스트 선택 판은 로비 **위에** 떠야 한다
            var hsp = root.transform.Find("HostSelectPanel");
            if (hsp != null) hsp.SetAsLastSibling();

            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();

            Debug.Log($"[로비] 납품 반영 {pulled}장 · 그림 꽂기 {bound}개 · 폰트 {fonts}칸"
                      + (missing > 0 ? $" · 못 찾은 것 {missing}개" : ""));
        }

        // ── 폰트 ─────────────────────────────────────────────────

        private const string GothicFont = "Assets/BaseResource/Fonts/NotoSansKR SDF.asset";

        /// <summary>
        /// 로비 글자를 **굵은 고딕**으로 맞춘다 (기획 2026-09-16 「목업처럼」).
        ///
        /// ⚠ TMP 는 폰트를 안 지정하면 `TMP_Settings.defaultFontAsset` 인 **픽셀 폰트**를 쓴다.
        ///   한글·일본어는 대체 폰트로 넘어가 고딕으로 나오지만 **라틴·숫자는 픽셀로 남아**
        ///   「4,288」 「HOST」 만 결이 달랐다. 여기서 한 번 고딕으로 못 박는다.
        ///
        /// 언어를 바꾸면 `LanguageModule.SwapFont` 가 한국어↔일본어 폰트를 갈아 끼운다 —
        /// 그 대상이 되려면 표에 있는 고딕이어야 해서 픽셀 폰트로 두면 안 된다.
        ///
        /// 굵기는 **가짜 굵게**(`FontStyles.Bold`)다. SDF 라 번지지 않고, 진짜 Bold 자형을
        /// 쓰려면 아틀라스를 새로 구워야 한다 — 지금 결로 충분하다.
        /// </summary>
        private static int ApplyGothic(Transform root)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(GothicFont);
            if (font == null) { Debug.LogWarning($"[로비] 고딕 폰트 없음: {GothicFont}"); return 0; }

            int n = 0;
            foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                // 호스트 선택 판은 제 화면이다 — 로비가 손대지 않는다
                if (IsUnder(t.transform, "HostSelectPanel")) continue;
                if (t.font != font) { t.font = font; t.fontSharedMaterial = font.material; }
                if ((t.fontStyle & TMPro.FontStyles.Bold) == 0) t.fontStyle |= TMPro.FontStyles.Bold;
                n++;
            }
            return n;
        }

        // ⚠ 자동 축소는 **여기서 건드리지 마라.** `UILayoutApplier.ApplyText` 가 표의
        //   `size` 를 기준으로 이미 잡아 준다. 여기서 `t.fontSize` 를 다시 읽어 기준으로 삼으면
        //   **이미 줄어든 값**을 새 최대치로 삼아, 도구를 돌릴 때마다 글자가 작아지다
        //   사라진다(2026-09-16 실제로 재화 숫자가 통째로 없어졌다).

        private static bool IsUnder(Transform t, string ancestor)
        {
            for (var p = t; p != null; p = p.parent)
                if (p.name == ancestor) return true;
            return false;
        }

        // ── 납품 끌어오기 ────────────────────────────────────────

        /// <summary>
        /// 여백을 잘라 낸 뒤의 9-slice 보더. `Projects/AVSR` 의 잘라내기 도구가 적어 둔다.
        ///
        /// ⚠ 납품 그림에 투명 여백이 남으면 박스를 목업대로 잡아도 **그만큼 작게** 그려진다.
        ///   여백을 잘라 「박스 = 그림」으로 만들고, 잘라 낸 만큼 보더도 줄인 값이 여기다.
        /// </summary>
        private const string BorderTable = "Assets/Scripts/Editor/UISpec/_lobby_borders.json";

        private static System.Collections.Generic.Dictionary<string, Vector4> LoadBorders()
        {
            var map = new System.Collections.Generic.Dictionary<string, Vector4>();
            if (!File.Exists(BorderTable)) return map;
            // 작은 표라 간단히 읽는다 — `"name": [l, b, r, t]`
            foreach (var line in File.ReadAllLines(BorderTable))
            {
                int q1 = line.IndexOf('"');
                int q2 = q1 < 0 ? -1 : line.IndexOf('"', q1 + 1);
                int lb = line.IndexOf('[');
                int rb = line.IndexOf(']');
                if (q1 < 0 || q2 < 0 || lb < 0 || rb < 0) continue;
                var nums = line.Substring(lb + 1, rb - lb - 1).Split(',');
                if (nums.Length != 4) continue;
                map[line.Substring(q1 + 1, q2 - q1 - 1)] = new Vector4(
                    float.Parse(nums[0]), float.Parse(nums[1]),
                    float.Parse(nums[2]), float.Parse(nums[3]));
            }
            return map;
        }

        private static int ImportIncoming()
        {
            int n = 0;
            foreach (var (file, _) in Incoming)
            {
                var src = $"{InDir}/{file}.png";
                if (!File.Exists(src)) continue;
                File.Copy(src, $"{Res}/{file}.png", true);
                n++;
            }
            if (n > 0) AssetDatabase.Refresh();

            var trimmed = LoadBorders();
            foreach (var (file, rawBorder) in Incoming)
            {
                var border = trimmed.TryGetValue(file, out var b) ? b : rawBorder;
                var dst = $"{Res}/{file}.png";
                if (AssetImporter.GetAtPath(dst) is not TextureImporter ti) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.spritePixelsPerUnit = 100f;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.spriteBorder = border;
                ti.SaveAndReimport();
            }
            return n;
        }

        // ── 꽂기 ─────────────────────────────────────────────────

        private static bool Put(Transform scope, string node, string file)
        {
            var t = Find(scope, node);
            if (t == null) { Debug.LogWarning($"[로비] 노드 없음: {node}"); return false; }
            var img = t.GetComponent<Image>();
            if (img == null) { Debug.LogWarning($"[로비] Image 없음: {node}"); return false; }
            var sprite = Sprite(file);
            if (sprite == null) { Debug.LogWarning($"[로비] 그림 없음: {file}"); return false; }

            img.sprite = sprite;
            img.color = Color.white;
            // 9-slice 로 들어온 것만 늘려 쓴다. 낱장은 비율이 눌리지 않게 Simple 이다.
            img.type = sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            if (img.type == Image.Type.Sliced) FitBorder(img, sprite);
            return true;
        }

        /// <summary>
        /// 9-slice 테두리가 **그려지는 칸보다 크면** 가운데가 사라져 한 줄로 뭉갠다.
        ///
        /// ⚠ 2026-09-16 에 실제로 그랬다 — 버튼 그림은 128×96 인데 화면에선 176×54 로
        ///   그려진다. 세로 테두리 28+28 = 56 이 높이 54 를 넘어 **금색 버튼이 통째로
        ///   가는 선 하나로 보였다.** 그림을 다시 그릴 일이 아니라 배율로 줄이면 된다.
        ///
        /// `pixelsPerUnitMultiplier` 를 올리면 테두리가 그만큼 작게 그려진다.
        /// 칸의 40 % 안에 들어오게 맞춘다 — 가운데가 충분히 남아야 늘어난 티가 안 난다.
        /// </summary>
        /// <summary>테두리가 칸에서 차지해도 되는 최대 비율. 목업 테두리는 이보다 훨씬 얇다.</summary>
        private const float BorderShare = 0.22f;

        private static void FitBorder(Image img, Sprite sprite)
        {
            var rect = ((RectTransform)img.transform).rect;
            float need = 1f;
            float h = sprite.border.y + sprite.border.w;   // 아래 + 위
            float w = sprite.border.x + sprite.border.z;   // 왼 + 오른
            if (rect.height > 1f && h > rect.height * BorderShare)
                need = Mathf.Max(need, h / (rect.height * BorderShare));
            if (rect.width > 1f && w > rect.width * BorderShare)
                need = Mathf.Max(need, w / (rect.width * BorderShare));
            img.pixelsPerUnitMultiplier = need;
        }

        private static void SetSprite(SerializedObject so, string field, string file)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = Sprite(file);
        }

        private static Sprite Sprite(string file)
            => AssetDatabase.LoadAssetAtPath<Sprite>($"{Res}/{file}.png");

        private static void Collect(Transform root, string name, System.Collections.Generic.List<Transform> into)
        {
            if (root.name == name) into.Add(root);
            for (int i = 0; i < root.childCount; i++) Collect(root.GetChild(i), name, into);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
