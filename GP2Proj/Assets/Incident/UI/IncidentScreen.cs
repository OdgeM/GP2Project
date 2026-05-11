using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class IncidentScreen : MonoBehaviour
{
    public TextMeshProUGUI titleLabel;
    public string titleTemplate = "{0}\n<size=75%>{1}\n<size=60%>Time Remaining: {2}";
    public string titleTemplateOver = "{0}\n<size=75%>{1}\n<size=60%>Resolved: Day {2}";

    public HeroSprite heroSprite;
    public HeroSprite heroPlaceholder;
    public HeroSprite villainSprite;

    public TextMeshProUGUI heroName;
    public TextMeshProUGUI heroStats;
    public TextMeshProUGUI heroDetails;
    public GameObject heroWinner;

    public TextMeshProUGUI villainName;
    public TextMeshProUGUI villainStats;
    public TextMeshProUGUI villainDetails;
    public GameObject villainWinner;

    public string statTemplate = "Attack: {0}\nDefence: {1}\nHp: {2}";
    public string detailTemplate = "Real Name: {0}\nFrom: {1}";

    public Button selectHeroButton;
    public Button goButton;
    public Button abortButton;

    public Incident incident;

    public Color HeroColour;
    public Color VillainColour;

    private bool active = false;
    public GameObject results;
    public Image arrow;
    public TextMeshProUGUI damageDealt;
    public TextMeshProUGUI vs;
    public void AssignIncident(Incident _incident)
    {
        bool over = false;
        incident = _incident;


        if (_incident.state == "Ongoing")
        {
            goButton.gameObject.SetActive(true);    
            abortButton.gameObject.SetActive(true);
            results.SetActive(false);
            vs.gameObject.SetActive(true);
            heroWinner.SetActive(false);
            villainWinner.SetActive(false);
        }
        else
        {
            over = true;
            IncidentOver(incident.state == "Expired");
        }

        if (!over)
        {
            AssignTitle();
            AssignCharacter(incident.villain, false);
            active = true;

            if (incident.hero != null)
            {
                AssignHero(incident.hero, true);
            }
            else
            {
                UnassignHero();
            }
        }

       
        


       

    }

    public void UnassignHero()
    {
        if (incident.hero != null)
            incident.hero.SetAvailable(true);

        incident.hero = null;

        selectHeroButton.gameObject.SetActive(true);
        heroPlaceholder.gameObject.SetActive(true);
        heroSprite.gameObject.SetActive(false);
        heroStats.text = "";
        heroDetails.text = "";
        heroName.text = "";
    }

    private void AssignTitle(bool over = false)
    {
        string titleText = string.Format(titleTemplate, "Attack on " + incident.Target.BuildingName, incident.incidentFlavour, Timer.Time2Date(incident.length));

        if (over)
        {
            titleText = string.Format(titleTemplateOver, "Attack on " + incident.Target.BuildingName, incident.incidentFlavour, incident.dateCompleted);
        }
            
        titleLabel.text = titleText;
    }

    public void AssignHero(Hero hero, bool first = true)
    {
        if (incident.hero != null)
        {
            UnassignHero();
        }

        selectHeroButton.gameObject.SetActive(false);
        heroPlaceholder.gameObject.SetActive(false);
        heroSprite.gameObject.SetActive(true);

        AssignCharacter(hero);
        if (first)
        {
            incident.AssignHero(hero);
        }
    }


    private void AssignCharacter(Character character, bool hero = true)
    {
        HeroSprite activeSprite = villainSprite;
        TextMeshProUGUI activeName = villainName;
        TextMeshProUGUI activeStats = villainStats;
        TextMeshProUGUI activeDetails = villainDetails; 

        if (hero)
        {
            activeSprite = heroSprite;
            activeName = heroName;
            activeStats = heroStats;
            activeDetails = heroDetails;
        }

        character.SetSprite(activeSprite);
        activeName.text = character.heroName;

        string statText = string.Format(statTemplate, character.attack, character.defence, character.HP, character.maxHP);
        activeStats.text = statText;

        string detailText = string.Format(detailTemplate, character.realName, character.hometown, character.deployments);
        activeDetails.text = detailText;
        
    }

    public void Update()
    {
        if (active)
        {
            if (incident.hero != null && incident.villain != null)
                goButton.interactable = true;
            else
            {
                goButton.interactable = false;
                if (incident.hero == null && !heroPlaceholder.isActiveAndEnabled && incident.state == "Ongoing")
                {
                    UnassignHero();
                }
            }
                
            if (incident.state == "Ongoing")
            {
                AssignTitle();
            }
            
        }
    }

    public void IncidentOver(bool expired = false)
    {
        selectHeroButton.gameObject.SetActive(false);
        vs.gameObject.SetActive(false);
        goButton.gameObject.SetActive(false);
        abortButton.gameObject.SetActive(false);
        AssignTitle(true);
        if (!expired)
        {
            
            int damageInt = (int)Mathf.Floor(incident.damageDone);

            results.SetActive(true);
            damageDealt.text = damageInt.ToString() + " Damage";

            results.gameObject.SetActive(true);

            Color activeColour = VillainColour;
            arrow.transform.localScale = new Vector3(1, 1, 1);
            if (incident.Attacker == incident.hero)
            {
                heroWinner.SetActive(true);
                arrow.transform.localScale = new Vector3(-1, 1, 1);
                activeColour = HeroColour;
            }
            else
            {
                villainWinner.SetActive(true);
            }

            damageDealt.color = activeColour;
            arrow.color = activeColour;

            AssignCharacter(incident.hero);
            AssignCharacter(incident.villain, false);
        }
        else
        {
            villainWinner.SetActive(true);
            AssignCharacter(incident.villain, false);
            UnassignHero();
            heroPlaceholder.gameObject.SetActive(false);
            selectHeroButton.gameObject.SetActive(false);
        }
        
        
    }

}
