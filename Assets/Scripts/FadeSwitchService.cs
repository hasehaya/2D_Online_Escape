using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フェードアウト/インで表示を切り替えるための小型サービス。
/// </summary>
public static class FadeSwitchService
{
    public static Sequence Switch(Component from, Component to, float fadeOutDuration, float fadeInDuration,
        Ease ease = Ease.Linear)
    {
        return Switch(CreateTarget(from), CreateTarget(to), fadeOutDuration, fadeInDuration, ease);
    }

    public static Sequence Switch(Image from, Image to, float fadeDuration, Ease ease = Ease.Linear)
    {
        return Switch(new ImageTarget(from), new ImageTarget(to), fadeDuration, fadeDuration, ease);
    }

    public static Sequence Switch(SpriteRenderer from, SpriteRenderer to, float fadeDuration, Ease ease = Ease.Linear)
    {
        return Switch(new SpriteRendererTarget(from), new SpriteRendererTarget(to), fadeDuration, fadeDuration, ease);
    }

    public static Sequence Switch(CanvasGroup from, CanvasGroup to, float fadeDuration, Ease ease = Ease.Linear)
    {
        return Switch(new CanvasGroupTarget(from), new CanvasGroupTarget(to), fadeDuration, fadeDuration, ease);
    }

    private static Sequence Switch(IFadeTarget from, IFadeTarget to, float fadeOutDuration, float fadeInDuration,
        Ease ease)
    {
        from.SetActive(true);
        to.SetActive(true);
        to.SetAlpha(0f);
        from.SetAlpha(1f);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(from.FadeTo(0f, fadeOutDuration).SetEase(ease));
        sequence.Join(to.FadeTo(1f, fadeInDuration).SetEase(ease));
        sequence.OnComplete(() =>
        {
            from.SetAlpha(0f);
            from.SetActive(false);
            to.SetAlpha(1f);
        });

        return sequence;
    }

    private static IFadeTarget CreateTarget(Component component)
    {
        if (component is Image image) return new ImageTarget(image);
        if (component is SpriteRenderer spriteRenderer) return new SpriteRendererTarget(spriteRenderer);
        if (component is CanvasGroup canvasGroup) return new CanvasGroupTarget(canvasGroup);
        throw new ArgumentException("FadeSwitchServiceに対応していないComponentです。", nameof(component));
    }

    private interface IFadeTarget
    {
        void SetActive(bool active);
        void SetAlpha(float alpha);
        Tween FadeTo(float alpha, float duration);
    }

    private readonly struct ImageTarget : IFadeTarget
    {
        private readonly Image _image;

        public ImageTarget(Image image)
        {
            _image = image;
        }

        public void SetActive(bool active)
        {
            _image.gameObject.SetActive(active);
        }

        public void SetAlpha(float alpha)
        {
            Color color = _image.color;
            color.a = alpha;
            _image.color = color;
        }

        public Tween FadeTo(float alpha, float duration)
        {
            return _image.DOFade(alpha, duration);
        }
    }

    private readonly struct SpriteRendererTarget : IFadeTarget
    {
        private readonly SpriteRenderer _renderer;

        public SpriteRendererTarget(SpriteRenderer renderer)
        {
            _renderer = renderer;
        }

        public void SetActive(bool active)
        {
            _renderer.gameObject.SetActive(active);
        }

        public void SetAlpha(float alpha)
        {
            Color color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }

        public Tween FadeTo(float alpha, float duration)
        {
            return _renderer.DOFade(alpha, duration);
        }
    }

    private readonly struct CanvasGroupTarget : IFadeTarget
    {
        private readonly CanvasGroup _group;

        public CanvasGroupTarget(CanvasGroup group)
        {
            _group = group;
        }

        public void SetActive(bool active)
        {
            _group.gameObject.SetActive(active);
        }

        public void SetAlpha(float alpha)
        {
            _group.alpha = alpha;
        }

        public Tween FadeTo(float alpha, float duration)
        {
            return _group.DOFade(alpha, duration);
        }
    }
}