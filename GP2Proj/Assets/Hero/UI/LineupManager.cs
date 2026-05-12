using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineupManager : MonoBehaviour
{
    public List<Hero> heroLineup;
    public int startingHeroSize = 3;
    public GameObject heroPrefab;
    public Map map; 

    public List<Villain> villainLineup;
    private List<Villain> activeVillains = new();
    public int startingVillainSize = 2;
    public int idealVillains = 5;
    public int totalIncidents = 0;
    public GameObject villainPrefab;

    public GameObject characterPanelPrefab;
    public CharacterMenu heroMenu;
    public GameObject heroPanelContent;

    public CharacterMenu villainMenu;
    public GameObject villainPanelContent;


    public GameObject heroStack;
    public GameObject villainStack;

    public GameManager gameManager;

    public Hero selectedHero;

    public Material blipMaterial;
     public Material highlightMaterial;

    public void CreateHeroes()
    {
        villainLineup.Clear();
        heroLineup.Clear();
        int childCount = heroPanelContent.transform.childCount;
        for (int i = 0; i < childCount; i++) {
            DestroyImmediate(heroPanelContent.transform.GetChild(0).gameObject);
            }

        childCount = villainPanelContent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            DestroyImmediate(villainPanelContent.transform.GetChild(0).gameObject);
        }
        /*while (heroPanelContent.transform.childCount > 0)
        {
            Destroy(heroPanelContent.transform.GetChild(0).gameObject);
        }*/
        /*
        while (villainPanelContent.transform.childCount != 0)
        {
            Destroy(villainPanelContent.transform.GetChild(0).gameObject);
        }*/

        //InitialiseLineup();
        InitialiseLineup();
    }

    public void InitialiseLineup()
    {
        for (int i = 0; i < startingHeroSize; i++)
        {
            CreateHero();
        }

        for (int i = 0; i<startingVillainSize; i++)
        {
            CreateVillain();
        }
    }
    public void Confirm()
    {
        foreach (var hero in heroLineup)
        {
            map.AddHero(hero);
            hero.map = map;
        }

        foreach (var villain in villainLineup)
        {
            villain.map = map;
        }
    }

    public Hero CreateHero()
    {
        //Debug.Log("HERE");
        CharacterPanel newPanel = Instantiate(characterPanelPrefab, heroPanelContent.transform).GetComponent<CharacterPanel>();

        Hero newHero = Instantiate(heroPrefab, heroPanelContent.transform).GetComponent<Hero>();   
        newHero.panel = newPanel;

        heroLineup.Add(newHero);

        heroMenu.PlacePanel(newPanel);

        newPanel.AssignCharacter(newHero);  

        newPanel.button.onClick.AddListener(delegate { SelectHero(newHero); });
        

        return newHero;
    }

    public void SelectHero(Hero newHero)
    {
        if (selectedHero != null)
        {
            selectedHero.icon.GetComponent<CanvasRenderer>().SetMaterial(blipMaterial,0);
        }

        if (newHero == selectedHero)
        {
            selectedHero = null;
        }
        else
        {
            selectedHero = newHero;
            selectedHero.icon.GetComponent<CanvasRenderer>().SetMaterial(highlightMaterial, 0);
        }
    }

    private Villain CreateVillain()
    {
        CharacterPanel newPanel = Instantiate(characterPanelPrefab, villainPanelContent.transform).GetComponent<CharacterPanel>();

        Villain newVillain = Instantiate(villainPrefab, newPanel.locationNode.transform).GetComponent<Villain>();
        newVillain.panel = newPanel;

        villainLineup.Add(newVillain);

        villainMenu.PlacePanel(newPanel);

        newPanel.AssignCharacter(newVillain);
        newPanel.button.onClick.AddListener(delegate { gameManager.VillainSelected(newVillain); });

        return newVillain;
    }

    public void HealCharacters()
    {
        List<Hero> aliveHeroes = heroLineup.Where(h => h.currentState != Character.States.Dead).ToList();
        List<Villain> aliveVillain = villainLineup.Where(h => h.currentState != Character.States.Dead).ToList();

        foreach(Hero h in aliveHeroes)
        {
            h.GetState(Random.value );
        }

        foreach(Villain v in aliveVillain)
        {
            v.GetState(Random.value);
        }
    }

    public void MoveCharacters(float time)
    {
        List<Hero> aliveHeroes = heroLineup.Where(h => h.currentState != Character.States.Dead).ToList();
        List<Villain> aliveVillain = villainLineup.Where(h => h.currentState != Character.States.Dead).ToList();

        foreach (Hero h in aliveHeroes)
        {
            h.Move(Random.value, time);
        }

        foreach (Villain v in aliveVillain)
        {
            v.Move(Random.value, time);
        }
    }

    public Villain SelectVillain()
    {
        List<Villain> availableVillains = villainLineup.Where(villain => villain.isAvailable).ToList();

        float newVillainChance;

        if (villainLineup.Count < idealVillains)
        {
            newVillainChance = (idealVillains - villainLineup.Count)/idealVillains;
        }
        else 
        {
           newVillainChance =  1 / villainLineup.Count;
        }

        if (totalIncidents < 3)
        {
            newVillainChance = 0;
        }

        if (Random.value <= newVillainChance || availableVillains.Count ==  0)
        {
            // Create new Villain
            Villain newVillain = CreateVillain();
            activeVillains.Add(newVillain);
            return newVillain;
            
        }
        else
        {
            Villain chosenVillain = availableVillains[Random.Range(0, availableVillains.Count)];
            if (totalIncidents < 3)
            {
                totalIncidents++;
            }
            activeVillains.Add(chosenVillain);
            return chosenVillain;    
        }

    }


    public void DeactivateVillain(Villain villain)
    {
        activeVillains.Remove(villain); 
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
