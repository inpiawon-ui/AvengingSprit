using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>전투 필드 위의 개체 한 기. 고스트·호스트·적·보스가 모두 이 한 종류다.</summary>
    public enum UnitSide
    {
        Player,
        Enemy,
    }

    /// <summary>
    /// 인게임 유닛. UI 캔버스 위에 Image 로 그린다(전투 필드가 `RoomField` 하위 RectTransform).
    ///
    /// 별도 물리를 쓰지 않는다. 방 기반 오토어택은 위치·거리 계산만으로 충분하고,
    /// Rigidbody 를 걸면 UI 좌표계와 물리 좌표계가 섞여 디버깅이 어려워진다.
    /// </summary>
    public sealed class Unit : MonoBehaviour
    {
        private RectTransform _rect;
        private Image _body;
        private Image _hpBarBg;
        private Image _hpBarFill;
        private Image _possessMark;

        private float _attackTimer;
        private float _flashTimer;
        private int _slowPercent;
        private float _slowTimer;

        public UnitSide Side { get; private set; }
        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public int Hp { get; private set; }
        public int HpMax { get; private set; }
        public int Atk { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackRange { get; private set; }
        public float AttackInterval { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsAlive => Hp > 0;

        public Vector2 Position
        {
            get => _rect.anchoredPosition;
            set => _rect.anchoredPosition = value;
        }

        /// <summary>호스트가 될 수 있는 적인가. 보스는 빙의 대상이 아니다.</summary>
        public bool IsPossessable => Side == UnitSide.Enemy && !IsBoss && IsAlive;

        /// <summary>공격 방식 데이터. 보스는 null (기본 단발).</summary>
        public Game.Character.HostEntry Profile { get; private set; }

        public void Setup(UnitSide side, string key, string displayName, Sprite sprite,
                          int hp, int atk, float moveSpeed, float attackRange,
                          float attackInterval, Vector2 size, bool isBoss = false,
                          Game.Character.HostEntry profile = null)
        {
            Profile = profile;
            _rect = (RectTransform)transform;
            Side = side;
            Key = key;
            DisplayName = displayName;
            HpMax = Mathf.Max(1, hp);
            Hp = HpMax;
            Atk = atk;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackInterval = attackInterval;
            IsBoss = isBoss;

            _rect.anchorMin = _rect.anchorMax = new Vector2(0f, 1f);
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.sizeDelta = size;

            _body = GetOrCreate("Body", size, Vector2.zero);
            _body.sprite = sprite;
            _body.preserveAspect = true;
            _body.raycastTarget = false;

            // 적만 머리 위 체력바를 단다. 플레이어 체력은 상단 HUD 가 담당한다.
            if (side == UnitSide.Enemy)
            {
                var barSize = new Vector2(size.x * 0.7f, 5f);
                var barPos = new Vector2(0f, size.y * 0.5f + 6f);
                _hpBarBg = GetOrCreate("HpBarBg", barSize, barPos);
                _hpBarBg.color = new Color(0.06f, 0.07f, 0.10f, 0.9f);
                _hpBarFill = GetOrCreate("HpBarFill", barSize, barPos, _hpBarBg.transform);
                _hpBarFill.color = new Color(0.85f, 0.20f, 0.16f, 1f);
                ((RectTransform)_hpBarFill.transform).pivot = new Vector2(0f, 0.5f);
                ((RectTransform)_hpBarFill.transform).anchoredPosition = new Vector2(-barSize.x * 0.5f, 0f);

                if (!isBoss)
                {
                    _possessMark = GetOrCreate("PossessMark", new Vector2(18f, 18f),
                                               new Vector2(0f, size.y * 0.5f + 20f));
                    _possessMark.color = new Color(0.55f, 0.80f, 1f, 0.95f);
                    _possessMark.gameObject.SetActive(false);
                }
            }
            RefreshHpBar();
        }

        private Image GetOrCreate(string name, Vector2 size, Vector2 pos, Transform parent = null)
        {
            var p = parent != null ? parent : transform;
            var t = p.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(p, false);
                t = go.transform;
            }
            var rt = (RectTransform)t;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = t.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        public void SetPossessMark(bool on)
        {
            if (_possessMark != null) _possessMark.gameObject.SetActive(on);
        }

        public void SetSprite(Sprite s)
        {
            if (_body != null) _body.sprite = s;
        }

        /// <summary>피해를 적용한다. 사망했으면 true.</summary>
        public bool TakeDamage(int amount)
        {
            if (!IsAlive) return false;
            Hp = Mathf.Max(0, Hp - Mathf.Max(1, amount));
            RefreshHpBar();
            _flashTimer = 0.12f;
            return Hp == 0;
        }

        public void Heal(int amount) { Hp = Mathf.Min(HpMax, Hp + amount); RefreshHpBar(); }

        private void RefreshHpBar()
        {
            if (_hpBarFill == null) return;
            var rt = (RectTransform)_hpBarFill.transform;
            float full = _hpBarBg.rectTransform.sizeDelta.x;
            rt.sizeDelta = new Vector2(full * ((float)Hp / HpMax), rt.sizeDelta.y);
        }

        /// <summary>공격 쿨다운을 진행시키고, 이번 프레임에 때릴 수 있으면 true.</summary>
        public bool TickAttack(float dt)
        {
            _attackTimer -= dt;
            if (_attackTimer > 0f) return false;
            _attackTimer = AttackInterval;
            return true;
        }

        /// <summary>피격 점멸. 스프라이트를 건드리지 않고 틴트만 흔든다.</summary>
        public void TickFlash(float dt)
        {
            if (_body == null) return;
            if (_flashTimer <= 0f) { _body.color = Color.white; return; }
            _flashTimer -= dt;
            _body.color = _flashTimer > 0f ? new Color(1f, 0.45f, 0.45f, 1f) : Color.white;
        }

        /// <summary>둔화 부여(설녀). 더 강한 둔화가 걸려 있으면 유지한다.</summary>
        public void ApplySlow(int percent, float seconds)
        {
            if (percent <= 0) return;
            if (percent >= _slowPercent) _slowPercent = Mathf.Clamp(percent, 0, 90);
            _slowTimer = Mathf.Max(_slowTimer, seconds);
        }

        public void TickSlow(float dt)
        {
            if (_slowTimer <= 0f) return;
            _slowTimer -= dt;
            if (_slowTimer <= 0f) _slowPercent = 0;
        }

        private float CurrentSpeed => MoveSpeed * (1f - _slowPercent / 100f);

        public void MoveToward(Vector2 target, float dt)
        {
            var d = target - Position;
            float len = d.magnitude;
            if (len < 0.001f) return;
            Position += d / len * CurrentSpeed * dt;
        }
    }
}
