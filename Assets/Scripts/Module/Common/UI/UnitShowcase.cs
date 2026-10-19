using Cysharp.Threading.Tasks;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 로비 · 아웃게임 진열 — 인게임과 **같은 몸 그림**을 큰 칸(UnitLarge, 384)으로 움직이며 띄운다(PD 2026-10-19
    /// 「다 사이즈 업해서 로비 및 아웃게임에서도 적용」). 붙인 Image(기존 정지 초상)는 그림이 없을 때의 대신으로 남는다.
    ///
    /// 그림 칸은 몸 둘레에 여백이 있다(128 칸 규격: 잉크 76 · 발밑이 아래에서 24/128). 자식 칸을 그만큼 키우고
    /// 발밑을 붙인 칸의 아래 변에 맞춘다 — 기존 초상 칸(아래 가운데 기준)에 그대로 선다.
    /// 아틀라스 `atlas/unitl_{키}` (`Assets/BaseResource/UnitLarge/{키}/unitL_{키}_{방향}{동작}.png`).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class UnitShowcase : MonoBehaviour
    {
        private const string AtlasPrefix = "atlas/unitl_";
        private const float FootFromBottom = 24f / 128f;   // 칸 아래에서 발밑까지
        private const float InkOfCell = 76f / 128f;        // 칸 높이 대비 몸(대기) 높이

        [SerializeField] private string _facing = "se";
        [SerializeField, Range(0.5f, 1.2f)] private float _fill = 0.95f;   // 붙인 칸 높이 대비 몸 높이
        [SerializeField] private float _idleSeconds = 2.4f;
        [SerializeField] private float _frameSeconds = 0.16f;
        /// <summary>고정 진열(유령 · 패스 · 밤의 갱스터 등) — 코드가 Show 를 안 부르는 칸은 이 몸을 스스로 띄운다.</summary>
        [SerializeField] private string _autoKey;
        /// <summary>떠다니는 몸(유령) — 공격 대신 대기 ↔ 이동 두 칸으로 둥실거린다.</summary>
        [SerializeField] private bool _float;

        private Image _fallback;
        private Image _body;
        private string _key;
        private string _address;
        private Sprite _idle, _atk1, _atk2, _walk1, _walk2;
        private Color _tint = Color.white;
        private bool _shown;
        private float _timer;
        private int _step;
        private int _loadTicket;

        private void Awake()
        {
            _fallback = GetComponent<Image>();
        }

        /// <summary>떠다니는 몸으로 둥실거리게 한다(코드가 붙이는 칸 — 홈 유령).</summary>
        public void SetFloat(bool on) => _float = on;

        private void Start()
        {
            if (!string.IsNullOrEmpty(_autoKey) && _key == null) Show(_autoKey);
        }

        private void OnDisable()
        {
            _step = 0;
            _timer = 0f;
        }

        private void OnDestroy() => Release();

        /// <summary>
        /// 그 호스트의 큰 몸 그림을 띄운다. 그림(아틀라스)이 없으면 붙인 Image 의 초상을 그대로 둔다.
        /// key 가 비면 몸을 숨긴다. tint 는 안 가진 몸 잿빛 등 — 초상 · 큰 그림 어느 쪽이 서든 같이 칠한다.
        /// </summary>
        public void Show(string key, Color? tint = null)
        {
            _tint = tint ?? Color.white;
            if (key == _key)
            {
                SetBodyVisible(_shown);
                return;
            }
            Release();
            _key = key;
            _idle = _atk1 = _atk2 = _walk1 = _walk2 = null;
            SetBodyVisible(false);
            if (string.IsNullOrEmpty(key)) return;
            LoadAsync(key, ++_loadTicket).Forget();   // fire-and-forget: 그림이 오면 그때 바꿔 낀다 — 그동안은 초상이 선다
        }

        private async UniTaskVoid LoadAsync(string key, int ticket)
        {
            if (!CoreModule.TryGet<IResourceManager>(out var res)) return;
            string address = AtlasPrefix + key;
            SpriteAtlas atlas;
            try { atlas = await res.LoadAsync<SpriteAtlas>(address); }
            catch (System.Exception) { return; }   // 아직 큰 그림이 없는 몸 — 초상으로 버틴다
            if (this == null || ticket != _loadTicket || atlas == null)
            {
                if (atlas != null) res.Release(address);
                return;
            }
            _address = address;
            string n = $"unitL_{key}_{_facing}";
            _idle = atlas.GetSprite(n);
            _atk1 = atlas.GetSprite(n + "_atk1");
            _atk2 = atlas.GetSprite(n + "_atk2");
            _walk1 = atlas.GetSprite(n + "_walk1");
            _walk2 = atlas.GetSprite(n + "_walk2");
            if (_idle == null) return;
            EnsureBody();
            _body.sprite = _idle;
            _step = 0;
            _timer = _idleSeconds;
            SetBodyVisible(true);
        }

        private void Update()
        {
            if (_body == null || !_body.enabled || _idle == null) return;
            Fit();
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            if (_float)
            {
                // 대기 → 이동1 → 대기 → 이동2 — 제자리에서 둥실거린다
                _step = (_step + 1) % 4;
                _body.sprite = _step == 1 && _walk1 != null ? _walk1 : _step == 3 && _walk2 != null ? _walk2 : _idle;
                _timer = _frameSeconds * 2.5f;
                return;
            }
            // 대기 → 공격1 → 공격2 → 대기 — 진열 칸에서 가만히 서 있지 않게, 몸마다 제 동작을 보여 준다
            _step = (_step + 1) % 3;
            _body.sprite = _step == 1 && _atk1 != null ? _atk1 : _step == 2 && _atk2 != null ? _atk2 : _idle;
            _timer = _step == 0 ? _idleSeconds : _step == 2 ? _frameSeconds * 2f : _frameSeconds;
        }

        private void EnsureBody()
        {
            if (_body != null) return;
            var go = new GameObject("UnitShowcaseBody", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            _body = go.AddComponent<Image>();
            _body.preserveAspect = true;
            _body.raycastTarget = false;
            Fit();
        }

        /// <summary>붙인 칸 크기를 따라 몸 칸을 맞춘다 — 화면 비율(4:3 · 20:9)로 칸이 바뀌어도 따라간다.</summary>
        private void Fit()
        {
            var parent = (RectTransform)transform;
            var rt = _body.rectTransform;
            float h = Mathf.Max(1f, parent.rect.height);
            float size = h * _fill / InkOfCell;
            if (Mathf.Abs(rt.sizeDelta.x - size) < 0.5f) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(0f, -size * FootFromBottom);
        }

        private void SetBodyVisible(bool on)
        {
            _shown = on;
            if (_body != null)
            {
                _body.enabled = on;
                _body.color = _tint;
            }
            // 큰 몸 그림이 서면 초상은 숨긴다(지우지 않는다 — 그림이 없을 때의 대신)
            if (_fallback != null) _fallback.color = on ? Color.clear : _tint;
        }

        private void Release()
        {
            if (string.IsNullOrEmpty(_address)) return;
            if (CoreModule.TryGet<IResourceManager>(out var res)) res.Release(_address);
            _address = null;
        }
    }
}
