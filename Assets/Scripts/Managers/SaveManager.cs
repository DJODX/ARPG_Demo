using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器（单例，跨场景常驻）
/// 负责 JSON 序列化、槽位文件路径、原子写入与损坏兜底；
/// 采集当前状态、把存档应用到各系统分别由 CaptureCurrentState 与各系统的 RestoreState 承担
///
/// 文件位置：Application.persistentDataPath/Saves/slot_{槽位}.json
/// 骨架阶段只负责存/取；读档后的恢复流程由各 Manager 的 RestoreState 接入
/// </summary>
public class SaveManager : MonoSingleton<SaveManager>
{
    private const string SaveFolderName = "Saves";
    private const string SlotFileFormat = "slot_{0}.json";
    private const string BackupSuffix = ".bak";
    private const string TempSuffix = ".tmp";

    /// <summary>存档写入成功（参数：槽位）</summary>
    public event Action<int> OnSaved;

    /// <summary>存档读取成功（参数：读出的数据），此时尚未应用到各系统</summary>
    public event Action<SaveData> OnLoaded;

    /// <summary>存档读取失败（参数：槽位、原因），供 UI 提示</summary>
    public event Action<int, string> OnLoadFailed;

    /// <summary>存档目录（不存在时由 Save 自动创建）</summary>
    public static string SaveDirectory => Path.Combine(Application.persistentDataPath, SaveFolderName);

    /// <summary>取指定槽位的存档文件路径</summary>
    public string GetSavePath(int slot) => Path.Combine(SaveDirectory, string.Format(SlotFileFormat, slot));

    /// <summary>槽位是否已有存档</summary>
    public bool HasSave(int slot)
    {
        if (!ValidateSlot(slot)) return false;
        return File.Exists(GetSavePath(slot));
    }

    // ==================== 存 ====================

    /// <summary>采集当前游戏状态并写入指定槽位</summary>
    public bool Save(int slot)
    {
        if (!ValidateSlot(slot)) return false;
        return Save(slot, CaptureCurrentState());
    }

