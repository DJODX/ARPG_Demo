using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 接取任务面板：展示单个任务的标题、描述、目标与奖励，并提供接受/关闭按钮
/// 由 NPC 或触发区在显示面板后调用 Setup(definition) 注入任务
/// </summary>
public class QuestAcceptPanel : PanelBase
{
    [SerializeField, Tooltip("任务标题")]
    private Text _titleText;

    [SerializeField, Tooltip("任务描述")]
    private Text _descText;

    [SerializeField, Tooltip("目标列表文本")]
    private Text _objectiveText;

    [SerializeField, Tooltip("奖励列表文本")]
    private Text _rewardText;

    [SerializeField, Tooltip("接受按钮")]
    private Button _acceptButton;

    [SerializeField, Tooltip("关闭按钮")]
    private Button _closeButton;

    private readonly StringBuilder _builder = new StringBuilder();
    private QuestDefinition _definition;

    protected override void Init()
    {
        _acceptButton.onClick.AddListener(HandleAcceptClicked);
        _closeButton.onClick.AddListener(HandleCloseClicked);
        Refresh();
    }

    /// <summary>注入要展示的任务（可在 ShowPanel 之后调用）</summary>
    public void Setup(QuestDefinition definition)
    {
        _definition = definition;
        Refresh();
    }

    private void Refresh()
    {
        bool has = _definition != null;
        _titleText.text = has ? _definition.title : string.Empty;
        _descText.text = has ? _definition.description : string.Empty;
        _acceptButton.interactable = has && !QuestManager.Instance.IsAccepted(_definition.questId);

        if (!has)
        {
            _objectiveText.text = string.Empty;
            _rewardText.text = string.Empty;
            return;
        }

        _builder.Clear();
        foreach (QuestObjectiveSO objective in _definition.objectives)
        {
            _builder.AppendLine($"{objective.description}  x{objective.requiredCount}");
        }
        _objectiveText.text = _builder.ToString();

        _builder.Clear();
        foreach (RewardEntry reward in _definition.rewards)
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

    private void HandleAcceptClicked()
    {
        if (_definition == null) return;
        if (!QuestManager.Instance.AcceptQuest(_definition)) return;

        UIManager.Instance.HidePanel<QuestAcceptPanel>();
        UIInputBinder.SetCursorLocked(true);
    }

    private void HandleCloseClicked()
    {
        UIManager.Instance.HidePanel<QuestAcceptPanel>();
        UIInputBinder.SetCursorLocked(true);
    }
}
