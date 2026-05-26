using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ブースター（成長促進剤や水）を用いて成長するプランターギミック。
/// 手順に沿ってアニメーション後に正解なら状態変化、失敗なら枯れてリセット。
/// 成熟期をタップすると葉っぱ等のアイテムを直接入手して最初に戻る。
/// </summary>
public class GimmickPlanter : InteractableObject
{
    public enum PlantState
    {
        Empty,
        Growing,
        Mature
    }

    [Header("UI References")] [SerializeField]
    private Image _planterImage;

    [SerializeField] private Sprite _emptySprite;

    [Header("Planter Settings")] [Tooltip("植物になるルートなどを設定したScriptableObject")] [SerializeField]
    private PlantRouteData _routeData;

    [Header("Current State Debug")] [SerializeField]
    private PlantState _currentState = PlantState.Empty;

    [SerializeField] private List<BoosterActionType> _history = new List<BoosterActionType>();
    [SerializeField] private PlantRoute _currentMatchingRoute = null;

    private bool _isAnimating = false;

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
        _history.Add(actionType);

        // 現在の履歴（手順）に合致するルートを検索（完全一致または前方一致）
        List<PlantRoute> matchingRoutes = new List<PlantRoute>();

        if (_routeData != null && _routeData.Routes != null)
        {
            foreach (var route in _routeData.Routes)
            {
                if (IsMatchingHistory(route.RequiredSequence, _history))
                {
                    matchingRoutes.Add(route);
                }
            }
        }

        if (matchingRoutes.Count > 0)
        {
            // 成長成功：とりあえず先頭の候補を保持（複数候補が前方一致する場合はどれかになる）
            PlantRoute bestMatch = matchingRoutes[0];
            _currentMatchingRoute = bestMatch;

            // 履歴と手数が完全に一致したか？
            if (_history.Count == bestMatch.RequiredSequence.Count)
            {
                // 成熟期へ
                _currentState = PlantState.Mature;
                _planterImage.sprite = bestMatch.MatureSprite;
                Debug.Log($"[{gameObject.name}] Reached Mature state: {bestMatch.RouteName}");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterMature);
                }
            }
            else
            {
                // まだ成長途中
                _currentState = PlantState.Growing;
                _planterImage.sprite = bestMatch.GrowingSprite;
                Debug.Log($"[{gameObject.name}] Reached Growing state: {bestMatch.RouteName}");

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySE(SESoundType.PlanterGrow);
                }
            }
        }
        else
        {
            // 失敗（枯れる）-> リセット
            Debug.Log($"[{gameObject.name}] Wrong sequence. The plant died. Resetting...");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySE(SESoundType.PlanterFail);
            }

            ResetPlanter();
        }
    }

    private bool IsMatchingHistory(List<BoosterActionType> required, List<BoosterActionType> current)
    {
        // そもそも手数が多すぎたら不一致
        if (current.Count > required.Count) return false;

        // 現在の手数まで一致しているか
        for (int i = 0; i < current.Count; i++)
        {
            if (required[i] != current[i]) return false;
        }

        return true;
    }

    private void CollectItem()
    {
        if (_currentMatchingRoute != null && _currentMatchingRoute.ResultItem != ItemType.None)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.TryAddItem(_currentMatchingRoute.ResultItem);
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
        if (_planterImage != null && _emptySprite != null)
        {
            _planterImage.sprite = _emptySprite;
        }
    }
}