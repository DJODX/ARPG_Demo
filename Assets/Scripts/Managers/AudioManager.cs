using UnityEngine;

/// <summary>
/// 音效管理器（全局单例）
/// 统一管理 BGM、音效的播放、音量控制和静音
/// </summary>
public class AudioManager : MonoSingleton<AudioManager>
{
    [Header("背景音乐")]
    public AudioClip bgmClip;

    [Header("战斗音效")]
    public AudioClip[] attackSwingClips;
    public AudioClip hitReceivedClip;
    public AudioClip enemyDeathClip;


    [Header("UI 音效")]
    public AudioClip buttonClickClip;

    [Header("音量设置")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("静音")]
    public bool bgmMuted;
    public bool sfxMuted;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;

    protected override void OnSingletonAwake()
    {
        // BGM AudioSource
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.clip = bgmClip;

        // SFX AudioSource
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;

        // 自动播放 BGM
        if (bgmClip != null && !bgmMuted)
            PlayBGM();
    }

    private void Update()
    {
        // 运行时同步音量
        _bgmSource.volume = bgmMuted ? 0f : bgmVolume;
    }

    // ==================== BGM ====================

    /// <summary>
    /// 开始播放背景音乐
    /// </summary>
    public void PlayBGM()
    {
        if (bgmClip == null) return;
        _bgmSource.Play();
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM()
    {
        _bgmSource.Pause();
    }

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM()
    {
        _bgmSource.UnPause();
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    /// <summary>
    /// 切换 BGM
    /// </summary>
    public void ChangeBGM(AudioClip newClip)
    {
        if (newClip == null || newClip == bgmClip) return;
        bgmClip = newClip;
        _bgmSource.clip = bgmClip;
        PlayBGM();
    }

    // ==================== SFX ====================

    /// <summary>
    /// 播放单个音效
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxMuted) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// 从数组中随机选一个播放
    /// </summary>
    public void PlayRandomSFX(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFX(clips[Random.Range(0, clips.Length)]);
    }

    /// <summary>
    /// 在世界空间位置播放音效（保留 3D 空间感，音量受全局 sfxVolume 与静音管理）
    /// </summary>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null || sfxMuted) return;
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume * Mathf.Clamp01(volume));
    }

    /// <summary>
    /// 在世界空间位置从数组中随机选一个播放
    /// </summary>
    public void PlayRandomSFXAtPoint(AudioClip[] clips, Vector3 position, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySFXAtPoint(clips[Random.Range(0, clips.Length)], position, volume);
    }

    // ==================== 音量 / 静音控制 ====================

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// 切换音效静音
    /// </summary>
    public void ToggleSFXMute()
    {
        sfxMuted = !sfxMuted;
    }

    /// <summary>
    /// 切换 BGM 静音
    /// </summary>
    public void ToggleBGMMute()
    {
        bgmMuted = !bgmMuted;
    }
}
