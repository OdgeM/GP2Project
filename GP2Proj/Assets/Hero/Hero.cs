using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Hero : Character
{
    public override void GenerateHeroName()
    {
        if (isAlien)
        {
            heroName = realName;
        }
        else
        {
            heroName = NameManager.GetAliasName(characterSeed);
        }
    }
}
