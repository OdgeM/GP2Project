using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    public TextMeshProUGUI trustLabel;
    public float currentTrust=0;

    public TextMeshProUGUI resourceLabel;
    public TextMeshProUGUI deltaLabel;
    public float currentResources=500;
    public float deltaResources;

    public float resourceRatio = .71f;

    public Button hireButton;

    public void Start()
    {
        TrustChange(75);
        resourceLabel.text = "Res: " + currentResources.ToString("0");
    }

    public void TrustChange(float amount)
    {
        currentTrust += amount;
        currentTrust = Mathf.Clamp(currentTrust, 0, 100);

        deltaResources = currentTrust * resourceRatio;

        trustLabel.text = "Trust: " + currentTrust.ToString("0.00") + "%";
        deltaLabel.text = "+" + deltaResources.ToString("0") + "/ Day";

    }
    public void addResources(float time)
    {
        currentResources += time*deltaResources;
        resourceLabel.text = "Res: " + currentResources.ToString("0");
        if (currentResources > 500)
        {
            hireButton.interactable = true;
        }

    }
    public void spendResources(float amount)
    {
        currentResources -= amount;
        resourceLabel.text = "Res: " + currentResources.ToString("0");

        if (currentResources < 500)
        {
            hireButton.interactable = false;
        }
    }
}
