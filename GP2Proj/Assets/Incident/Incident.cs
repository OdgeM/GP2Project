using NUnit.Framework;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.VisualScripting;

public class Incident
{

    public Building Target;
    public Villain villain;
    public IncidentPanel panel;
    public float maxLength = 3;
    public float length = 3;
    public Hero hero;

    public float trustValue;

    public bool resolved = false;

    public float incidentStakes = 1;
    public string state = "Ongoing";

    public bool ReadyToResolve = false;

    public float damageDone;
    public Character Victim;
    public Character Attacker;

    public string incidentFlavour;

    public float dateCompleted;
    public Incident(Building target, Villain _villain)
    {
        Target = target;
        villain = _villain;
        villain.SetAvailable(false);
        trustValue = ((villain.attack + villain.defence) / 2) + Random.value ;
        /*
        if (villain.isAlien)
        {
            string flavour = alienFlavour[Random.Range(0, alienFlavour.Length)];
            incidentFlavour = string.Format(flavour, villain.heroName.Trim(), Target.BuildingName, villain.hometown);
        }
        else
        {
            string connector = "at";

    

            string flavour = villainFlavour[Random.Range(0, villainFlavour.Length)];
            incidentFlavour = string.Format(flavour, villain.heroName, connector, Target.BuildingName);
        }
        */
         
    }

    public void AssignHero(Hero _hero)
    {
        hero = _hero;
        hero.SetAvailable(false);
    }

    public void passTime(float timePassed)
    {
        length -= timePassed; 
    }

    public bool ResolveIncident(float date)
    {
        

        dateCompleted = date;
        hero.deployments++;
        villain.deployments++;
        bool result = false;

        float heroAttack = (float)hero.attack + Random.Range(-2.5f, 2.5f);
        float heroDefence = (float)hero.defence + Random.Range(-2.5f, 2.5f);

        float villainAttack = (float)villain.attack + Random.Range(-2.5f, 2.5f);
        float villainDefence = (float)villain.defence + Random.Range(-2.5f, 2.5f);

        float heroScore = heroAttack - villainDefence + 15;
        float villainScore = villainAttack - villainDefence + 15;

        float heroWinChance = heroScore / (villainScore + heroScore);

        Victim = hero;
        Attacker = villain;
        float winningScore = villainScore;

        //Debug.Log(heroWinChance);
        float value = Random.value;
        //Debug.Log(value);
        if (value < heroWinChance)
        {
            //Debug.Log("HERo0");
            Victim = villain;
            Attacker = hero;
            winningScore = heroScore;
            result = true;
        }

        

        if (winningScore / 2 <= incidentStakes)
        {
            damageDone = incidentStakes;
        }
        else
        {
            damageDone = Random.Range(incidentStakes, winningScore/2);
        }

        

        Victim.TakeDamage(damageDone, Attacker);
        Target.Deactivate();
        state = "Over";
        villain.currentIncident = null;
        hero.incident = null;
        resolved = true;
        return result;
    }

    public void Expire(float date)
    {
        villain.deployments++;
        villain.SetAvailable(true);
        dateCompleted = date;
        Target.Deactivate();
        state = "Expired";
    }

    static string[] villainFlavour =
    {
        "{0} is attacking {2}!",
        "{0} is causing chaos {1} {2}!",
        "{0} is running amok {1} {2}!",
        "{0} is enacting a dastardly plan {1} {2}!"
    };

    static string[] alienFlavour =
    {
        "Aliens from the planet {2} are attacking {1}!",
        "Extraterrestrials lead by {0} are conducting an assault of {1}!"
    };

}
