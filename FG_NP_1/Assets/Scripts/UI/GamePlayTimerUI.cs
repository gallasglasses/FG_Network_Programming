using UnityEngine;
using UnityEngine.UI;

public class GamePlayTimerUI : MonoBehaviour
{
    [SerializeField] private Image timerImage;

    void Update()
    {
        timerImage.fillAmount = GameManager.Instance.GetGamePlayingTimePersent();
    }
}
