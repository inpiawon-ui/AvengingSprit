using System;
using Cysharp.Threading.Tasks;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 챕터 클리어 결과 (기획 2026-09-18 · 시안 ui_new_chapter_result_v1).
    ///
    /// 클리어했을 때만 뜬다 — 죽으면 보상이 없어 알림창 하나로 끝난다.
    /// 골드와 상자는 **전투가 이미 넣었다.** 여기는 보여 주고 OK 로 로비에 보낼 뿐이다.
    /// 상자 칸이 가득 차 상자를 못 받았으면 빨간 띠로 알린다(골드는 그래도 받았다).
    ///
    /// 노드는 `ChapterScreensBuilder` 가 세운다. 그림은 발주 부품이 오면 같은 이름으로 갈아 끼운다.
    /// </summary>
    public sealed class ChapterResultPopup : MonoBehaviour
    {
        [Serializable]
        private struct ChestArt
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private ChestArt[] _chestArts = Array.Empty<ChestArt>();

        private UIBinder _ui;
        private bool _leaving;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("ResultOkButton", OnOk);
        }

        public void Show(StageFinishedEvent e)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로

            // ⚠ 제목은 **글자**다. 챕터마다 그림으로 받으면 챕터가 늘 때마다 발주가 붙는다
            //   (2026-09-18 지적 「100챕터 나오면 일일이 리소스로 만들 거냐」). 숫자만 바뀐다.
            int ch = Mathf.Max(1, e.FinishedChapter);
            _ui.SetText("ResultTitleText", $"CHAPTER {ch} CLEAR");

            _ui.SetText("ResultSubText", Localize.Get($"stage.{ch}.1.name"));
            _ui.SetText("ResultGoldLabelText", Localize.Get("ui.result.gold_label"));
            _ui.SetText("ResultGoldValueText", "+0");

            // 상자 — 칸이 가득 차 못 받았으면 상자 줄을 아예 빼고 그 자리에 「받지 못했다」 경고만 둔다.
            // 흐리게라도 상자를 보여 주면 받은 것처럼 읽혔다(PD 2026-10-08 「가득 차 있으면 상자를 주면 안 되는데」)
            _ui.SetActive("ResultChestRow", e.ChestAccepted);
            if (e.ChestAccepted)
            {
                var chest = _ui.Get<Image>("ResultChestArt");
                var art = ChestArtOf(e.RewardChestKey);
                if (chest != null && art != null) chest.sprite = art;
                if (chest != null) chest.color = Color.white;
                _ui.SetText("ResultChestNameText", Localize.Get($"chest.{e.RewardChestKey}.name"));
            }
            _ui.SetActive("ResultWarnBar", !e.ChestAccepted);
            // 짧은 문구 — 긴 「받지 못했습니다」는 일본어 · 영어가 띠(346px)를 넘어 경고 아이콘을 덮었다(2026-10-08 게임 검사).
            // 상자 줄이 아예 빠져 있으니 「가득 찼다」만으로 못 받았다는 것이 읽힌다
            _ui.SetText("ResultWarnText", Localize.Get("ui.lobby.chest.full"));

            // 아래 경고 띠 자리가 비므로 두 줄을 띠 몫의 절반만큼 내려 위아래를 고른다. 경고는 상자 줄 자리 가운데에
            ShiftRow("ResultGoldRow", ref _goldRowY, RowDropWithoutWarn);
            ShiftRow("ResultChestRow", ref _chestRowY, RowDropWithoutWarn);
            if (!e.ChestAccepted && _chestRowY.HasValue && _ui.Find("ResultWarnBar") is RectTransform warn
                && _ui.Find("ResultChestRow") is RectTransform row)
            {
                warn.anchoredPosition = new Vector2(warn.anchoredPosition.x,
                    row.anchoredPosition.y - (row.rect.height - warn.rect.height) * 0.5f);
            }
            // 모은 조각 — 노드(`ResultShardText`)는 결과 창을 다시 짤 때 들어온다. 없으면 조용히 넘어간다
            _ui.SetText("ResultShardText", InGameMainUI.ShardLine(e).TrimStart('\n'));
            _ui.SetText("ResultOkText", "OK");

            // 연출(시안 mock_fxstory_clear_v2, PD 통과 2026-10-08) — 제목 뒤 빛살 · 금화 · 상자 착지 · OK 숨쉬기 · 원혼 입자
            // 좌표는 가운데 720x1280 판(ResultContent) 기준이라 거기에 띄운다(태블릿에서도 틀과 같이 움직인다)
            var fx = GetComponentInParent<PopupFxPlayer>();
            if (fx != null) fx.Open((RectTransform)transform, _ui.Find("ResultContent") as RectTransform);
            RevealAsync(e, fx, ++_countId).Forget();   // fire-and-forget: 등장 · 금화 · 상자 연출은 제 시간에 끝난다
        }

        // ── 등장 · 금화 · 상자 (PD 2026-10-08 반려 뒤 다시 짬) ─────────────────
        //
        //   0.00 창이 OutBack 곡선으로 살짝 커졌다 제자리(0.86 → 1)
        //   1.55 위에서 금화가 떨어지기 시작 — 금화 더미 그림 자체가 없다가 네 단계로 쌓인다(그림이 바뀔 때마다 톡 튐), 숫자 0 → 금액
        //   2.55 상자가 위에서 떨어져 바닥에 통통 튀며 놓인다(OutBounce) — 착지 2.95 에 먼지 고리(연출 표 clear_chest_land)
        //        상자 칸이 가득 찼으면 상자 대신 경고 띠가 그 자리에 나타난다
        // 금화 · 먼지 시각은 연출 표(fx_story.py layers_clear)와 같다 — 한쪽을 바꾸면 같이 바꾼다.

        private const float EnterSeconds = 0.38f;
        private const float GoldStart = 1.55f;
        private const float GoldEnd = 2.35f;
        private const float ChestDrop = 2.55f;
        private const float ChestFall = 0.4f;
        private const float ChestFallHeight = 90f;
        private int _countId;
        private Vector2? _chestArtHome;

        private async UniTaskVoid RevealAsync(StageFinishedEvent e, PopupFxPlayer fx, int id)
        {
            var token = this.GetCancellationTokenOnDestroy();
            var content = _ui.Find("ResultContent") as RectTransform;
            var contentGroup = Group(content);
            var goldIcon = _ui.Get<Image>("ResultGoldIcon");
            var finalPile = goldIcon != null ? goldIcon.sprite : null;
            var stages = fx != null ? fx.FramesOf("present_goldpile_stages") : null;
            var chest = _ui.Find("ResultChestArt") as RectTransform;
            var chestName = Group(_ui.Find("ResultChestNameText"));
            var chestGroup = Group(chest);
            var warnGroup = Group(_ui.Find("ResultWarnBar"));
            if (chest != null) _chestArtHome ??= chest.anchoredPosition;

            // 처음 모습 — 금화 더미 · 상자 · 경고는 아직 없다
            if (goldIcon != null) goldIcon.color = new Color(1f, 1f, 1f, 0f);
            if (chestGroup != null) chestGroup.alpha = 0f;
            if (chestName != null) chestName.alpha = 0f;
            if (warnGroup != null) warnGroup.alpha = 0f;

            int shown = -1;
            float t = 0f;
            while (t < ChestDrop + ChestFall + 0.4f)
            {
                if (this == null || id != _countId) return;
                t += Time.unscaledDeltaTime;

                // 창 등장
                float ek = Mathf.Clamp01(t / EnterSeconds);
                if (content != null) content.localScale = Vector3.one * Mathf.LerpUnclamped(0.86f, 1f, Ease.OutBack(ek));
                if (contentGroup != null) contentGroup.alpha = Ease.OutCubic(Mathf.Clamp01(t / (EnterSeconds * 0.6f)));

                // 금화 — 숫자는 곡선으로 오르고, 더미 그림은 네 단계로 쌓인다
                float gk = Mathf.Clamp01((t - GoldStart) / (GoldEnd - GoldStart));
                _ui.SetText("ResultGoldValueText", $"+{Mathf.RoundToInt(e.RewardGold * Ease.OutQuad(gk)):N0}");
                int stage = gk <= 0.1f ? -1 : gk < 0.4f ? 0 : gk < 0.7f ? 1 : gk < 1f ? 2 : 3;
                if (stage != shown && goldIcon != null)
                {
                    shown = stage;
                    if (stage >= 0)
                    {
                        goldIcon.sprite = stage == 3 || stages == null || stages.Length < 4 ? finalPile : stages[stage];
                        goldIcon.color = Color.white;
                        PunchAsync(goldIcon.transform, 1.14f, 0.2f, id).Forget();   // fire-and-forget: 짧은 튐
                    }
                }

                // 상자(받았으면) — 위에서 떨어져 통통 튀며 놓인다 / 못 받았으면 경고 띠가 나타난다
                float ck = Mathf.Clamp01((t - ChestDrop) / ChestFall);
                if (e.ChestAccepted && chest != null && _chestArtHome.HasValue && t >= ChestDrop)
                {
                    chest.anchoredPosition = _chestArtHome.Value + new Vector2(0f, ChestFallHeight * (1f - Ease.OutBounce(ck)));
                    if (chestGroup != null) chestGroup.alpha = Mathf.Clamp01(ck * 4f);
                    if (chestName != null) chestName.alpha = Ease.OutCubic(Mathf.Clamp01((t - ChestDrop - ChestFall) / 0.25f));
                }
                if (!e.ChestAccepted && warnGroup != null) warnGroup.alpha = Ease.OutCubic(ck);

                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (this == null || id != _countId) return;
            if (content != null) content.localScale = Vector3.one;
            if (contentGroup != null) contentGroup.alpha = 1f;
            _ui.SetText("ResultGoldValueText", $"+{e.RewardGold:N0}");
            if (goldIcon != null) { goldIcon.sprite = finalPile; goldIcon.color = Color.white; }
            if (chest != null && _chestArtHome.HasValue) chest.anchoredPosition = _chestArtHome.Value;
        }

        private async UniTaskVoid PunchAsync(Transform t, float from, float seconds, int id)
        {
            var token = this.GetCancellationTokenOnDestroy();
            for (float p = 0f; p < seconds; p += Time.unscaledDeltaTime)
            {
                if (this == null || t == null || id != _countId) return;
                t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, Ease.OutBack(p / seconds));
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        private static CanvasGroup Group(Transform t)
        {
            if (t == null) return null;
            var g = t.GetComponent<CanvasGroup>();
            return g != null ? g : t.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>경고 띠(46) + 간격이 빠질 때 두 줄을 내리는 거리 — 그 절반.</summary>
        private const float RowDropWithoutWarn = 28f;

        // 빌더가 세운 자리. 처음 한 번 기억해 두고 거기서부터 옮긴다 — 창을 다시 띄워도 안 밀리게.
        private float? _goldRowY, _chestRowY;

        private void ShiftRow(string node, ref float? baseY, float drop)
        {
            if (_ui.Find(node) is not RectTransform row) return;
            baseY ??= row.anchoredPosition.y;
            row.anchoredPosition = new Vector2(row.anchoredPosition.x, baseY.Value - drop);
        }

        private Sprite ChestArtOf(string key)
        {
            for (int i = 0; i < _chestArts.Length; i++)
                if (_chestArts[i].Key == key) return _chestArts[i].Sprite;
            return null;
        }

        private void OnOk()
        {
            if (_leaving) return;   // 두 번 눌러 씬을 두 번 부르지 않게
            _leaving = true;
            // OK 테두리가 짧게 한 번 밝아진다(시안 3컷) — 로딩 덮개가 내려오는 동안 보인다
            var fx = GetComponentInParent<PopupFxPlayer>();
            if (fx != null) fx.Fire((RectTransform)transform);
            GoLobbyAsync().Forget();   // fire-and-forget: 씬 전환 대기 불필요
        }

        private static async UniTaskVoid GoLobbyAsync()
        {
            await CoreModule.Get<ISceneManager>().LoadAsync(new SceneLoadRequest
            {
                SceneName = SceneNames.Lobby,
                LoadingStyle = LoadingStyle.Overlay,
            });
        }
    }
}
