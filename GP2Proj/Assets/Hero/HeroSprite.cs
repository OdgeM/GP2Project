using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class HeroSprite : MonoBehaviour
{


    public SpriteLibraryAsset spriteLibrary;
    [Header("Head")]
    public SpriteRenderer headSprite;
    public SpriteRenderer hairSprite;
    public SpriteResolver hairResolver;
    private string hairCategory = "Hair";
    public List<string> hairSprites;
    public SpriteRenderer headwearSprite;
    public SpriteResolver headwearResolver;
    private string headwearCategory = "Headwear";
    public List<string> headwearSprites;
    public SpriteRenderer eyeSprite;

    [Header("Body")]
    public SpriteRenderer bodySprite;
    public SpriteRenderer pantsSprite;
    public SpriteRenderer insigniaSprite;
    public SpriteResolver insigniaResolver;
    private string insigniaCategory = "Insignia";
    public List<string> insigniaSprites;

    [Header("Right Arm")]
    public GameObject rightArm;
    public SpriteRenderer rightArmSprite;
    public SpriteRenderer rightHandSprite;

    [Header("Left Arm")]
    public GameObject leftArm;
    public SpriteRenderer leftArmSprite;
    public SpriteRenderer leftHandSprite;

    [Header("Right Leg")]
    public GameObject rightLeg;
    public SpriteRenderer rightLegSprite;
    public SpriteRenderer rightBoot;

    [Header("Left Leg")]
    public GameObject leftLeg;
    public SpriteRenderer leftLegSprite;
    public SpriteRenderer leftBoot;

    [Header("Cape")]
    public SpriteRenderer cape;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        hairSprites = spriteLibrary.GetCategoryLabelNames(hairCategory).ToList();
        headwearSprites = spriteLibrary.GetCategoryLabelNames(headwearCategory).ToList();
        insigniaSprites = spriteLibrary.GetCategoryLabelNames(insigniaCategory).ToList();
    }


    public void SetSkinColour(Color colour)
    {
        headSprite.color = colour;
        rightHandSprite.color = colour;
        leftHandSprite.color = colour;
    }

    public void SetEyeColour(Color colour)
    {
        eyeSprite.color = colour;
    }
    public void SetSuitColour(Color bodyColour, Color armColour, Color legColour, Color bootColour)
    {
        bodySprite.color = bodyColour;
        leftArmSprite.color = armColour;
        rightArmSprite.color = armColour;
        leftLegSprite.color = legColour;
        rightLegSprite.color = legColour;
        leftBoot.color = bootColour;
        rightBoot.color = bootColour;
    }

    public void SetOptionalSprite(SpriteRenderer spriteRenderer, SpriteResolver spriteResolver, string category,string label, Color colour)
    {
        spriteRenderer.gameObject.SetActive(true);
        spriteResolver.SetCategoryAndLabel(category, label);
        spriteRenderer.color = colour;
    }

    public void SetHair(bool hasHair, int hairIndex, Color colour)
    {
        if (hasHair)
        {
            SetOptionalSprite(hairSprite, hairResolver, hairCategory, hairSprites[hairIndex], colour);
        }
        else
        {
            hairSprite.gameObject.SetActive(false);
        }
    }

    public void SetHeadwear(bool hasHeadwear, int headwearIndex, Color colour)
    {
        if (hasHeadwear)
        {
            SetOptionalSprite(headwearSprite, headwearResolver, headwearCategory, headwearSprites[headwearIndex], colour);
        }
        else
        {
            headwearSprite.gameObject.SetActive(false);
        }
    }

    public void SetInsignia(bool hasInsignia, int insigniaIndex, Color colour)
    {
        if (hasInsignia)
        {
            SetOptionalSprite(insigniaSprite, insigniaResolver, insigniaCategory, insigniaSprites[insigniaIndex], colour);
        }
        else
        {
            insigniaSprite.gameObject.SetActive(false);
        }
    }

    public void SetPants(bool hasPants,  Color colour)
    {
        if (hasPants)
        {
            pantsSprite.gameObject.SetActive(true);
            pantsSprite.color = colour;
        }
        else
        {
            pantsSprite.gameObject.SetActive(false);
        }
    }

    public void SetCape(bool hasCape, Color colour) //Wear Cape, Fly
    {
        if (hasCape)
        {
            cape.gameObject.SetActive(true);
            cape.color = colour;
        }
        else
        {
            cape.gameObject.SetActive(false);
        }
    }

}
