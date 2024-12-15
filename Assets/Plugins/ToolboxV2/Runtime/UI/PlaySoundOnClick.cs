using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class PlaySoundOnClick : MonoBehaviour
{
    public AudioClip sound;

    private void Start()
    {
        if (TryGetComponent(out Button button))
            button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out Button button))
            button.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        AudioSource.PlayClipAtPoint(sound, Vector3.zero);
    }
}