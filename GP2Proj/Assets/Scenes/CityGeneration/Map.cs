using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class Node
{
    public Vector2 Position;
    public List<Edge> Edges = new();
    public bool Motorway = false;

    public Node(bool isMotorway = false)
    {
        Motorway = isMotorway;
    }

};

public class Edge
{
    public Node a;
    public Node b;

    public Node Other(Node n) => n == a ? b : a;
};

public class Building
{
    public RotatedRect rect;
    public float height = 1;
    public Vector2 centre;
    public string BuildingName;
    public bool activeIncident = false;
    public Building(RotatedRect r, float h, Vector2 c)
    {
        rect = r;
        height = h;
        centre = c;
    }

    public void Deactivate()
    {
        activeIncident = false;
    }
}

public class RotatedRect
{


    public List<Vector2> Vertices;
    public List<Vector2> Edges;
    public List<Vector2> Normals;
    public Vector2 Centre;

    public RotatedRect(CityGenerator.RoadSegment rs, float Lookahead = .25f)
    {
        FromSegment(rs, Lookahead);


    }

    private void FromSegment(CityGenerator.RoadSegment rs, float Lookahead = 0)
    {
        Vector2 Norm = (rs.ra.endLocation - rs.ra.startLocation).normalized;
        Vector2 widthFactor = Vector2.Perpendicular(Norm) * rs.qa.width / 2;

        Vertices = new();
        Edges = new();
        Normals = new();

        Vertices.Add(rs.ra.startLocation + widthFactor);
        Vertices.Add(rs.ra.startLocation - widthFactor);
        Vertices.Add(rs.ra.endLocation - widthFactor + Lookahead * rs.ra.distance * Norm);
        Vertices.Add(rs.ra.endLocation + widthFactor + Norm * Lookahead * rs.ra.distance);

        Centre = new Vector2(Vertices.Select(n => n.x).Average(), Vertices.Select(n => n.y).Average());

        Edges.Add(Vertices[1] - Vertices[0]);
        Edges.Add(Vertices[2] - Vertices[1]);
        Edges.Add(Vertices[3] - Vertices[2]);
        Edges.Add(Vertices[0] - Vertices[1]);

        foreach (Vector2 e in Edges)
        {
            Normals.Add(Vector2.Perpendicular(e));

        }

    }

    public RotatedRect(List<Vector2> vertices)
    {
        Vertices = vertices;

        Edges = new();
        Normals = new();

        for (int i = 0; i < Vertices.Count; i++)
        {
            if (i != vertices.Count - 1)
            {
                Edges.Add(vertices[i + 1] - vertices[i]);
            }
            else
            {
                Edges.Add(vertices[0] - vertices[i]);
            }

        }

        foreach (Vector2 e in Edges)
        {
            Normals.Add(Vector2.Perpendicular(e));

        }
    }

    public RotatedRect(Vector2 centre, Vector2 size, float angle)
    {
        Vertices = new();

        Edges = new();
        Normals = new();
        Centre = centre;
        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;

        // Convert to radians
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        // Local corners (counter-clockwise)
        Vector2[] local =
                    {
                    new Vector2(-halfW, -halfH),
                    new Vector2( halfW, -halfH),
                    new Vector2( halfW,  halfH),
                    new Vector2(-halfW,  halfH)
                };

        for (int i = 0; i < 4; i++)
        {
            Vector2 p = local[i];

            // Rotate
            float x = p.x * cos - p.y * sin;
            float y = p.x * sin + p.y * cos;

            // Translate
            Vertices.Add(new Vector2(x, y) + centre);
        }

        for (int i = 0; i < Vertices.Count; i++)
        {
            if (i != Vertices.Count - 1)
            {
                Edges.Add(Vertices[i + 1] - Vertices[i]);
            }
            else
            {
                Edges.Add(Vertices[0] - Vertices[i]);
            }

        }

        foreach (Vector2 e in Edges)
        {
            Normals.Add(Vector2.Perpendicular(e));

        }
    }



    public void draw()
    {
        Debug.DrawLine(Vertices[1], Vertices[0], Color.red, Mathf.Infinity);
        Debug.DrawLine(Vertices[2], Vertices[1], Color.red, Mathf.Infinity);
        Debug.DrawLine(Vertices[3], Vertices[2], Color.red, Mathf.Infinity);
        Debug.DrawLine(Vertices[0], Vertices[3], Color.red, Mathf.Infinity);
    }

