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

    private List<Incident> currentIncidents = new();
    private List<Incident> resolvedIncidents = new();

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

    public bool isGenerated = false;

    public IncidentScreen incidentScreen;
    public CharacterScreen characterScreen;

    public Button mapButton;

    private string selectedScreen = "Map";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    float timePassed;
    float hourCount = 1;

    async public void GenerateMap(int Segments, bool grid, bool coastal, bool river, MapConfiguration mapConfiguration )
    {
        CreateLineup(); 
        await map.Generate(Segments, grid, coastal, river, Random.Range(0, 10000));
        mapConfiguration.Generated();
    }

    public void GenerationDone()
    {
        isGenerated = true;
        lineupManager.Confirm();
        lineupManager.HealCharacters();
    }

    public void CreateLineup()
    {
        lineupManager.CreateHeroes();
    }


    // Update is called once per frame
    void Update()
    {
        
        if (!timer.pauseButton.isOn && isGenerated)
        {
           


            if (timer.timeElapsed < prevTime)
            {
                lineupManager.HealCharacters();

                incidentsToday = 0;
                timePassed = 1 - prevTime + timer.timeElapsed;
            }
            else
            {
                timePassed = timer.timeElapsed - prevTime;
            }

            lineupManager.MoveCharacters(timePassed);

            hourCount += timePassed * 24;
            if (hourCount >= 1)
            {
                
                hourCount = hourCount - 1;
            }

            
            List<Incident> activeIncidents = currentIncidents.Where(p => !p.resolved).ToList();
            foreach(Incident i in activeIncidents)
            {
                if (i.ReadyToResolve)
                {
                    i.ResolveIncident(timer.currentDate);
                    continue;
                }
                
                i.passTime(timePassed);

                if (i.length < 0)
                {
                    i.Expire(timer.currentDate);
                }
            }

            map.Clock(timePassed * 24);
            prevTime = timer.timeElapsed;
        }
        else
        {
            timePassed = 0;
        }
    }

    public void CreateIncident(Incident incident)
    {
      
        
        currentIncidents.Add(incident);
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

    public void ResolveIncident(Incident incident)
    {


     
        
        bool won = incident.ResolveIncident(timer.currentDate);

        float multiplier = -1;
        if (won)
        {
            multiplier = 1;
        }
        resourceManager.TrustChange(multiplier * incident.trustValue);

        StartCoroutine(Fight(incident.hero, incident.villain));
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



