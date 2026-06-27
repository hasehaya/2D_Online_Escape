using System.Collections.Generic;
using Escape.SceneObject.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Escape.SceneObject.Elias.Prepare
{
    /// <summary>
    /// ブースター（成長促進剤や水）を用いて成長するプランターギミック。
    /// 手順に沿ってアニメーション後に正解なら状態変化、失敗なら枯れてリセット。
    /// 成熟期をタップすると葉っぱ等のアイテムを直接入手して最初に戻る。
    /// </summary>
    public class GimmickPlanter : InteractableObject
    {
        private enum PlantState
        {
            Empty,
            Growing,
            Mature
        }

        [Header("UI References")] [SerializeField]
        private Image _leafImage;

        [Header("Planter Settings")] [Tooltip("植物になるルートなどを設定したScriptableObject")] [SerializeField]
        private PlantRouteData _routeData;

        [Header("Current State Debug")] [SerializeField]
        private PlantState _currentState = PlantState.Empty;

        [SerializeField] private List<BoosterActionType> _history = new List<BoosterActionType>();
        [SerializeField] private PlantRoute _currentMatchingRoute;

        private bool _isAnimating;

        protected override void Awake()
        {
            base.Awake();
            ResetPlanter();
        }

        protected override void Interact()
        {
            base.Interact();

            if (_isAnimating) return;

            // すでに成熟している場合はアイテム回収してリセット
            if (_currentState == PlantState.Mature && _currentMatchingRoute != null)
            {
                CollectItem();
                return;
            }

            // ブースターが選択されているか確認
            var booster = GimmickGrowthBooster.SelectedBooster;
            if (booster != null)
            {
                _isAnimating = true;
                BoosterActionType actionType = booster.GetActionType();

                // ブースターをプランターのアニメーション基準点に移動させてアニメーション実行
                booster.ExecuteAction(transform, () =>
                {
                    ProcessGrowth(actionType);
                    _isAnimating = false;
                });
            }
            else
            {
                Debug.Log($"[{gameObject.name}] No booster selected. Or clicked while empty without Booster.");
            }
        }

        private void ProcessGrowth(BoosterActionType actionType)
        {
            if (_currentMatchingRoute == null)
            {
                _currentMatchingRoute = FindRouteByFirstAction(actionType);
            }

            if (_currentMatchingRoute == null)
            {
                // 失敗（枯れる）-> リセット
                Debug.Log($"[{gameObject.name}] Wrong sequence. The plant died. Resetting...");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterFail);
                }

                ResetPlanter();
                return;
            }

            if (!IsNextActionMatch(_currentMatchingRoute.RequiredSequence, _history, actionType))
            {
                // 失敗（枯れる）-> リセット
                Debug.Log($"[{gameObject.name}] Wrong sequence. The plant died. Resetting...");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterFail);
                }

                ResetPlanter();
                return;
            }

            _history.Add(actionType);

            // 1回目でルート確定。2回目で成長期、それ以降は手順が進むまで成長期を維持。
            if (_history.Count == 1)
            {
                Debug.Log($"[{gameObject.name}] Route fixed: {_currentMatchingRoute.RouteName}");
                return;
            }

            if (_history.Count == 2)
            {
                _currentState = PlantState.Growing;
                _leafImage.enabled = true;
                _leafImage.sprite = _currentMatchingRoute.GrowingSprite;
                Debug.Log($"[{gameObject.name}] Reached Growing state: {_currentMatchingRoute.RouteName}");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterGrow);
                }

                return;
            }

            if (_history.Count == 4)
            {
                _currentState = PlantState.Mature;
                _leafImage.enabled = true;
                _leafImage.sprite = _currentMatchingRoute.MatureSprite;
                Debug.Log($"[{gameObject.name}] Reached Mature state: {_currentMatchingRoute.RouteName}");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterMature);
                }
            }
        }

        private PlantRoute FindRouteByFirstAction(BoosterActionType actionType)
        {
            if (_routeData == null || _routeData.Routes == null) return null;

            foreach (var route in _routeData.Routes)
            {
                if (route.RequiredSequence != null && route.RequiredSequence.Count > 0 &&
                    route.RequiredSequence[0] == actionType)
                {
                    return route;
                }
            }

            return null;
        }

        private bool IsNextActionMatch(List<BoosterActionType> required, List<BoosterActionType> current,
            BoosterActionType nextAction)
        {
            if (current.Count >= required.Count) return false;

            return required[current.Count] == nextAction;
        }

        private void CollectItem()
        {
            if (_currentMatchingRoute != null && _currentMatchingRoute.ResultItem != ItemType.None)
            {
                if (InventoryManager.Instance != null)
                {
                    if (!InventoryManager.Instance.TryAddItem(_currentMatchingRoute.ResultItem))
                    {
                        Debug.Log(
                            $"[{gameObject.name}] Inventory full. Harvest blocked: {_currentMatchingRoute.ResultItem}");
                        return;
                    }

                    Debug.Log($"[{gameObject.name}] Collected item: {_currentMatchingRoute.ResultItem}");
                    AudioManager.Instance.PlaySE(SESoundType.PlanterHarvest);
                }
            }

            // アイテムを入手したら最初からになる
            ResetPlanter();
        }

        private void ResetPlanter()
        {
            _currentState = PlantState.Empty;
            _history.Clear();
            _currentMatchingRoute = null;
            _leafImage.enabled = false;
        }
    }
}