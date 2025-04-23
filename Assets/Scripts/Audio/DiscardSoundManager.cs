using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCRGame.Common;   // GameActionType, CallBlockType

namespace MCRGame.Audio
{
    /// <summary>
    /// 버림(Discard) 효과음을 즉시 재생하는 매니저
    /// </summary>
    public class DiscardSoundManager : MonoBehaviour
    {
        public static DiscardSoundManager Instance { get; private set; }

        [Header("Audio Source (즉시 재생용)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Discard Clip")]
        [SerializeField] private AudioClip discardClip;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource가 없으면 자동 생성
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        /// <summary>
        /// 즉시 재생용 AudioSource 볼륨 (0~1)
        /// </summary>
        public float Volume
        {
            get => audioSource.volume;
            set => audioSource.volume = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 즉시 버림 효과음 재생
        /// </summary>
        public void PlayDiscardSound()
        {
            if (discardClip == null || audioSource == null) return;
            audioSource.PlayOneShot(discardClip);
        }
    }
}
