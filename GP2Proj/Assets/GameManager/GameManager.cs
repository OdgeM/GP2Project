using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public Timer timer;
    public LineupManager lineupManager;
    public Map map;

    public ResourceManager resourceManager;

    public float daysPerIncident = 1; // Average
    public int maxIncidentsPerDay = 3;
    private int incidentsToday = 0;
    private float prevTime = 0;
    private float dryDays = 0;
    public float dryDayMultiplier;

    private List<IncidentPanel> currentIncidents = new();
    private List<IncidentPanel> resolvedIncidents = new();

    public IncidentMenu incidentMenu;
    public GameObject incidentPanelContent;
    public GameObject incidentPanelPrefab;
    public GameObject spritePrefab;

    public CharacterMenu heroMenu;
    public CharacterMenu villainMenu;

    public float sidePanelStack = 300;
    public float sidePanelActive = 0;
    public float screenStack = 334;
    public float screenActive = -212.25f;

    public ToggleGroup sidePanelButtons;
    public Toggle incidentButton;
    public Toggle villainButton;
    public Toggle heroButton;

    public Toggle pauseButton;

    public IncidentScreen incidentScreen;
    public CharacterScreen characterScreen;

    public Button mapButton;

    private string selectedScreen = "Map";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    float timePassed;

    async public void GenerateMap(int Segments, bool grid, bool coastal, bool river, MapConfiguration mapConfiguration )
    {
        CreateLineup();
        await map.Generate(Segments, grid, coastal, river, Random.Range(0, int.MaxValue));
        mapConfiguration.Generated();
    }

    public void CreateLineup()
    {
        lineupManager.CreateHeroes();
    }


    // Update is called once per frame
    void Update()
    {
        /*
        if (!timer.pauseButton.isOn)
        {
           
            if (timer.timeElapsed < prevTime)
            {
                resourceManager.addResources(1);
                lineupManager.HealCharacters();

                incidentsToday = 0;
                timePassed = 1 - prevTime + timer.timeElapsed;
            }
            else
            {
                timePassed = timer.timeElapsed - prevTime;
            }

            List<IncidentPanel> activeIncidents = currentIncidents.Where(p => !p.resolved).ToList();
            foreach(IncidentPanel incidentPanel in activeIncidents)
            {
                Incident incident = incidentPanel.incident;
                incident.passTime(timePassed);

                if (incident.length < 0)
                {
                    incident.Expire(timer.currentDate);

                    resourceManager.TrustChange(incident.trustValue * -2);

                    IncidentPanel panel = currentIncidents.Where(inc => inc.incident == incident).FirstOrDefault();
                    panel.Expire();
                    if (incidentScreen.incident == incident)
                    {
                        incidentScreen.IncidentOver(true);
                    }
                }
            }

            if (incidentsToday < maxIncidentsPerDay)
            {
                float incidentChance = timePassed / daysPerIncident;
                Debug.Log(incidentChance);
                if (Random.value < incidentChance)
                {
                    incidentsToday++;
                    CreateIncident();
                }
                else
                {
                    dryDays += timePassed;
                }
            }

            prevTime = timer.timeElapsed;
        }
        else
        {
            timePassed = 0;
        }*/
    }

    public void CreateIncident()
    {
        Debug.Log("INCIDENT");
        Villain incidentVillain = lineupManager.SelectVillain();
        Incident incident = map.GenerateIncident(incidentVillain);

        IncidentPanel newPanel = Instantiate(incidentPanelPrefab, incidentPanelContent.transform).GetComponent<IncidentPanel>();
        HeroSprite newSprite = Instantiate(spritePrefab, newPanel.locationNode.transform).GetComponent<HeroSprite>();

        incidentMenu.PlacePanel(newPanel);
        newPanel.AssignIncident(incident, newSprite);

        newPanel.button.onClick.AddListener(delegate { incidentSelected(newPanel.incident); });
        
        currentIncidents.Add(newPanel);
    }

    public void SelectHero()
    {
        heroButton.isOn = true;
    }

    public void HeroSelected(Hero hero)
    {
        if (selectedScreen == "Incident")
        {
            if (incidentScreen.incident.state != "Over")
            {
                if (hero.isAvailable)
                {
                    incidentScreen.AssignHero(hero);
                }
                else
                {
                    if (incidentScreen.incident.hero == hero)
                    {
                        incidentScreen.UnassignHero();
                    }
                    else
                    {
                        CharacterSelected(hero);
                    }
         

                    
                }
            }
            else
            {
                CharacterSelected(hero);
            }


        }
        else
        {
            CharacterSelected(hero);
        }
    }

    public void VillainSelected(Villain villain)
    {
        CharacterSelected(villain, false);
    }

    public void CharacterSelected(Character character, bool hero = true)
    {
        pauseButton.isOn = true;
        map.GetComponent<RectTransform>().anchoredPosition = new Vector2(map.GetComponent<RectTransform>().anchoredPosition.x, screenStack);
        incidentScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(incidentScreen.GetComponent<RectTransform>().anchoredPosition.x, screenStack);
        characterScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(incidentScreen.GetComponent<RectTransform>().anchoredPosition.x, screenActive);
        characterScreen.AssignCharacter(character, hero);
        selectedScreen = "Character";
        mapButton.interactable = true;
    }

    public void incidentSelected(Incident incident)
    {
        pauseButton.isOn = true;

        map.GetComponent<RectTransform>().anchoredPosition = new Vector2(map.GetComponent<RectTransform>().anchoredPosition.x, screenStack);
        characterScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(characterScreen.GetComponent<RectTransform>().anchoredPosition.x, screenStack);

        incidentScreen.AssignIncident(incident);
        incidentScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(incidentScreen.GetComponent<RectTransform>().anchoredPosition.x, screenActive);

        selectedScreen = "Incident";
        mapButton.interactable = true;
    }

    public void ResolveIncident()
    {


        Incident incident = incidentScreen.incident;
        IncidentPanel panel = currentIncidents.Where(inc => inc.incident == incident).FirstOrDefault();
        
        bool won = incident.ResolveIncident(timer.currentDate);

        float multiplier = -1;
        if (won)
        {
            multiplier = 1;
        }
        resourceManager.TrustChange(multiplier * incident.trustValue);

        StartCoroutine(Fight(incident.hero, incident.villain));
        panel.Resolve(won);
        incidentScreen.IncidentOver();
    }

    public IEnumerator Fight(Hero hero, Villain villain)
    {
        float time = 0;

        while (time < 1)
        {
            time += timePassed;
            yield return new WaitForEndOfFrame();
        }

        hero.SetAvailable(true);
        villain.SetAvailable(true);

    }

    public void HireHero()
    {
        resourceManager.spendResources(500);
        lineupManager.CreateHero();
    }

    public void selectMapScreen()
    {
        selectedScreen = "Map";
        incidentScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(incidentScreen.GetComponent<RectTransform>().anchoredPosition.x, screenStack);
        characterScreen.GetComponent<RectTransform>().anchoredPosition = new Vector2(characterScreen.GetComponent<RectTransform>().anchoredPosition.x, screenStack);
        map.GetComponent<RectTransform>().anchoredPosition = new Vector2(map.GetComponent<RectTransform>().anchoredPosition.x, screenActive);

        mapButton.interactable = false;
    }

    public void SidePanelButtonPressed()
    {
        heroMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelStack, heroMenu.GetComponent<RectTransform>().anchoredPosition.y);
        incidentMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelStack, incidentMenu.GetComponent<RectTransform>().anchoredPosition.y);
        villainMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelStack, villainMenu.GetComponent<RectTransform>().anchoredPosition.y);

        switch (sidePanelButtons.GetFirstActiveToggle().name)
        {
            case "Incidents":
                incidentMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelActive, incidentMenu.GetComponent<RectTransform>().anchoredPosition.y);
                break;
            case "Heroes":
                heroMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelActive, heroMenu.GetComponent<RectTransform>().anchoredPosition.y);
                break;
            case "Villains":
                villainMenu.GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelActive, villainMenu.GetComponent<RectTransform>().anchoredPosition.y);
                break;
        }
    }

}



