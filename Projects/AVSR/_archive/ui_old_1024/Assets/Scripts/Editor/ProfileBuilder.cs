using System.Collections.Generic;
using System.IO;
using Game.Module.Lobby;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.LobbyShopBuilder;

namespace Game.Editor
{
    /// <summary>프로필 꾸미기 팝업(`ProfilePopup`) — 시안 `mock_store_profile` × 0.765. 로비 왼쪽 위 프로필로 연다.</summary>
    public static class ProfileBuilder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Pf = "Assets/BaseResource/Profile";
        private const string St = "Assets/BaseResource/Store";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string Pa = "Assets/BaseResource/Pass";
        private const string Faces = "Assets/BaseResource/Growth/Faces";

        private static readonly Color Ink = new(0.16f, 0.08f, 0.03f, 1f);
        private const int Cells = 24;
        private const float CellW = 287f, CellH = 218f, StepX = 297f, StepY = 232f;

        [MenuItem("Tools/Game/로비 — 프로필 꾸미기 팝업 세우기")]
        public static void Run()
        {
            B($"{Pf}/profile_cell.png", 50); B($"{Pf}/profile_tab.png", 40); B($"{Pf}/title_ribbon.png", new Vector4(50, 20, 50, 20));
            var lobby = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                if (!UseTemplates(lobby.transform)) { Debug.LogError("[프로필] 글자 본이 없다"); return; }
                var old = lobby.transform.Find("ProfilePopup");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var panel = new GameObject("ProfilePopup", typeof(RectTransform)).GetComponent<RectTransform>();
                panel.gameObject.layer = lobby.layer;
                panel.SetParent(lobby.transform, false);
                Stretch(panel);
                var dim = panel.gameObject.AddComponent<Image>();
                dim.color = new Color(0f, 0f, 0.03f, 0.6f);
                dim.raycastTarget = true;
                var box = new GameObject("ProfileContent", typeof(RectTransform)).GetComponent<RectTransform>();
                box.gameObject.layer = lobby.layer;
                box.SetParent(panel, false);
                box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0.5f);
                box.sizeDelta = new Vector2(720, 1280);
                box.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();

                Sliced(box, "ProfilePlate", $"{St}/popup_plate_wide.png", 27, 130, 692, 1056, 2f);
                Title(box, "ProfileTitleText", 160, 166, 400, 52, 40, Color.white).alignment = TextAlignmentOptions.Center;
                var x = Pic(box, "ProfileClose", $"{St}/btn_close_x.png", 614, 143, 664, 193);
                x.raycastTarget = true;
                Button(x.gameObject);

                // 미리보기
                // 미리보기 배경 — 칸을 덮게 키워 깔고 칸(마스크)이 잘라 낸다(늘리면 찌그러진다)
                var pvMask = Node(box, "ProfilePreviewMask", 57, 200, 662, 367);
                pvMask.gameObject.AddComponent<RectMask2D>();
                var pv = Pic(pvMask, "ProfilePreviewBg", $"{Ch}/ch_art_4.png", 0, 0, 0, 0);
                StretchIn(pv.rectTransform);
                if (pv.sprite != null)
                {
                    var fit = pv.gameObject.AddComponent<AspectRatioFitter>();
                    fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fit.aspectRatio = pv.sprite.rect.width / pv.sprite.rect.height;
                }
                Sliced(box, "ProfilePreviewBox", $"{Ch}/ch_s22e_box.png", 57, 200, 662, 367, 2f).color = new Color(1f, 1f, 1f, 0.35f);
                Pic(box, "ProfilePreviewFace", null, 165, 230, 295, 350);
                Pic(box, "ProfilePreviewFrame", $"{Pf}/profile_frame_default.png", 150, 214, 310, 364);
                Title(box, "ProfilePreviewName", 329, 275, 300, 50, 40, Color.white).alignment = TextAlignmentOptions.Left;
                var rib = Sliced(box, "ProfilePreviewTitleBand", $"{Pf}/title_ribbon.png", 330, 306, 574, 344, 3f);
                var rt = Title(rib.transform, "ProfilePreviewTitle", 0, 0, 0, 0, 22, Color.white);
                Local(rt.rectTransform, 0, 0, 220, 30);

                // 탭
                Tab(box, "ProfileTabFrame", "ProfileTabFrameText", $"{Pf}/profile_frame_default.png", 54, 386, 249, 448);
                Tab(box, "ProfileTabFace", "ProfileTabFaceText", $"{Faces}/face_ninja.png", 253, 386, 466, 448);
                Tab(box, "ProfileTabTitle", "ProfileTabTitleText", "Assets/BaseResource/LobbyMainUI/Nav/icon_season_crown.png", 471, 386, 666, 448);

                // 칸 — 2열 스크롤
                var view = Node(box, "ProfileGrid", 64, 458, 656, 918);
                view.gameObject.AddComponent<RectMask2D>();
                var hit = view.gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                var scroll = view.gameObject.AddComponent<ScrollRect>();
                var content = new GameObject("ProfileGridContent", typeof(RectTransform)).GetComponent<RectTransform>();
                content.gameObject.layer = view.gameObject.layer;
                content.SetParent(view, false);
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.sizeDelta = new Vector2(0, 470);
                scroll.content = content;
                scroll.viewport = view;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                for (int i = 0; i < Cells; i++) Cell(content, i);

                Body(box, "ProfileNote", 140, 946, 440, 28, 19, Color.white).alignment = TextAlignmentOptions.Center;
                var eq = Sliced(box, "ProfileEquip", $"{St}/btn_confirm.png", 203, 970, 516, 1033, 3f);
                eq.raycastTarget = true;
                Button(eq.gameObject);
                var et = Title(eq.transform, "ProfileEquipText", 0, 0, 0, 0, 34, Ink);
                Local(et.rectTransform, 0, 0, 280, 46);
                Plain(et);

