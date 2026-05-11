using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Collections;

public class Timer : MonoBehaviour
{
    public UnityEngine.UI.Toggle pauseButton;
    public UnityEngine.UI.Toggle speed1Button;
    public UnityEngine.UI.Toggle speed2Button;
    public UnityEngine.UI.Toggle speed3Button;
    public ToggleGroup toggleGroup;
    public TextMeshProUGUI dateLabel;
    // Seconds per day
    public float speed1 = 3f;
    public float speed2 = 1.5f;
    public float speed3 = 0.75f;
    public float timeElapsed = 0;
    public float gameTimeElapsed = 1;

    public float currentSpeed = 0f;

    public int currentDate = 1;
    //private string currentButton = "Pause";

    private void Start()
    {
        StartCoroutine(TimeKeeping());
    }

    public void ButtonPressed()
    {

        switch (toggleGroup.GetFirstActiveToggle().name)
        {
            case "Speed1":
                currentSpeed = speed1;
                break;
            case "Speed2":
                currentSpeed = speed2;
                break;
            case "Speed3":
                currentSpeed = speed3;
                break;
            case "Pause":
                break;
        }


    }

    public IEnumerator TimeKeeping()
    {
        while (true)
        {
            if (!pauseButton.isOn)
            {
                yield return new WaitForSeconds(Time.deltaTime);
                timeElapsed += Time.deltaTime/currentSpeed;
                gameTimeElapsed += Time.deltaTime/currentSpeed;
                if (timeElapsed >= 1)
                {
                    currentDate++;
                    timeElapsed %= 1;
                   
                }
                float hour = timeElapsed * 24;
                float minute = hour % 1 * 60;

                dateLabel.text = "Day: " + Time2Date(gameTimeElapsed);

            }

            yield return new WaitForEndOfFrame();
        }
        
    }

    static public string Time2Date(float time)
    {
        float hoursFloat = time % 1;
        float days = time - hoursFloat;
        float hours = hoursFloat * 24;
        float minutes = hours % 1 * 60;

        int day = (int)Mathf.Floor(days);

        return day.ToString() + ", " + hours.ToString("00") + ":" + minutes.ToString("00");
    }
}
