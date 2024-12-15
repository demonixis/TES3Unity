using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class OpenLinkOnClick : MonoBehaviour
{
    public string url;

    private void Start()
    {
        if (TryGetComponent(out Button button))
            button.onClick.AddListener(OpenURL);
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out Button button))
            button.onClick.RemoveListener(OpenURL);
    }

    public void OpenURL()
    {
        Application.OpenURL(url);
    }
}