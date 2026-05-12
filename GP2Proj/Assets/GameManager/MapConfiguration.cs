using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MapConfiguration : MonoBehaviour
{
    public Slider sizeSlider;
    public TextMeshProUGUI sizeLabel;
    public Toggle gridBased;
    public Toggle coastalToggle;
    public Toggle riverToggle;
    public Button goButton;
    public GameManager gm;
    public GameObject timerPanel; 

    public Button rerollButton;
    public Button confirmButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnSliderChange()
    {

        sizeLabel.text = "City Size: " + (int)sizeSlider.value + " roads";
    }

    public void OnGo()
    {
        //Debug.Log(sizeSlider.value);
        gm.GenerateMap((int)(sizeSlider.value), gridBased.isOn, coastalToggle.isOn, riverToggle.isOn, this);
        goButton.interactable = false;
        confirmButton.interactable = false;
        rerollButton.interactable = true;
    }

    public void Generated()
    {
        goButton.interactable = true;

        confirmButton.interactable = true;
    }

    public void Reroll()
    {
        gm.CreateLineup();
    }

    public void Confirm()
    {
        gm.GenerationDone();
        timerPanel.SetActive(true);
        gameObject.SetActive(false);
    }


    // Update is called once per frame
    void Update()
    {
        
    }



}
