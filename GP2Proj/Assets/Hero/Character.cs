using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Character : MonoBehaviour
{
    public bool useSeed = false;
    public int characterSeed = 1234567;
    public float alienChance = 0.1f;
    public bool isAlien = false;
    

    [Header("Visuals")]

    public HeroSprite sprite;

    public float pantsChance = .5f;
    public float capeChance = .5f;
    public float insigniaChance = .5f;
    public float headwearChance = .5f;
    public float armAccentChance = .25f;
    public float baldChance = 0.1f;

    private float goldenRatio = 0.618033988749895f;

    public List<Color> humanSkinColours;
    public List<Color> hairColours;

    private Color mainColour;
    private Color accentColour;

    private Color skinColour;
    public  Color eyeColour = Color.white;

    private bool hasHair = true;
    private Color hairColour;
    private int hairIndex = 0;

    private Color bodyColour;
    private Color armColour;
    private Color legColour;
    private Color bootColour;

    private bool hasHeadwear = false;
    private Color headwearColour;
    private int headwearIndex = 0;

    private bool hasPants = false;
    private Color pantsColour;

    private bool hasCape = false;
    private Color capeColour;

    private bool hasInsignia = false;
    private Color insigniaColour;
    private int insigniaIndex = 0;

    [Header("Details")]
    public string heroName;
    public string realName;
    public int age;
    public string hometown;
    public string homeCountry; //or System for aliens
    public string backstory;

    [Header("Stats")]
    public int deployments = 0;
    public int attack = 0;
    public int defence = 0;
    public int maxHP = 0;
    public int HP;

    public CharacterPanel panel;

    public bool ready  = false;

    public bool isAvailable = true;
    public bool isDead = false;
    public Character killer;

    public void Awake()
    {
        if (useSeed)
        {
            Random.InitState(characterSeed);
        }
        else
        {
            characterSeed = Random.Range(0, 1000000);
            Random.InitState(characterSeed);
        }
    }

    public void Start()
    {
        Debug.Log("HERE");
        if (Random.value < alienChance)
        {
            isAlien = true;
            realName = NameManager.GetAlienName(characterSeed);

            hometown = NameManager.GetAlienName(characterSeed, true);
            homeCountry = hometown.Split(" ")[0];

            age = Random.Range(18, 1250);
        }
        else
        {
            City randomCity = NameManager.GetCity(characterSeed);   
            hometown = randomCity.Name;
            homeCountry = randomCity.Country;
            realName = NameManager.GetRealName(characterSeed);

            age = Random.Range(18, 50);
        }

        GenerateColours();
        GenerateHeroName();
        SetSprite(sprite);

        GenerateStats();

        //panel.AssignCharacter(this);
        ready = true;
    }

    public virtual void GenerateStats()
    {
        attack = Random.Range(1, 11);
        defence = Random.Range(1, 11);
        maxHP = Random.Range(10, 31);
        HP = maxHP;
    }

    public virtual void GenerateHeroName()
    {
        return;
    }    

    private void GenerateColours()
    {
        if (isAlien) {
            skinColour = Color.HSVToRGB(Random.value, Random.value, Random.Range(0.1f, 1f));
            hasHair = false;
        }
        else
        {
            int skinIdx = Random.Range(0, humanSkinColours.Count);
            skinColour = humanSkinColours[skinIdx];

            if (Random.value > baldChance)
            {
                int hairColourIdx = Random.Range(0, hairColours.Count);
                hairColour = hairColours[hairColourIdx];

                hairIndex = Random.Range(0, sprite.hairSprites.Count); 
            }
            else
            {
                hasHair = false;
            }
        }


        Vector3 mainColourHSV = GenerateColour();
        mainColour = Color.HSVToRGB(mainColourHSV.x,mainColourHSV.y,mainColourHSV.z);

        Vector3 accentColourHSV = GenerateColour();
        float multiplier = 1;
        if (Random.value > 0.5)
        {
            multiplier = -1;
        }

        accentColourHSV.x = Mathf.Abs((mainColourHSV.x + multiplier * goldenRatio)%1);

        if (accentColourHSV.z == 0)
        {
            Debug.Log("HUH");
        }

        accentColour = Color.HSVToRGB(accentColourHSV.x,accentColourHSV.y, accentColourHSV.z);

        if (accentColour.r + accentColour.g + accentColour.b == 0)
            Debug.Log(accentColourHSV);

        bodyColour = mainColour;

        if (Random.value < pantsChance)
        {
            hasPants = true;
            pantsColour = accentColour;
            legColour = mainColour;
            bootColour = accentColour;
        }
        else
        {
            hasPants = false;
            legColour = accentColour;
            bootColour = mainColour;
        }

        if (Random.value < capeChance)
        {
            hasCape = true;
            capeColour = accentColour;
        }

        if (Random.value < insigniaChance)
        {
            hasInsignia = true;
            insigniaColour = accentColour;

            insigniaIndex = Random.Range(0, sprite.insigniaSprites.Count);
        }

        if (Random.value < armAccentChance)
        {
            armColour = accentColour;   
        }
        else
        {
            armColour = mainColour;
        }

        if (Random.value < headwearChance)
        {
            HeadwearHandler(accentColour);
        }

    }

    private void HeadwearHandler(Color colour)
    {
        hasHeadwear = true;
        headwearColour = accentColour;

        headwearIndex = Random.Range(0, sprite.headwearSprites.Count);
    }

    public virtual Vector3 GenerateColour()
    {
        float h = Random.value;
        float s = Random.value;
        float v = Random.Range(0.1f,1f);

        return new Vector3(h, s, v);
    }

    public void SetSprite(HeroSprite _sprite)
    {
        _sprite.SetSkinColour(skinColour);
        _sprite.SetEyeColour(eyeColour);
        _sprite.SetSuitColour(bodyColour, armColour, legColour, bootColour);
        _sprite.SetHair(hasHair, hairIndex, hairColour);
        _sprite.SetHeadwear(hasHeadwear, headwearIndex, headwearColour);
        _sprite.SetInsignia(hasInsignia, insigniaIndex, insigniaColour);
        _sprite.SetPants(hasPants, pantsColour);
        _sprite.SetCape(hasCape, capeColour);
    }

    public void TakeDamage(float damage, Character attacker)
    {
        int damageInt = (int)Mathf.Floor(damage);
        HP -= damageInt;
        if (HP <= 0)
        {
            killer = attacker;
            isDead = true;
            isAvailable = false;
            HP = 0;
            panel.transform.SetAsLastSibling();
        }
        panel.TakeDamage();


    }
    public void Heal(int amount)
    {
        HP += amount;
        HP = Mathf.Clamp(HP, 0, maxHP);


        panel.TakeDamage();
    }

    public void SetAvailable(bool availabililty)
    {
        if (!isDead)
        {
            isAvailable = availabililty;
            panel.UpdateBusy();
        }
        
    }

}
