using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class IncidentMenu : MonoBehaviour
{
    public GameObject contentBox;
    public List<IncidentPanel> panelList = new();

    public GameManager gameManager;

    public void PlacePanel(IncidentPanel panel)
    {
        panelList.Add(panel);

        if (panelList.Count > 1)
        {
            List<IncidentPanel> resolvedPanels = panelList.Where(panel => panel.resolved).ToList();

            foreach (IncidentPanel rPanels in resolvedPanels)
            {
                rPanels.transform.SetAsLastSibling();
            }
        }
    }
}