                var comp = panel.gameObject.AddComponent<ProfilePopup>();
                Bind(comp);
                var main = new SerializedObject(lobby.GetComponent<LobbyMainUI>());
                main.FindProperty("_profilePopup").objectReferenceValue = comp;
                main.ApplyModifiedPropertiesWithoutUndo();
                panel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(lobby); }
            Debug.Log("[프로필] 세움");
        }

        private static void Tab(Transform box, string name, string label, string icon, float x0, float y0, float x1, float y1)
        {
            var tab = Sliced(box, name, $"{St}/shop_tab.png", x0, y0, x1, y1, 2.2f);
            tab.raycastTarget = true;
            Button(tab.gameObject);
            var ic = Pic(tab.transform, "TabIcon", icon, 0, 0, 0, 0);
            Local(ic.rectTransform, -(x1 - x0) * 0.5f + 40, 0, 40, 36);
            var lb = Title(tab.transform, label, 0, 0, 0, 0, 22, Color.white);
            Local(lb.rectTransform, 22, 0, 130, 32);
        }

        private static void Cell(RectTransform content, int i)
        {
            var cell = new GameObject($"ProfileCell{i}", typeof(RectTransform)).GetComponent<RectTransform>();
            cell.gameObject.layer = content.gameObject.layer;
            cell.SetParent(content, false);
            cell.anchorMin = cell.anchorMax = new Vector2(0.5f, 1f);
            cell.anchoredPosition = new Vector2((i % 2 == 0 ? -1 : 1) * StepX * 0.5f, -(i / 2 * StepY + CellH * 0.5f + 4));
            cell.sizeDelta = new Vector2(CellW, CellH);
            var plate = Sliced(cell, "CellPlate", $"{Pf}/profile_cell.png", 0, 0, 0, 0, 2.2f);
            StretchIn(plate.rectTransform);
            plate.raycastTarget = true;
            Button(cell.gameObject);
            // 고른 칸 = 금 테만(시안). 카드용 덮개(ch_card_sel_overlay)는 모서리에 체크가 그려져 있어 체크가 두 개가 된다
            var sel = Sliced(cell, "CellSelected", "Assets/BaseResource/LobbyMainUI/Nav/nav_sel_rim.png", 0, 0, 0, 0, 1.6f);
            StretchIn(sel.rectTransform);
            sel.gameObject.SetActive(false);
            var n = Title(cell, "CellName", 0, 0, 0, 0, 22, Color.white);
            Local(n.rectTransform, 0, 82, CellW - 24, 30);
            var art = Pic(cell, "CellArt", null, 0, 0, 0, 0);
            Local(art.rectTransform, 0, 4, 120, 110);
            var band = Sliced(cell, "CellTitleBand", $"{Pf}/title_ribbon.png", 0, 0, 0, 0, 3f);
            Local(band.rectTransform, 0, 4, CellW - 40, 44);
            var bt = Title(band.transform, "CellTitleText", 0, 0, 0, 0, 20, Color.white);
            Local(bt.rectTransform, 0, 0, CellW - 70, 30);
            band.gameObject.SetActive(false);
            var lk = Pic(cell, "CellLock", $"{Ch}/ch_lock.png", 0, 0, 0, 0);
            Local(lk.rectTransform, 0, 4, 30, 38);
            lk.gameObject.SetActive(false);
            var chk = Pic(cell, "CellCheck", $"{Pa}/mark_claimed.png", 0, 0, 0, 0);
            Local(chk.rectTransform, 80, 50, 38, 38);
            chk.gameObject.SetActive(false);
            var st = Sliced(cell, "CellStatus", $"{St}/btn_small.png", 0, 0, 0, 0, 3.4f);
            Local(st.rectTransform, 0, -80, CellW - 60, 32);
            var stt = Body(st.transform, "CellStatusText", 0, 0, 0, 0, 16, new Color(0.6f, 1f, 0.7f));
            Local(stt.rectTransform, 0, 0, CellW - 76, 24);
        }

        private static void Bind(ProfilePopup p)
        {
            var so = new SerializedObject(p);
            var frames = new List<(string, Sprite)>
            {
                ("profile_frame_default", Spr($"{Pf}/profile_frame_default.png")), ("profile_frame_s1", Spr($"{Pf}/profile_frame_s1.png")),
                ("profile_frame_ch10", Spr($"{Pf}/profile_frame_ch10.png")),
            };
            var faces = new List<(string, Sprite)>();
            foreach (var f in Directory.GetFiles(Faces, "face_*.png"))
                faces.Add((Path.GetFileNameWithoutExtension(f).Substring(5), Spr(f.Replace('\\', '/'))));
            faces.Add(("ghost", Spr("Assets/BaseResource/Growth/Portraits/portrait_ghost.png")));
            Keyed(so, "_frames", frames);
            Keyed(so, "_faces", faces);
            so.FindProperty("_tab").objectReferenceValue = Spr($"{St}/shop_tab.png");
            so.FindProperty("_tabSelected").objectReferenceValue = Spr($"{St}/shop_tab_sel.png");
            so.FindProperty("_soon").objectReferenceValue = Spr("Assets/BaseResource/ChapterHost/mode_random_art.png");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Keyed(SerializedObject so, string field, List<(string key, Sprite sprite)> list)
        {
            var prop = so.FindProperty(field);
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Key").stringValue = list[i].key;
                e.FindPropertyRelative("Sprite").objectReferenceValue = list[i].sprite;
            }
        }
    }
}
