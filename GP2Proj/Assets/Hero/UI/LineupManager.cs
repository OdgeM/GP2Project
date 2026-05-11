using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineupManager : MonoBehaviour
{
    public List<Hero> heroLineup;
    public int startingHeroSize = 3;
    public GameObject heroPrefab;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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


    public Hero CreateHero()
    {
        CharacterPanel newPanel = Instantiate(characterPanelPrefab, heroPanelContent.transform).GetComponent<CharacterPanel>();

        Hero newHero = Instantiate(heroPrefab, newPanel.locationNode.transform).GetComponent<Hero>();   
        newHero.panel = newPanel;

        heroLineup.Add(newHero);

        heroMenu.PlacePanel(newPanel);

        newPanel.AssignCharacter(newHero);  

        newPanel.button.onClick.AddListener(delegate { gameManager.HeroSelected(newHero); });
        

        return newHero;
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
        List<Hero> aliveHeroes = heroLineup.Where(h => !h.isAvailable).ToList();
        List<Villain> aliveVillain = villainLineup.Where(h => !h.isAvailable).ToList();

        foreach(Hero h in aliveHeroes)
        {
            h.Heal( Random.Range(0, 4));
        }

        foreach(Villain v in aliveVillain)
        {
            v.Heal(Random.Range(0, 4));
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
