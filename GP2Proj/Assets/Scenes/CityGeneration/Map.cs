using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector2 Position;
    public List<Edge> Edges = new();
};

public class Edge
{
    public Node a;
    public Node b;

    public Node Other(Node n) => n == a ? b : a;
};

public class Lot 
{
    public Vector2 Location;
    public RotatedRect Rect;

    public Lot(RotatedRect rect)
    {
        Rect = rect;
    }
}

public class RotatedRect
{
    public List<Vector2> Vertices;
    public List<Vector2> Edges;
    public List<Vector2> Normals;

    public RotatedRect(CityGenerator.RoadSegment rs, float Lookahead = .25f)
    {
        Vector2 Norm = (rs.ra.endLocation - rs.ra.startLocation).normalized;
        Vector2 widthFactor = Vector2.Perpendicular(Norm) * rs.qa.width / 2;

        Vertices = new();
        Edges = new();
        Normals = new();

        Vertices.Add(rs.ra.startLocation + widthFactor);
        Vertices.Add(rs.ra.startLocation - widthFactor);
        Vertices.Add(rs.ra.endLocation - widthFactor + Norm * Lookahead * rs.ra.distance);
        Vertices.Add(rs.ra.endLocation + widthFactor + Norm * Lookahead * rs.ra.distance);

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

    public RotatedRect(Vector2 center, Vector2 size, float angle)
    {
        Vertices = new();

        Edges = new();
        Normals = new();

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
            Vertices.Add(new Vector2(x, y) + center);
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

            bool overlap = (projection1.y - projection1.x + projection2.y - projection2.x) > (Mathf.Max(projection2.y, projection1.y) - Mathf.Min(projection2.x, projection1.y));

            if (!overlap) return false;
        }

        foreach (Vector2 axis in Other.Normals)
        {
            Vector2 projection1 = this.Project(axis);
            Vector2 projection2 = Other.Project(axis);

            bool overlap = ((projection1.y - projection1.x) + (projection2.y - projection2.x)) > (Mathf.Max(projection2.y, projection1.y) - Mathf.Min(projection2.x, projection1.y));

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Generator = gameObject.AddComponent<CityGenerator>();
        Generator.Generate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
