using TMPro;
using UnityEngine;

public class CharacterScreen : MonoBehaviour
{
    public TextMeshProUGUI titleLabel;
    public HeroSprite heroSprite;
    public TextMeshProUGUI heroStats;
    public TextMeshProUGUI heroDetails;
    public TextMeshProUGUI heroTag;
    public GameObject deadTag;
    public TextMeshProUGUI killer;
    public string statTemplate = "Attack: {0}\nDefence: {1}\nHp: {2}";
    public string detailTemplate = "Real Name: {0}\nFrom: {1}\nAge: {2}\nDeployments: {3}";
    public Color HeroColour;
    public Color VillainColour;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void AssignCharacter(Character character, bool hero = true)
    {
        if (character.isDead)
        {
            deadTag.SetActive(true);
            killer.text = "Killed by " + character.killer;
        }
        else
        {
            deadTag.SetActive(false);
        }

        Color tagColour = VillainColour;
        string tagText = "Villain";
        if (hero)
        {
            tagColour = HeroColour;
            tagText = "Hero";
        }

        heroTag.text = tagText;
        heroTag.color = tagColour;   

        character.SetSprite(heroSprite);
        titleLabel.text = character.heroName;

        string statText = string.Format(statTemplate, character.attack, character.defence, character.HP, character.maxHP);
        heroStats.text = statText;

        string detailText = string.Format(detailTemplate, character.realName, character.hometown, character.age, character.deployments);
        heroDetails.text = detailText;

    }
}
