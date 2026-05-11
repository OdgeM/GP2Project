using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IncidentPanel : MonoBehaviour
{
    public GameObject locationNode;
    public TextMeshProUGUI incidentName;
    public TextMeshProUGUI villainName;
    public TextMeshProUGUI timeRemaining;
    public HeroSprite sprite;

    public Button button;

    public Incident incident;
    public Villain villain;
    public bool resolved = false;
    public RectTransform rectTransform;

    public Color warningColour = Color.orange;
    public Color winColour = Color.green;
    public Color lossColour = Color.red;
    public void AssignIncident(Incident _incident, HeroSprite _sprite)
    {
        incident = _incident;
        villain = incident.villain;
        sprite = _sprite;
        incidentName.text = "Attack on " + incident.Target.BuildingName;

        StartCoroutine(waitForVillain());
    }

    public void Resolve(bool won)
    {
        resolved = true;

        string result = "Lost...";
        Color colour = lossColour;
        if (won)
        {
            colour = winColour;
            result = "Won!";
        }

        this.GetComponent<Image>().color = new Color(0, 0, 0, .75f);
        timeRemaining.text = result;
        timeRemaining.color = colour;
        transform.SetAsLastSibling();

    }

    public void Expire()
    {
        resolved = true;
        timeRemaining.text = "Expired...";
        timeRemaining.color = lossColour;
        this.GetComponent<Image>().color = new Color(0, 0, 0, .75f);
        transform.SetAsLastSibling();
    }

    public IEnumerator waitForVillain()
    {
        while (!villain.ready)
        {
            yield return new WaitForEndOfFrame();
        }
        villain.SetSprite(sprite);

        villainName.text = "by " + villain.heroName;
    }

    public void Update()
    {
        if (!resolved)
        {
            if (incident != null)
            {
                float time = incident.length;

                timeRemaining.text = "Time Remaining: " + Timer.Time2Date(time);
                timeRemaining.color = Color.Lerp(warningColour, Color.white, time / incident.maxLength);

            }
        }
        
    }

}
