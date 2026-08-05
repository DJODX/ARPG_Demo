using System;
using UnityEngine;

/// <summary>
/// UI 面板基类：淡入淡出显示/隐藏
/// 子类继承并实现 Init()，通过 Show()/Hide() 控制面板
/// </summary>
public abstract class PanelBase : MonoBehaviour
{
    [SerializeField, Tooltip("面板 CanvasGroup（为空时自动获取/添加）")]
    private CanvasGroup _canvasGroup;

    [SerializeField, Tooltip("淡入淡出速度")]
    private float _alphaSpeed = 10f;

    private bool _isShow;
    private Action _onHide;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        Init();
    }

    /// <summary>子类初始化（Start 时调用一次）</summary>
    protected abstract void Init();

    /// <summary>显示面板（淡入）</summary>
    public virtual void Show()
    {
        _isShow = true;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        _onHide = null; // 清除未完成的隐藏回调，避免旧回调残留
    }

    /// <summary>隐藏面板（淡出，完全隐藏后回调）</summary>
    public virtual void Hide(Action onHide = null)
    {
        _isShow = false;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false; // 隐藏后不再拦截点击
        _onHide = onHide;

        // alpha 已为 0（如从未显示过）：立即执行回调，否则回调永远不触发
        if (_canvasGroup.alpha <= 0f && _onHide != null)
        {
            Action cb = _onHide;
            _onHide = null;
            cb.Invoke();
        }
    }

    protected virtual void Update()
    {
        // 已完全隐藏且没有待办回调时无需每帧处理
        if (!_isShow && _canvasGroup.alpha == 0f) return;

        if (_isShow)
        {
            _canvasGroup.alpha = Mathf.Min(1f, _canvasGroup.alpha + _alphaSpeed * Time.deltaTime);
        }
        else
        {
            _canvasGroup.alpha = Mathf.Max(0f, _canvasGroup.alpha - _alphaSpeed * Time.deltaTime);
            if (_canvasGroup.alpha <= 0f)
            {
                Action cb = _onHide;
                _onHide = null;
                cb?.Invoke();
            }
        }
    }
}
