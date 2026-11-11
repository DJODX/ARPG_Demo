using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务面板：左侧列出已接取任务，右侧显示选中任务的描述、目标进度与奖励
/// 预制体需配置：条目容器（挂 VerticalLayoutGroup）+ 条目预制体 + 详情文本 + 关闭按钮
/// </summary>
public class QuestPanel : PanelBase
{
    [SerializeField, Tooltip("任务条目容器（挂 VerticalLayoutGroup）")]
    private Transform _listParent;

    [SerializeField, Tooltip("任务条目预制体（挂 QuestEntryView）")]
    private GameObject _entryPrefab;

    [SerializeField, Tooltip("详情标题")]
    private Text _titleText;

    [SerializeField, Tooltip("详情描述")]
    private Text _descText;

    [SerializeField, Tooltip("目标进度文本")]
    private Text _objectiveText;

    [SerializeField, Tooltip("奖励文本")]
    private Text _rewardText;

    [SerializeField, Tooltip("关闭按钮")]
    private Button _closeButton;

    private readonly List<QuestEntryView> _entries = new List<QuestEntryView>();
    private readonly StringBuilder _builder = new StringBuilder();

    private QuestManager _questManager;
    private QuestEntryView _selected;

    private void Awake()
    {
        _questManager = QuestManager.Instance;
    }

    protected override void Init()
    {
        _closeButton.onClick.AddListener(HandleCloseClicked);
        _questManager.OnQuestAccepted += HandleQuestChanged;
        _questManager.OnQuestUpdated += HandleQuestChanged;
        _questManager.OnQuestCompleted += HandleQuestChanged;
        Refresh();
    }

    private void HandleQuestChanged(QuestRuntime runtime) => Refresh();

    /// <summary>按当前已接任务刷新列表，并尽量保持原选中项</summary>
    private void Refresh()
    {
        string selectedId = _selected != null ? _selected.Runtime.questId : null;
        List<QuestRuntime> quests = new List<QuestRuntime>(_questManager.ActiveQuests);

        // 条目复用：数量不足时补建，够用时隐藏多余的，避免每次刷新都重建
        while (_entries.Count < quests.Count)
        {
            QuestEntryView entry = Instantiate(_entryPrefab, _listParent).GetComponent<QuestEntryView>();
            entry.OnClicked += HandleEntryClicked;
            _entries.Add(entry);
        }

        _selected = null;
        for (int i = 0; i < _entries.Count; i++)
        {
            QuestEntryView entry = _entries[i];
            bool used = i < quests.Count;
            entry.gameObject.SetActive(used);
            if (!used) continue;

            entry.Refresh(quests[i]);
            entry.SetSelected(false);
            if (quests[i].questId == selectedId) _selected = entry;
        }

        // 首次打开或原选中任务已不存在时，默认选中第一个
        if (_selected == null && quests.Count > 0) _selected = _entries[0];

        if (_selected != null) _selected.SetSelected(true);
        RefreshDetail();
    }

    private void HandleEntryClicked(QuestEntryView entry)
    {
        if (_selected == entry) return;

        if (_selected != null) _selected.SetSelected(false);
        _selected = entry;
        _selected.SetSelected(true);
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        QuestRuntime runtime = _selected != null ? _selected.Runtime : null;
        bool has = runtime != null && runtime.definition != null;

        _titleText.text = has
            ? (runtime.status == QuestStatus.Completed
                ? runtime.definition.title + "（已完成）"
                : runtime.definition.title)
            : "暂无任务";
        _descText.text = has ? runtime.definition.description : string.Empty;

        if (!has)
        {
            _objectiveText.text = string.Empty;
            _rewardText.text = string.Empty;
            return;
        }

        QuestDefinition definition = runtime.definition;

        _builder.Clear();
        for (int i = 0; i < definition.objectives.Count; i++)
        {
            QuestObjectiveSO objective = definition.objectives[i];
            _builder.AppendLine($"{objective.description}  {runtime.GetProgress(i)}/{objective.requiredCount}");
        }
        _objectiveText.text = _builder.ToString();

        _builder.Clear();
        foreach (RewardEntry reward in definition.rewards)
        {
            _builder.AppendLine($"{GetRewardName(reward)} x{reward.amount}");
        }
        _rewardText.text = _builder.ToString();
    }

    private static string GetRewardName(RewardEntry reward)
    {
        if (reward.type == RewardType.Item) return reward.item != null ? reward.item.itemName : "道具";
        return reward.type == RewardType.Gold ? "金币" : "经验";
    }

    private void HandleCloseClicked()
    {
        UIManager.Instance.HidePanel<QuestPanel>();
        UIInputBinder.SetCursorLocked(true); // 关闭面板后恢复鼠标锁定
    }

    private void OnDestroy()
    {
        if (_questManager == null) return;

        _questManager.OnQuestAccepted -= HandleQuestChanged;
        _questManager.OnQuestUpdated -= HandleQuestChanged;
        _questManager.OnQuestCompleted -= HandleQuestChanged;
    }
}
