using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace GameFramework.Core.Module.UI
{
    public sealed class UIManager : IUIManager
    {
        private readonly IResourceManager _resource;

        // 타입 → (주소, 레이어, 인스턴스 모드)
        private readonly Dictionary<Type, (string address, UILayer layer, UIInstanceMode mode)> _registry = new();

        // 현재 열린 패널 스택
        private readonly List<UIPanel> _openStack = new();

        // Single 모드 인스턴스 캐시
        private readonly Dictionary<Type, UIPanel> _singleInstances = new();

        // Multiple 모드 인스턴스 집합 (닫힐 때 Destroy)
        private readonly HashSet<UIPanel> _multipleInstances = new();

        // 패널 인스턴스 → 로드 시 사용한 Addressable 주소 (Release 호출용)
        private readonly Dictionary<UIPanel, string> _panelAddresses = new();

        public int OpenCount => _openStack.Count;

        public UIManager(IResourceManager resource) => _resource = resource;

        // ──────────────────────────────────────────────
        // 등록
        // ──────────────────────────────────────────────

        public void Register<T>(string address, UILayer layer = UILayer.Default,
                                UIInstanceMode mode = UIInstanceMode.Single) where T : UIPanel =>
            _registry[typeof(T)] = (address, layer, mode);

        // ──────────────────────────────────────────────
        // 열기
        // ──────────────────────────────────────────────

        public async UniTask<T> OpenAsync<T>() where T : UIPanel
        {
            var type = typeof(T);
            if (!_registry.TryGetValue(type, out var info))
                throw new InvalidOperationException($"[UIManager] Panel not registered: {type.Name}");

            if (info.mode == UIInstanceMode.Single)
                return await OpenSingleAsync<T>(type, info);
            else
                return await OpenMultipleAsync<T>(info);
        }

        private async UniTask<T> OpenSingleAsync<T>(
            Type type, (string address, UILayer layer, UIInstanceMode mode) info) where T : UIPanel
        {
            // 이미 스택에 있는 경우 → 최상단으로 승격
            if (_singleInstances.TryGetValue(type, out var existing) && _openStack.Contains(existing))
            {
                if (_openStack[^1] == existing)
                    return (T)existing; // 이미 최상단

                _openStack.Remove(existing);
                if (_openStack.Count > 0) _openStack[^1].OnBlur();
                _openStack.Add(existing);
                existing.OnFocus();
                return (T)existing;
            }

            // 캐시에 있지만 닫혀 있는 경우 → 재활성화
            if (_singleInstances.TryGetValue(type, out var cached))
            {
                if (_openStack.Count > 0) _openStack[^1].OnBlur();
                cached.gameObject.SetActive(true);
                _openStack.Add(cached);
                await cached.OnOpenAsync();
                return (T)cached;
            }

            // 최초 생성
            var panel = await CreatePanelAsync<T>(info);
            _singleInstances[type] = panel;
            if (_openStack.Count > 0) _openStack[^1].OnBlur();
            _openStack.Add(panel);
            await panel.OnOpenAsync();
            return (T)panel;
        }

        private async UniTask<T> OpenMultipleAsync<T>(
            (string address, UILayer layer, UIInstanceMode mode) info) where T : UIPanel
        {
            var panel = await CreatePanelAsync<T>(info);
            _multipleInstances.Add(panel);
            if (_openStack.Count > 0) _openStack[^1].OnBlur();
            _openStack.Add(panel);
            await panel.OnOpenAsync();
            return (T)panel;
        }

        private async UniTask<T> CreatePanelAsync<T>(
            (string address, UILayer layer, UIInstanceMode mode) info) where T : UIPanel
        {
            var prefab = await _resource.LoadAsync<GameObject>(info.address);
            if (prefab == null)
                throw new Exception($"[UIManager] Failed to load panel prefab: {info.address}");

            var go    = UnityEngine.Object.Instantiate(prefab);
            var panel = go.GetComponent<T>() ?? go.AddComponent<T>();

            var canvas = go.GetComponent<Canvas>() ?? go.AddComponent<Canvas>();
            canvas.sortingOrder = (int)info.layer;
            panel.Layer = info.layer;

            UnityEngine.Object.DontDestroyOnLoad(go);

            // Addressable 핸들 해제 추적용 — 패널 영구 파괴 시 Release 호출에 사용
            _panelAddresses[panel] = info.address;

            return panel;
        }

        // ──────────────────────────────────────────────
        // 닫기
        // ──────────────────────────────────────────────

        public T FindOpenPanel<T>() where T : UIPanel
        {
            for (int i = _openStack.Count - 1; i >= 0; i--)
                if (_openStack[i] is T panel) return panel;
            return null;
        }

        public async UniTask CloseAsync<T>() where T : UIPanel
        {
            if (!_singleInstances.TryGetValue(typeof(T), out var panel)) return;
            await CloseAndDeactivate(panel, destroyIfMultiple: false);
        }

        public async UniTask CloseAsync(UIPanel instance)
        {
            if (instance == null || !_openStack.Contains(instance)) return;
            await CloseAndDeactivate(instance, destroyIfMultiple: true);
        }

        public async UniTask CloseTopAsync()
        {
            if (_openStack.Count == 0) return;
            await CloseAndDeactivate(_openStack[^1], destroyIfMultiple: true);
        }

        public async UniTask CloseAllAsync()
        {
            for (int i = _openStack.Count - 1; i >= 0; i--)
            {
                var p = _openStack[i];
                await p.OnCloseAsync();
                DestroyOrDeactivate(p);
            }
            _openStack.Clear();
        }

        /// <summary>
        /// UIModule.Dispose에서 호출. 모든 인스턴스를 동기적으로 즉시 파괴하고
        /// Addressable 핸들을 일괄 해제한다 (Single/Multiple 구분 없음).
        /// </summary>
        public void DisposeAll()
        {
            // Single 인스턴스도 영구 파괴 — 매니저 종료 시점이므로 캐시 의미 없음
            foreach (var panel in _singleInstances.Values)
            {
                if (panel == null) continue;
                ReleasePanelAddress(panel);
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            _singleInstances.Clear();

            foreach (var panel in _multipleInstances)
            {
                if (panel == null) continue;
                ReleasePanelAddress(panel);
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            _multipleInstances.Clear();

            _openStack.Clear();
            _panelAddresses.Clear();
        }

        private async UniTask CloseAndDeactivate(UIPanel panel, bool destroyIfMultiple)
        {
            if (!_openStack.Contains(panel)) return;
            bool wasTop = _openStack[^1] == panel;
            _openStack.Remove(panel);

            await panel.OnCloseAsync();
            DestroyOrDeactivate(panel);

            if (wasTop && _openStack.Count > 0)
                _openStack[^1].OnFocus();
        }

        private void DestroyOrDeactivate(UIPanel panel)
        {
            if (_multipleInstances.Contains(panel))
            {
                _multipleInstances.Remove(panel);
                ReleasePanelAddress(panel);
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            else
            {
                // Single 모드 — 인스턴스는 유지하고 비활성화만 한다 (재오픈 시 재활성화).
                // Addressable 핸들도 함께 유지된다 — DisposeAll에서 일괄 해제된다.
                panel.gameObject.SetActive(false);
            }
        }

        private void ReleasePanelAddress(UIPanel panel)
        {
            if (_panelAddresses.TryGetValue(panel, out var address))
            {
                _resource.Release(address);
                _panelAddresses.Remove(panel);
            }
        }
    }
}
