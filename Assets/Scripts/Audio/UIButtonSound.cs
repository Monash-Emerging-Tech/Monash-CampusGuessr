/* ===============================
 * Written by salma
 * Last Modified: 09 / 05 / 2026
 * ===============================
*/
using UnityEngine;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour
{
    private Button button;
    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }
}