using TMPro;
using UnityEngine;

public class PlayerResultUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI capturedTilesText;
    
    public void SetText(string name, string count)
    {
        playerNameText.text = name;
        capturedTilesText.text = count;
    }
}
