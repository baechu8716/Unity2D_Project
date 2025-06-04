using System.Collections.Generic;
using UnityEngine;


public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource; // 효과음은 여러 개 동시에 재생될 수 있도록 여러 AudioSource를 관리하거나, PlayOneShot을 주로 사용

    [Header("Audio Clips")]
    public AudioClip mainBgm; // 메인 배경음악
    public AudioClip gameSceneBgm; // 게임씬 배경음악

    public AudioClip playerAttackSound;
    public AudioClip playerHitSound;
    public AudioClip bossAttackSound;
    public AudioClip bossHitSound;
    public AudioClip buttonClickSound; // UI 버튼 클릭음

    private Dictionary<string, AudioClip> _sfxClips = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 방지
            return;
        }

        // 효과음 클립들을 Dictionary에 등록 (더 체계적인 관리를 위해)
        if (playerAttackSound != null) _sfxClips["PlayerAttack"] = playerAttackSound;
        if (playerHitSound != null) _sfxClips["PlayerHit"] = playerHitSound;
        if (bossAttackSound != null) _sfxClips["BossAttack"] = bossAttackSound;
        if (bossHitSound != null) _sfxClips["BossHit"] = bossHitSound;
        if (buttonClickSound != null) _sfxClips["ButtonClick"] = buttonClickSound;
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySFX(string clipName)
    {
        if (sfxSource != null && _sfxClips.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip); // PlayOneShot은 여러 효과음이 겹쳐서 재생될 수 있게 함
        }
    }

    // AudioClip 객체로 직접 효과음 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // 특정 상황별 효과음 재생 메서드
    public void PlayPlayerAttackSound() => PlaySFX("PlayerAttack");
    public void PlayPlayerHitSound() => PlaySFX("PlayerHit");
    public void PlayBossAttackSound() => PlaySFX("BossAttack");
    public void PlayBossHitSound() => PlaySFX("BossHit");
    public void PlayButtonClickSound() => PlaySFX("ButtonClick");


}

