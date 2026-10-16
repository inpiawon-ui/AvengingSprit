using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 피해 수치 표시 — 던전앤파이터식 「꽂히는」 숫자 (PD 2026-10-16 「피격 데미지 연출을 던파랑 똑같이 · 뜨는 연출을 맛깔나게」).
    ///
    /// 레퍼런스(던파 상급 던전 솔플 영상, 1080p 프레임 단위로 잼 — 2026-10-16):
    ///   ① 숫자가 **크고 옅게(약 2.4배 · 35%)** 나타나 3프레임(0.1초) 만에 제 크기로 **쾅 꽂힌다**.
    ///   ② 꽂히는 동안 큰 잔상이 그 자리에 남아 옅어진다 — 충격파처럼 보인다.
    ///   ③ 꽂힌 뒤에는 거의 움직이지 않고 버틴다. 같은 적이 또 맞으면 새 숫자가 아래에 꽂히고 앞 숫자들이 한 줄씩 밀려 올라간다.
    ///   ④ 치명타는 붉고 더 크다. 사라질 때는 짧게 떠오르며 꺼진다.
    ///
    /// 그림: 숫자 글꼴 `dmg_digit_0~9 · plus · minus`(흰 → 회색 그라데이션 · 짙은 외곽선, 색은 곱해서 입힌다).
    /// 그림이 아직 없으면 예전처럼 TMP 글꼴로 그린다(같은 움직임).
    ///
    /// 탄과 같은 풀 방식이다. 교전 중 매 타격마다 GameObject 를 만들면 GC 가 튄다.
    /// </summary>
    public sealed class DamageText : MonoBehaviour
    {
        // ── 박자 (초) ───────────────────────────────────────────
        private const float SlamSeconds = 0.09f;      // 크고 옅게 → 제 크기 (레퍼런스 3프레임)
        private const float EchoSeconds = 0.22f;      // 큰 잔상이 남아 옅어지는 시간
        private const float HoldSeconds = 0.5f;       // 꽂힌 뒤 버티는 시간
        private const float CritHoldSeconds = 0.75f;
        private const float ExitSeconds = 0.2f;

        // ── 크기 (방 좌표 px — 방 화면은 약 0.47배로 보인다) ─────────
        private const float NormalHeight = 40f;
        private const float CritHeight = 56f;
        private const float MinorHeight = 24f;
        private const float DigitAspect = 0.8f;       // 칸 32x40
        private const float DigitOverlap = 0.14f;     // 기운 숫자끼리 살짝 겹친다

        private const float SlamFrom = 2.4f;
        private const float CritSlamFrom = 2.9f;
        private const float SlamAlpha = 0.35f;
        private const float EchoAlpha = 0.4f;
        private const float EchoGrow = 1.15f;         // 잔상은 꽂히는 동안 조금 더 커지며 사라진다
        private const float DropIn = 8f;              // 꽂힐 때 위에서 살짝 떨어져 박힌다
        private const float HoldDrift = 6f;           // 버티는 동안 아주 조금 떠오른다
        private const float ExitRise = 16f;

        /// <summary>맞은 자리보다 이만큼 위에서 뜬다 — 타격 이펙트(불꽃 · 섬광)에 숫자가 가려지지 않게.</summary>
        private const float StartLift = 26f;

        // ── 쌓이기 ──────────────────────────────────────────────
        private const float StackLine = 0.82f;        // 한 줄 높이 = 글자 높이 × 이것
        private const float StackLerp = 22f;          // 밀려 올라가는 빠르기(초당)
        public const int MaxStack = 4;                // 이보다 위로 밀리면 바로 꺼진다

        private const int MaxGlyphs = 10;

        // 숫자 그림 — 판이 그림을 받은 뒤 한 번 꽂는다(BattleDirector.SetDamageDigits). 순서 0~9, +, −
        private static Sprite[] s_digits;
        public static void SetDigits(Sprite[] digits) => s_digits = digits;
        private static bool HasDigits => s_digits != null && s_digits.Length >= 12 && s_digits[0] != null;

        private RectTransform _rect;
        private RectTransform _face, _echo;
        private readonly Image[] _faceGlyphs = new Image[MaxGlyphs];
        private readonly Image[] _echoGlyphs = new Image[MaxGlyphs];
        private int _glyphCount;
        private bool _glyphsBuilt;

        // TMP 대체(숫자 그림이 없을 때)
        private TextMeshProUGUI _tmp;
        private TextMeshProUGUI _shadow;
        private TextMeshProUGUI _echoTmp;
        private bool _useGlyphs;

        private Color _color;
        private bool _crit;
        private bool _minor;
        private float _age;
        private float _hold;
        private float _height;
        private float _slamFrom;
        private float _drift;

        private float _stackLift;       // 지금 밀려 올라간 높이
        private float _stackTarget;     // 가야 할 높이

        public bool IsActive { get; private set; }
        public Vector2 Origin { get; private set; }
        public int StackLevel { get; private set; }
        /// <summary>사라지는 중 — 더는 밀어 올리지 않는다.</summary>
        public bool IsLeaving => _age > SlamSeconds + _hold;

        public void Init(TMP_FontAsset font)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = new Vector2(160f, 48f);

            // 잔상이 뒤(먼저), 본 숫자가 앞 — UGUI 는 형제 순서대로 그린다
            _echo = MakeLayer("Echo");
            _face = MakeLayer("Face");

            // ── TMP 대체 경로(그림이 아직 없을 때) ──
            // 원작 도트 폰트는 **비트맵**이라 SDF 외곽선이 없다 — 검은 글자를 한 칸 어긋나게 깔아 외곽선을 낸다.
            bool bitmap = font != null && font.atlasRenderMode == GlyphRenderMode.RASTER;
            _echoTmp = Setup(_echo.gameObject.AddComponent<TextMeshProUGUI>(), font);
            if (bitmap)
            {
                _shadow = Setup(_face.gameObject.AddComponent<TextMeshProUGUI>(), font);
                _shadow.color = Color.black;
                var fg = new GameObject("FaceText", typeof(RectTransform));
                var fr = (RectTransform)fg.transform;
                fr.SetParent(_face, false);
                fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
                fr.sizeDelta = _rect.sizeDelta;
                fr.anchoredPosition = new Vector2(-1f, 1f);
                _tmp = Setup(fg.AddComponent<TextMeshProUGUI>(), font);
            }
            else
            {
                _tmp = Setup(_face.gameObject.AddComponent<TextMeshProUGUI>(), font);
                var mat = _tmp.fontMaterial;   // 인스턴스 — 공유 머티리얼을 건드리면 다른 UI 까지 변한다
                mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            }
            gameObject.SetActive(false);
        }

        private RectTransform MakeLayer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(transform, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = _rect.sizeDelta;
            return r;
        }

        private static TextMeshProUGUI Setup(TextMeshProUGUI tmp, TMP_FontAsset font)
        {
            if (font != null) tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmp.fontSize = NormalHeight;
            return tmp;
        }

        private void BuildGlyphs()
        {
            if (_glyphsBuilt) return;
            _glyphsBuilt = true;
            for (int i = 0; i < MaxGlyphs; i++)
            {
                _echoGlyphs[i] = MakeGlyph(_echo, "e" + i);
                _faceGlyphs[i] = MakeGlyph(_face, "g" + i);
            }
        }

        private static Image MakeGlyph(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = false;
            img.enabled = false;
            return img;
        }

        // ── 띄우기 ──────────────────────────────────────────────

        public void Show(Vector2 at, int damage, Color color)
            => Show(at, damage.ToString(), color, crit: false);

        /// <summary>치명타는 붉고 크게, 더 크게 꽂힌다.</summary>
        public void Show(Vector2 at, int damage, Color color, bool crit)
            => Show(at, damage.ToString(), color, crit);

        /// <summary>피해 말고도 띄울 것이 있다 — 전술 빙의로 나간 Ghost HP 같은 것.</summary>
        public void Show(Vector2 at, string text, Color color) => Show(at, text, color, false);

        /// <summary>곁다리 수치(피흡 등) — 작게, 덜 튀게, 잔상 없이. 피해 숫자를 덮지 않는다.</summary>
        public void ShowMinor(Vector2 at, string text, Color color)
        {
            Begin(at, text, color, false, true);
        }

        public void Show(Vector2 at, string text, Color color, bool crit)
        {
            Begin(at, text, color, crit, false);
        }

        private void Begin(Vector2 at, string text, Color color, bool crit, bool minor)
        {
            _crit = crit;
            _minor = minor;
            _color = color;
            _age = 0f;
            _hold = crit ? CritHoldSeconds : HoldSeconds;
            _height = minor ? MinorHeight : crit ? CritHeight : NormalHeight;
            _slamFrom = minor ? 1.25f : crit ? CritSlamFrom : SlamFrom;
            _drift = Random.Range(-8f, 8f);
            Origin = at;
            StackLevel = 0;
            _stackLift = _stackTarget = 0f;
            IsActive = true;

            _useGlyphs = HasDigits && Layout(text);
            _tmp.enabled = !_useGlyphs;
            if (_shadow != null) _shadow.enabled = !_useGlyphs;
            _echoTmp.enabled = !_useGlyphs && !minor;
            if (!_useGlyphs)
            {
                for (int i = 0; i < _glyphCount; i++) { _faceGlyphs[i].enabled = false; _echoGlyphs[i].enabled = false; }
                _glyphCount = 0;
                _tmp.fontSize = _height;
                _tmp.text = text;
                if (_shadow != null) { _shadow.fontSize = _height; _shadow.text = text; }
                _echoTmp.fontSize = _height;
                _echoTmp.text = text;
            }

            gameObject.SetActive(true);
            Apply();
        }

        /// <summary>글자마다 숫자 그림을 놓는다. 그림에 없는 글자가 있으면 false(TMP 로 그린다).</summary>
        private bool Layout(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length > MaxGlyphs) return false;
            for (int i = 0; i < text.Length; i++)
                if (GlyphIndex(text[i]) < 0) return false;
            BuildGlyphs();
            float w = _height * DigitAspect;
            float step = w * (1f - DigitOverlap);
            float total = step * (text.Length - 1) + w;
            float x0 = -total * 0.5f + w * 0.5f;
            for (int i = 0; i < MaxGlyphs; i++)
            {
                bool on = i < text.Length;
                var f = _faceGlyphs[i];
                var e = _echoGlyphs[i];
                f.enabled = on;
                e.enabled = on && !_minor;
                if (!on) continue;
                var sp = s_digits[GlyphIndex(text[i])];
                f.sprite = sp;
                e.sprite = sp;
                var pos = new Vector2(Mathf.Round(x0 + step * i), 0f);
                var size = new Vector2(w, _height);
                f.rectTransform.anchoredPosition = pos;
                f.rectTransform.sizeDelta = size;
                e.rectTransform.anchoredPosition = pos;
                e.rectTransform.sizeDelta = size;
            }
            _glyphCount = text.Length;
            return true;
        }

        private static int GlyphIndex(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c == '+') return 10;
            if (c == '-') return 11;
            return -1;
        }

        /// <summary>
        /// 같은 적 위에 새 숫자가 꽂혔다 — 이 숫자는 한 줄 위로 밀린다. 너무 높이 쌓이면 바로 꺼지기 시작한다.
        /// </summary>
        public void PushUp()
        {
            if (!IsActive || IsLeaving) return;
            StackLevel++;
            _stackTarget = StackLevel * _height * StackLine;
            if (StackLevel >= MaxStack) _age = Mathf.Max(_age, SlamSeconds + _hold);   // 맨 위는 비켜 준다
        }

        public void Tick(float dt)
        {
            if (!IsActive) return;
            _age += dt;
            if (_age >= SlamSeconds + _hold + ExitSeconds) { Despawn(); return; }
            _stackLift += (_stackTarget - _stackLift) * Mathf.Min(1f, StackLerp * dt);
            Apply();
        }

        public void Despawn()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }

        private void Apply()
        {
            float y = StartLift + _stackLift;
            float scale, alpha;
            if (_age < SlamSeconds)
            {
                // 크고 옅게 → 제 크기로 꽂힌다(OutCubic — 마지막에 「쾅」 하고 선다)
                float k = _age / SlamSeconds;
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                scale = Mathf.Lerp(_slamFrom, 1f, e);
                alpha = Mathf.Lerp(_minor ? 0.6f : SlamAlpha, 1f, e);
                y += DropIn * (1f - e);
            }
            else if (_age < SlamSeconds + _hold)
            {
                float k = (_age - SlamSeconds) / _hold;
                // 꽂힌 순간 한 번 눌렸다 튄다(1 → 0.92 → 1) — 박힌 무게
                float squash = k < 0.12f ? Mathf.Lerp(0.92f, 1f, k / 0.12f) : 1f;
                scale = _minor ? 1f : squash;
                alpha = 1f;
                y += HoldDrift * k;
            }
            else
            {
                float k = (_age - SlamSeconds - _hold) / ExitSeconds;
                scale = Mathf.Lerp(1f, 0.85f, k);
                alpha = 1f - k * k;
                y += HoldDrift + ExitRise * k;
            }

            // 픽셀아트라 소수 좌표로 두면 글자가 흐릿하게 떨린다. 정수로 스냅한다.
            _rect.anchoredPosition = new Vector2(Mathf.Round(Origin.x + _drift), Mathf.Round(Origin.y + y));
            _face.localScale = new Vector3(scale, scale, 1f);

            // 치명타는 꽂힌 순간 흰빛으로 번쩍였다가 제 색으로 — 「터졌다」가 읽힌다
            var c = _color;
            if (_crit)
            {
                float flash = Mathf.Clamp01(1f - Mathf.Abs(_age - SlamSeconds) / 0.08f);
                c = Color.Lerp(c, Color.white, flash * 0.7f);
            }
            c.a = alpha;
            ApplyFace(c);

            // 잔상 — 꽂히는 동안 큰 크기로 남아 조금 더 커지며 사라진다
            float ea = 0f, es = _slamFrom;
            if (!_minor && _age < EchoSeconds)
            {
                float k = _age / EchoSeconds;
                ea = EchoAlpha * (1f - k);
                es = _slamFrom * Mathf.Lerp(1f, EchoGrow, k);
            }
            _echo.gameObject.SetActive(ea > 0.01f);
            if (ea > 0.01f)
            {
                _echo.localScale = new Vector3(es, es, 1f);
                var ec = _color;
                ec.a = ea;
                ApplyEcho(ec);
            }
        }

        private void ApplyFace(Color c)
        {
            if (_useGlyphs)
            {
                for (int i = 0; i < _glyphCount; i++) _faceGlyphs[i].color = c;
                return;
            }
            _tmp.color = c;
            if (_shadow != null) _shadow.color = new Color(0f, 0f, 0f, c.a);
        }

        private void ApplyEcho(Color c)
        {
            if (_useGlyphs)
            {
                for (int i = 0; i < _glyphCount; i++) _echoGlyphs[i].color = c;
                return;
            }
            _echoTmp.color = c;
        }
    }
}
