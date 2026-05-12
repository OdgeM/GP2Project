using System.Linq;
using UnityEngine;

public class Villain : Character
{
    float PlotProgress = 0;
    public Incident currentIncident;
    public override void GenerateHeroName()
    {
        isHero = false;

        if (isAlien)
        {
            heroName = NameManager.GetAlienVillain(characterSeed, realName);
        }
        else
        {
            heroName = NameManager.GetAliasName(characterSeed, false);
        }
    }
    public override Vector3 GenerateColour()
    {
        float h = Random.value;
        float s = Random.value;
        float v = Random.Range(0.1f,0.5f);

        return new Vector3(h, s, v);
    }

    public override void GetState(float amount)
    {
        base.GetState(amount);

        if (currentIncident != null)
        {
            Skulk();
            return;
        }

       

        if (Random.value < StatePriorities[States.Resting])
        {
            currentState = States.Resting;
        }
        else
        {


            if (PlotProgress < 1)
            {
                currentState = States.Scheming;
                
            }
            else
            {
                currentState = States.Attacking;


            }
        }

        panel.UpdateBusy();

    }

    public void Skulk()
    {
        if (patrolTarget == null)
        {
            PlotProgress = 0;
            patrolTarget = GetPatrolTarget();
            map.GenerateIncident(this, patrolTarget);
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
            case States.Scheming:
                Scheme(amount / 3);
                break;
            case States.Attacking:
                Skulk();
                break;

        }

        panel.UpdateBusy();

    }

    public void Scheme(float amount)
    {

        if(HQ != null && position != HQ.centre)
        {
            
            patrolTarget = HQ;
            Patrol();
            return;
        }
        Debug.Log(PlotProgress);
        PlotProgress += amount;

        if (PlotProgress > 1)
        {
            patrolTarget = null;
        }
    }



}
