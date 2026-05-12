using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using RoadSegment = CityGenerator.RoadSegment;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CityRenderer : MonoBehaviour
{
    public List<RoadSegment> roads = new();
    public List<Building> buildings = new();

    public float roadBorderWidth = 0.5f;

    public float sunAngle;
    public float sunElevation;
    public float maxShadowLength = 5;
    public Vector2 depthDir = new Vector2(-1, 1).normalized;
    public float GlowAmount = 0;
    public float MaxGlowAmount = 1;
    public Color GlowColour = Color.gold;

    public Color RoadColour = Color.black;
    public Color ShadowColour = Color.darkGray;
    public Color BuildingColour = Color.brown;
    public Color WallColour = Color.black;

    Mesh mesh;

    List<Vector2> uv2 = new();
    public void clear()
    {
       GetComponent<MeshFilter>().sharedMesh = null;
        roads.Clear(); buildings.Clear();
        uv2.Clear();
    }
    public void Generate(float Time)
    {
        
        float adjMaxShadow =
   Mathf.Lerp(.5f * maxShadowLength, maxShadowLength,
   1f - sunElevation);
        if (Time <= 5 || Time >= 20)
        {
            sunAngle = 0;
            sunElevation = 0;

             GlowAmount = MaxGlowAmount;
            adjMaxShadow = 0;

        }
        else
        {
            GlowAmount = 0;
            if (Time >= 18)
            {
                GlowAmount = Mathf.Lerp(0, MaxGlowAmount, 1 - ((float)(20 - Time) / (20-18)));
            }
            else if (Time <= 7)
            {
                GlowAmount = Mathf.Lerp(MaxGlowAmount, 0, (Time - 5) / (7 - 5));
            }

                sunAngle = Mathf.Lerp(-45, -135, (float)(Time - 5) / (20 - 7));
            sunElevation = Mathf.Lerp(.0f, .9f, 1 - Mathf.Abs((float)(13-Time))/13);
        }

        
        Vector2 lightDir = new Vector2(
    Mathf.Cos(sunAngle * Mathf.Deg2Rad),
    Mathf.Sin(sunAngle * Mathf.Deg2Rad)
).normalized;


        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        List<Vector3> vertices = new();
        List<int> tris = new();
        List<Color> colours = new();
        uv2 = new();
        foreach (var road in roads)
        {


            AddQuad(road.RotRect.Vertices, RoadColour, ref vertices, ref tris, ref colours);


            if (road.next != null && !road.next.isFailed)
            {
                List<Vector2> v = new();
                v.Add(road.RotRect.Vertices[3]);
                v.Add(road.RotRect.Vertices[2]);

                v.Add(road.next.RotRect.Vertices[1]);
                v.Add(road.next.RotRect.Vertices[0]);

                AddQuad(v, RoadColour, ref vertices, ref tris, ref colours);
            }


        }
       // buildings = buildings.OrderBy(n => n.height).ToList();

        

  

        foreach (var building in buildings)
        {
            float shadowLength =
            building.height /
            Mathf.Tan(sunElevation * Mathf.Deg2Rad);

            shadowLength = Mathf.Min(shadowLength, adjMaxShadow);

            var shadow = building.rect.Vertices.Select(n => n + lightDir * shadowLength).ToList();
           AddQuad(shadow, ShadowColour, ref vertices, ref tris, ref colours,0,Mathf.Clamp(.5f-GlowAmount/2, 0, .5f));

                
        }
        foreach (var building in buildings) {
            Vector2 depthOffset = depthDir * building.height;
            var roof = building.rect.Vertices.Select(n => n + depthOffset).ToList();

            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;

                Vector2 v0 = building.rect.Vertices[i];
                Vector2 v1 = building.rect.Vertices[next];
                Vector2 r0 = roof[i];
                Vector2 r1 = roof[next];

                Vector2 edge = v1 - v0;
                Vector2 normal = -Vector2.Perpendicular(edge).normalized;
                if (Vector2.Dot(normal, depthDir) < 0)
                {
                    WallColour.a = GlowAmount;
                    List<Vector2> wall = new List<Vector2>() { v0, v1, r1, r0 };
                    AddQuad(
                        wall,
                        WallColour,
                        ref vertices,
                        ref tris,
                        ref colours
                    );
                }

            }
            AddQuad(roof, BuildingColour, ref vertices, ref tris, ref colours, building.index);
            
        }



        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colours);
        mesh.SetUVs(1, uv2);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        

       
    }





    void AddQuad(
        List<Vector2> v,
        Color colour,
        ref List<Vector3> verts,
        ref List<int> tris,
        ref List<Color> cols,
        int index = 0,
        float opacity = 1
    )
    {
        int start = verts.Count;

        foreach (var vertex in v)
        {
            verts.Add(vertex);
            uv2.Add(new Vector2(index, opacity));
            cols.Add(colour);

        }

 
        

        tris.Add(start + 0);
        tris.Add(start + 1);
        tris.Add(start + 2);

        tris.Add(start + 0);
        tris.Add(start + 2);
        tris.Add(start + 3);
    }
}
