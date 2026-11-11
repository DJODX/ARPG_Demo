using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 任务列表条目：显示任务标题与整体完成进度，支持点击选中
/// 由 QuestPanel 实例化到列表容器中
/// </summary>
public class QuestEntryView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField, Tooltip("任务标题文本")]
    private Text _titleText;

    [SerializeField, Tooltip("进度文本（如 1/3 或 已完成）")]
    private Text _progressText;

    [SerializeField, Tooltip("选中时的背景色")]
    private Color _selectedColor = new Color(1f, 0.85f, 0.3f, 1f);

    /// <summary>根节点背景 Image，用于选中高亮</summary>
    private Image _background;
    private Color _normalColor;

    /// <summary>当前展示的任务运行时状态</summary>
    public QuestRuntime Runtime { get; private set; }

    /// <summary>点击事件，由面板订阅</summary>
    public event Action<QuestEntryView> OnClicked;

    private void Awake()
    {
        _background = GetComponent<Image>();
        if (_background != null) _normalColor = _background.color;
    }

    /// <summary>刷新条目显示</summary>
    public void Refresh(QuestRuntime runtime)
    {
        Runtime = runtime;
        QuestDefinition definition = runtime.definition;

        _titleText.text = definition != null ? definition.title : runtime.questId;

        int total = definition != null ? definition.objectives.Count : 0;
        int done = 0;
        for (int i = 0; i < total; i++)
        {
            if (runtime.IsObjectiveComplete(i)) done++;
        }

        _progressText.text = runtime.status == QuestStatus.Completed ? "已完成" : $"{done}/{total}";
    }

    /// <summary>设置选中状态（仅切换背景色）</summary>
    public void SetSelected(bool selected)
    {
        if (_background != null)
            _background.color = selected ? _selectedColor : _normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}