    public bool Collides(RotatedRect Other)
    {
        foreach (Vector2 axis in this.Normals)
        {
            Vector2 projection1 = this.Project(axis);
            Vector2 projection2 = Other.Project(axis);

            bool overlap = (projection1.y - projection1.x + projection2.y - projection2.x) > (Mathf.Max(projection2.y, projection1.y) - Mathf.Min(projection2.x, projection1.x));

            if (!overlap) return false;
        }

        foreach (Vector2 axis in Other.Normals)
        {
            Vector2 projection1 = this.Project(axis);
            Vector2 projection2 = Other.Project(axis);

            bool overlap =
     projection1.x <= projection2.y &&
     projection1.y >= projection2.x;

            if (!overlap) return false;
        }


        return true;
    }

    public Vector2 Project(Vector2 Axes)
    {
        float min = Mathf.Infinity;
        float max = Mathf.NegativeInfinity;

        foreach (Vector2 p in Vertices)
        {
            float dot = Vector2.Dot(Axes, p);

            if (dot < min) min = dot;
            if (dot > max) max = dot;
        }

        Vector2 Projection = new(min, max);
        return Projection;


    }
}


public class Map : MonoBehaviour
{
    public CityGenerator Generator;
    public CityRenderer cRenderer;
    public CustomRenderTexture WaterTexture;
    public Material WaterMaterial;
    public Material MapMaterial;
    public float Time = 9;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public async Awaitable Generate(int segments, bool gridBased, bool coastal, bool river, int seed)
    {

        StopAllCoroutines();
        Time = 9;
        cRenderer.clear();
        Random r = new(seed);
        WaterMaterial.SetInt("_Seed", seed);
        if (coastal || river)
        {
            WaterMaterial.SetInt("_Water", 1);

            WaterMaterial.SetInt("_RiverCoast", coastal ? 1 : 0);
        }
        else
        {
            WaterMaterial.SetInt("_Water", 0);
        }
        WaterTexture.material = WaterMaterial;
        WaterTexture.Update();
        await Awaitable.NextFrameAsync();

        Generator.WaterTexture = WaterTexture;
        Generator.maxSegments = segments;

        if (gridBased)
        {
            Generator.motorwayBranchAngleSD = 0;
            Generator.branchAngleSD = 0;
            Generator.maxMotorwayStraightAngle = 1;
            Generator.straightAngleSD = 1;
            Generator.motorwayBranchProbability = .5f;
            Generator.defaultBranchProbability = 1f;
            Generator.motorwaySegmentLength = 32;
            Generator.defaultSegmentLength = 32;
        }
        else
        {
            Generator.motorwayBranchAngleSD = r.Next(10,30);
            Generator.branchAngleSD = r.Next(10, 30);
            Generator.maxMotorwayStraightAngle = r.Next(4, 6);
            Generator.straightAngleSD = r.Next(7, 14);
            Generator.motorwaySegmentLength = 12;
            Generator.defaultSegmentLength = 12;
            Generator.motorwayBranchProbability = .25f;
            Generator.defaultBranchProbability = .8f;
        }

            
        await Generator.CreateCity(r);
        Debug.Log(Generator.segmentList.Count);
        foreach (var road in Generator.segmentList)
        {
            cRenderer.roads.Add(road);
        }

        foreach (var b in Generator.buildings)
        {
            cRenderer.buildings.Add(b);
        }

        cRenderer.Generate(Time);
        StartCoroutine(Clock());

    }


    IEnumerator Clock()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            Time += .25f;
            if (Time == 24)
            {
                Time = 0;
            }
            cRenderer.Generate(Time);
            float NightTime = 1;

            if (Time >= 20 || Time <= 5)
            {
                NightTime = 0.3f;
            }

            else if (Time >= 18)
            {
                NightTime = Mathf.Lerp(.3f, 1, ((float)(20 - Time) / (20 - 18)));
            }
            else if (Time <= 7)
            {
                NightTime = Mathf.Lerp(.3f, 1, ((float)(Time - 5) / (7 - 5)));
            }

            Debug.Log(NightTime);

            MapMaterial.SetFloat("_NightTime", NightTime);
        }
    }


    public Incident GenerateIncident(Villain villain)
    {
        return new Incident(Generator.buildings[0], villain);
    }


    // Update is called once per frame
    void Update()
    {

    }
}
