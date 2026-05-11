using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterMenu : MonoBehaviour
{
    public GameObject contentBox;
    public List<CharacterPanel> panelList = new();

    public void PlacePanel(CharacterPanel panel)
    {
       
        panelList.Add(panel);

        if (panelList.Count > 1) {
            List<CharacterPanel> deadPanels = panelList.Where(panel => panel.isDead).ToList();

            foreach (CharacterPanel dPanel in deadPanels)
            {
                dPanel.transform.SetAsLastSibling();
            }
        }

    }
}
