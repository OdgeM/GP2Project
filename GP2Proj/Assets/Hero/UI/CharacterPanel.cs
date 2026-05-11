using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CharacterPanel : MonoBehaviour
{
    public GameObject locationNode;
    public TextMeshProUGUI characterName;
    public Character character;

    public RectTransform rectTransform;
    public Button button;
    
    public TextMeshProUGUI stats;
    public string statTemplate = "At: {0}\nDe: {1}\nHP: {2}/{3}";
    public TextMeshProUGUI availability;
    public string availableText = "<color=#57c4ad>Ready";
    public string busyText = "<color=#eda247>Busy";
    public string deadText = "<color=#db4325>Dead";
    public TextMeshProUGUI from;
    public string fromTemplate = "From: {0}";

    public GameObject deadPanel;
    public bool isDead = false;


    public void AssignCharacter(Character _character)
    {
        character = _character;

        StartCoroutine(waitForCharacter());

        
    }

    public void UpdateStats()
    {
        string statText = string.Format(statTemplate, character.attack, character.defence, character.HP, character.maxHP);
        stats.text = statText;
    }

    public void UpdateBusy()
    {

        if (character.isAvailable)
        {
            availability.text = availableText;
        }
        else
        {
            availability.text = busyText;
        }
        
    }

    public void TakeDamage()
    {
        if (character.HP > 0)
        {
            UpdateStats();
        }
        else
        {
            deadPanel.gameObject.SetActive(true);
            availability.text = deadText;
            stats.text = "";
            from.text = "";
            isDead = true;
        }
    }

    public IEnumerator waitForCharacter()
    {
        while (!character.ready)
        {
            yield return new WaitForEndOfFrame();
        }

        characterName.text = character.heroName;

        UpdateStats();

        string fromText = string.Format(fromTemplate, character.hometown);
        from.text = fromText;


    }
}
