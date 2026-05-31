using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 成長促進剤や水などのアクションタイプ。
/// </summary>
public enum BoosterActionType
{
    NutrientA, // 栄養剤1
    NutrientB, // 栄養剤2
    Water // 水やり
}

/// <summary>
/// 画面上に固定配置された専用のインタラクティブオブジェクト（ブースターや如雨露など）。
/// タップで選択状態になり、その後プランターをタップすると対象に向けてアニメーションする。
/// </summary>
public class GimmickGrowthBooster : InteractableObject
{
    public static GimmickGrowthBooster SelectedBooster { get; private set; }

    [SerializeField] private BoosterActionType _actionType;

    [Header("Animation Settings")] [SerializeField]
    private float _moveDuration = 0.5f;

    [SerializeField] private float _actionDuration = 1.0f;
    [SerializeField] private Vector3 _actionOffset = new Vector3(0, 100, 0); // プランターに対してどれくらい上等に移動するか

    private Vector3 _initialPosition;

    protected override void Awake()
    {
        base.Awake();
        _initialPosition = transform.position;
    }

    protected override void Interact()
    {
        base.Interact();
        // 選択状態にする
        SelectedBooster = this;
        Debug.Log($"[{gameObject.name}] Booster Selected: {_actionType}");

        // 選択されたことがわかるように少し跳ねるアニメーション（DOTween）
        transform.DOKill();
        transform.position = _initialPosition; // リセット
        transform.DOPunchPosition(Vector3.up * 10f, 0.3f, 2, 0.5f);
    }

    /// <summary>
    /// 指定された対象へ向かってアニメーションし、完了後にコールバックを呼ぶ。
    /// </summary>
    public void ExecuteAction(Transform targetTransform, Action onComplete)
    {
        SelectedBooster = null; // 実行開始で選択解除（連続使用を防ぐ場合は必要に応じて調整）

        Sequence seq = DOTween.Sequence();

        Vector3 targetPos = targetTransform.position + _actionOffset;

        // 1. 対象のプランターへ移動
        seq.Append(transform.DOMove(targetPos, _moveDuration).SetEase(Ease.OutQuad));

        // 2. Enumごとに異なるアクションのアニメーション
        switch (_actionType)
        {
            case BoosterActionType.NutrientA:
                seq.Append(transform.DORotate(new Vector3(0, 0, 45), 0.2f));
                seq.AppendInterval(_actionDuration);
                seq.Append(transform.DORotate(Vector3.zero, 0.2f));
                break;
            case BoosterActionType.NutrientB:
                seq.Append(transform.DOPunchPosition(Vector3.down * 20f, _actionDuration, 5, 0.5f));
                break;
            case BoosterActionType.Water:
                seq.Append(transform.DORotate(new Vector3(0, 0, 45), 0.2f));
                seq.AppendInterval(_actionDuration);
                seq.Append(transform.DORotate(Vector3.zero, 0.2f));
                break;
        }

        // 3. 元の位置へ戻る
        seq.Append(transform.DOMove(_initialPosition, _moveDuration).SetEase(Ease.InQuad));

        // 4. 完了通知
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public BoosterActionType GetActionType() => _actionType;
}