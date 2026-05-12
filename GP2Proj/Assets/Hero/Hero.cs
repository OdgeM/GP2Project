using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Hero : Character
{
    public TextMeshProUGUI nameLabel;
    public GameObject icon;
    public Incident incident;
    
    public override void GenerateHeroName()
    {
        StatePriorities[States.Patrolling] = 0.5f;
        StatePriorities[States.Training] = 0.5f;
        StatePriorities[States.Resting] = 0;

        isHero = true;
        if (isAlien)
        {
            heroName = realName;
        }
        else
        {
            heroName = NameManager.GetAliasName(characterSeed);
        }

    }

    public override void GetState(float amount)
    {
        base.GetState(amount);


        if (incident != null)
        {
            return;
        }

        if (Random.value < StatePriorities[States.Resting])
        {
            currentState = States.Resting;
           
        }
        else
        {
            var incidents = map.buildings.Where(n => n.incident != null && n.incident.hero == null);
            if (incidents.Count() > 0)
            {
                currentState = States.Responding;
                incident = incidents.First().incident;
                incident.hero = this;
     
            }
            else
            {
                if (Random.value < StatePriorities[States.Training])
                {
                    currentState = States.Training;
                }
                else
                {
                    currentState = States.Patrolling;
                    StatePriorities[States.Training] += .01f;
                    StatePriorities[States.Patrolling] -= .01f;
                }
            }

            
        }

        panel.UpdateBusy();


    }

    public override void Move(float amount, float TimePassed)
    {
        

        switch (currentState)
        {
            case States.Resting:
                Heal(amount / 10);
                break;
            case States.Training:
                Train(amount / 10);
                break;
            case States.Patrolling:
                Patrol();
                break;
            case States.Responding:
                Respond();
                break;

        }

        panel.UpdateBusy();

    }

    public override void SetLocation(Vector2 location)
    {
        base.SetLocation(location);
        icon.transform.localPosition = new Vector3(location.x, location.y, -1);
    }


    public void Respond()
    {
        if (incident == null)
        {
            return;
        }
        patrolTarget = incident.Target;
        Patrol();

        if (Vector2.Distance(incident.Target.centre, position) <= 1)
        {
            incident.ReadyToResolve = true;
        }
    }



    public void Clicked()
    {
        icon.GetComponent<CanvasRenderer>().GetMaterial().SetColor("_Color", Color.gold);
    }



    public void Train(float amount)
    {
        if (HQ != null && position != HQ.centre)
        {
            patrolTarget = HQ;
            Patrol();
            return;
        }
        if (Random.value < amount)
        {
            maxHP += 1;
            HP += 1;
        }
        else if (Random.value < amount)
        {
            attack += 1;
        }
        else if(Random.value < amount)
        {
            defence += 1;
        }

        StatePriorities[States.Training] -= .01f;
        StatePriorities[States.Patrolling] += .01f;

        panel.UpdateStats();
    }
}
