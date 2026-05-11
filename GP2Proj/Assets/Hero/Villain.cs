using UnityEngine;

public class Villain : Character
{
    public override void GenerateHeroName()
    {
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
}
