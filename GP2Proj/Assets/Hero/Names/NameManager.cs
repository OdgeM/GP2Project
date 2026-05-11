using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public struct City
{
    public string Name;
    public string Country;

    public City(string name, string country)
    {
        Name = name; Country = country;
    }
}

public class NameManager : MonoBehaviour
{

    

    public static int nameSeed = 0;
    public static int planetSeed = 1234567;
    public TextAsset firstNameAsset;
    public TextAsset lastNameAsset;
    public TextAsset alienVillainSuffixAsset;
    public static float alienSuffixChance = .5f;

    public TextAsset genericPrefixAsset;
    public TextAsset genericSuffixAsset;
    public TextAsset villainPrefixAsset;
    public TextAsset villainSuffixAsset;
    public TextAsset heroPrefixAsset;
    public TextAsset heroSuffixAsset;
    public TextAsset cityAsset;


    public static List<string> firstNames;
    public static List<string> lastNames;
    public static List<string> alienVillainSuffix;
    public static List<string> genericPrefix;
    public static List<string> genericSuffix;
    public static List<string> villainPrefix;
    public static List<string> villainSuffix;
    public static List<string> heroPrefix;
    public static List<string> heroSuffix;
    public static List<City> cities;

    private static List<string> existingSystems = new();
    public static float newSystemChance = 0.5f;

    private static string[] firstSound =
    {
        "k",
        "t",
        "z",
        "r",
        "l",
        "s",
        "th",
        "q",
        "x",
        "z",
        ""
    };

    private static string[] vowel =
    {
        "a",
        "e",
        "i",
        "o",
        "u",
        "y"
    };

    private static string[] thirdSound =
    {
        "r",
        "l",
        "n",
        "m",
        "t",
        "q",
        "'"
    };

    private static string[] planetSuffix =
    {
        "I",
        "II",
        "III",
        "IV",
        "V",
        "VI",
        "VII",
        "VIII",
        "IX",
        "X",
        "Prime",
        "Delta",
        "Gamma"
    };

    private static void setSeed(int randomSeed)
    {
        Random.InitState(randomSeed);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        firstNames = firstNameAsset.ToString().Split("\n").ToList();
        lastNames = lastNameAsset.ToString().Split("\n").ToList();  
        alienVillainSuffix = alienVillainSuffixAsset.ToString().Split("\n").ToList();
        genericPrefix = genericPrefixAsset.ToString().Split("\n").ToList();
        genericSuffix = genericSuffixAsset.ToString().Split("\n").ToList();
        villainPrefix = villainPrefixAsset.ToString().Split("\n").ToList();
        villainSuffix = villainSuffixAsset.ToString().Split("\n").ToList();
        heroPrefix = heroPrefixAsset.ToString().Split("\n").ToList();
        heroSuffix = heroSuffixAsset.ToString().Split("\n").ToList();


        var cityList = cityAsset.ToString().Split("\n").Select(n => n.Split(","));
        cities = cityList.Where(n => n.Count() > 1).Select(n => new City(n[0], n[1])).ToList();




    }

    public static City GetCity(int CharacterSeed)
    {
        setSeed(nameSeed + CharacterSeed);
        return cities[Random.Range(0, firstNames.Count)];
    }

    public static string GetRealName(int characterSeed)
    {
        setSeed(nameSeed + characterSeed);

        string name1 = firstNames[Random.Range(0, firstNames.Count)].Trim();
        string name2 = lastNames[Random.Range(0, lastNames.Count)].Trim();

        return name1 + " " + name2;
    }

    public static string GetAlienName(int characterSeed, bool planet = false)
    {
        if (planet){
            setSeed(planetSeed + characterSeed);
        }
        else
        {
            setSeed(nameSeed + characterSeed);
        }
            

        string output = "";
        string capitalisedOutput = "";

        if (!planet || existingSystems.Count == 0 || Random.value < newSystemChance)
        {
            string first = firstSound[Random.Range(0, firstSound.Length)];
            output += first;


            string[] secondOptions = { "" };
            secondOptions = secondOptions.Concat(vowel).ToArray();

            string second = secondOptions[Random.Range(0, secondOptions.Length)];
            output += second;

            int thirdMax = thirdSound.Length;
            if (first == "" && second == "")
            {
                thirdMax--;
            }

            string third = thirdSound[Random.Range(0, thirdMax)];
            output += third;

            string fourth = vowel[Random.Range(0, vowel.Length)];
            output += fourth;

            string[] fifthOptions = firstSound.Concat(thirdSound).ToArray();
            string fifth = fifthOptions[Random.Range(0, fifthOptions.Length - 1)];
            output += fifth;

            
            bool capitaliseNext = true;
            int idx = 1;
            foreach (char c in output)
            {
                idx++;
                if (capitaliseNext)
                {
                    capitalisedOutput += c.ToString().ToUpper();
                    capitaliseNext = false;
                }
                else
                {
                    capitalisedOutput += c;

                    if (c == '\'' && idx != capitalisedOutput.Length)
                    {
                        capitaliseNext = true;
                    }
                }
            }

            if (planet)
                existingSystems.Add(capitalisedOutput);
        }
        else
        {
            capitalisedOutput = existingSystems[Random.Range(0, existingSystems.Count)];
        }

        if (planet)
        {
            capitalisedOutput += " ";
            capitalisedOutput += planetSuffix[Random.Range(0, planetSuffix.Length)];
        }
        
        return capitalisedOutput;


    }

    public static string GetAliasName(int characterSeed, bool hero = true)
    {
        setSeed(nameSeed + characterSeed);

        List<string> activePrefix;
        List<string> activeSuffix;

        if (hero)
        {
            activePrefix = heroPrefix;
            activeSuffix = heroSuffix;
        }
        else
        {
            activePrefix = villainPrefix;
            activeSuffix = villainSuffix;
        }

        List<string> allPrefix = activePrefix.Concat(genericPrefix).ToList();
        List<string> allSuffix = activeSuffix.Concat(genericSuffix).ToList();   

        string prefix = allPrefix[Random.Range(0, allPrefix.Count)].Trim();
        string suffix = allSuffix[Random.Range(0, allSuffix.Count)].Trim();

        return string.Join(" ", prefix,suffix);

    }

    public static string GetAlienVillain(int characterSeed, string name)
    {
        setSeed(nameSeed + characterSeed);

        if (Random.value < alienSuffixChance)
        {
            return name + alienVillainSuffix[Random.Range(0, alienVillainSuffix.Count)];
        }
        else
        {
            return name;
        }
    }

}   