    /// <summary>
    /// 写入指定存档数据：临时文件 + 备份 + 替换，
    /// 避免写入中途崩溃留下半截 JSON，同时保留上一份可用存档
    /// </summary>
    public bool Save(int slot, SaveData data)
    {
        if (!ValidateSlot(slot)) return false;
        if (data == null)
        {
            Debug.LogError($"[Save] 槽位 {slot} 的存档数据为空，已取消保存");
            return false;
        }

        string path = GetSavePath(slot);
        string tempPath = path + TempSuffix;
        string backupPath = path + BackupSuffix;

        try
        {
            Directory.CreateDirectory(SaveDirectory);

            data.version = SaveData.CurrentVersion;
            data.savedAtTicks = DateTime.UtcNow.Ticks;

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(path)) File.Copy(path, backupPath, true);
            File.Copy(tempPath, path, true);
            File.Delete(tempPath);

            Debug.Log($"[Save] 已保存到槽位 {slot}：{path}");
            OnSaved?.Invoke(slot);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 保存槽位 {slot} 失败：{e}");
            return false;
        }
        finally
        {
            // 异常时可能残留临时文件，这里统一清理
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 清理临时文件失败：{e.Message}");
            }
        }
    }

    // ==================== 读 ====================

    /// <summary>
    /// 读取指定槽位存档；正式文件损坏时自动回退 .bak 备份。
    /// 只返回数据并触发 OnLoaded，应用到各系统由调用方按流程执行
    /// </summary>
    public SaveData Load(int slot)
    {
        if (!ValidateSlot(slot)) return null;

        string path = GetSavePath(slot);
        if (!File.Exists(path))
        {
            ReportLoadFailed(slot, "存档不存在");
            return null;
        }

        SaveData data = TryRead(path, slot);
        if (data == null)
        {
            string backupPath = path + BackupSuffix;
            if (File.Exists(backupPath)) data = TryRead(backupPath, slot);
        }
        if (data == null)
        {
            ReportLoadFailed(slot, "存档与备份均无法解析");
            return null;
        }

        if (data.version > SaveData.CurrentVersion)
        {
            Debug.LogWarning($"[Save] 槽位 {slot} 的存档版本 {data.version} 高于当前 "
                             + $"{SaveData.CurrentVersion}，未识别的字段会被忽略");
        }

        OnLoaded?.Invoke(data);
        return data;
    }

    /// <summary>删除槽位存档（连同备份）</summary>
    public bool DeleteSave(int slot)
    {
        if (!ValidateSlot(slot)) return false;

        try
        {
            string path = GetSavePath(slot);
            if (File.Exists(path)) File.Delete(path);

            string backupPath = path + BackupSuffix;
            if (File.Exists(backupPath)) File.Delete(backupPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 删除槽位 {slot} 失败：{e}");
            return false;
        }
    }

    // ==================== 状态采集 ====================

    /// <summary>采集当前游戏状态（背包 / 装备 / 任务 / 玩家数值与位置）</summary>
    public SaveData CaptureCurrentState()
    {
        SaveData data = new SaveData
        {
            version = SaveData.CurrentVersion,
            savedAtTicks = DateTime.UtcNow.Ticks,
            sceneName = SceneManager.GetActiveScene().name,
        };

        CaptureInventory(data);
        CaptureEquipment(data);
        CaptureQuests(data);
        CapturePlayer(data);
        CaptureAudio(data);
        return data;
    }

    /// <summary>背包：深拷贝格子数据，不能让存档与运行时列表共享引用</summary>
    private static void CaptureInventory(SaveData data)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || inventory.Data == null) return;

        data.inventory.Clear();
        foreach (ItemStack stack in inventory.Data.slots)
        {
            if (stack == null || stack.itemId == 0 || stack.count <= 0) continue;
            data.inventory.Add(new ItemStack { itemId = stack.itemId, count = stack.count });
        }
    }

    private static void CaptureEquipment(SaveData data)
    {
        EquipmentManager equipment = EquipmentManager.Instance;
        if (equipment == null || equipment.Data == null) return;

        data.equipment = (int[])equipment.Data.slots.Clone();
    }

    private static void CaptureQuests(SaveData data)
    {
        data.quests = QuestManager.Instance != null
            ? QuestManager.Instance.CaptureState()
            : new List<QuestRuntime>();
    }

    private static void CapturePlayer(SaveData data)
    {
        PlayerProgression progression = FindObjectOfType<PlayerProgression>();
        if (progression == null)
        {
            Debug.LogWarning("[Save] 场景中未找到 PlayerProgression，玩家数据未写入存档");
            return;
        }

        Transform root = progression.transform.root;
        data.player.position = root.position;
        data.player.rotationY = root.eulerAngles.y;
        data.player.gold = progression.Gold;
        data.player.exp = progression.Exp;

        AttributeComponent attribute = progression.GetComponentInParent<AttributeComponent>();
        if (attribute == null) return;

        data.player.hp = attribute.CurrentHp;
        data.player.mp = attribute.CurrentMp;
    }

    /// <summary>音效设置：导出 AudioManager 当前的音量与静音状态</summary>
    private static void CaptureAudio(SaveData data)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null) return;

        data.audio = audio.CaptureSettings();
    }

    // ==================== 内部工具 ====================

    private static SaveData TryRead(string path, int slot)
    {
        try
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) Debug.LogError($"[Save] 槽位 {slot} 的存档解析为空：{path}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 读取存档失败：{path}\n{e}");
            return null;
        }
    }

    private void ReportLoadFailed(int slot, string reason)
    {
        Debug.LogWarning($"[Save] 槽位 {slot} 读取失败：{reason}");
        OnLoadFailed?.Invoke(slot, reason);
    }

    private static bool ValidateSlot(int slot)
    {
        if (slot >= 0) return true;

        Debug.LogError($"[Save] 非法槽位：{slot}");
        return false;
    }
}
