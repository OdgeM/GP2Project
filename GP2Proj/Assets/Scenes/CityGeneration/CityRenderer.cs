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
    public List<RotatedRect> buildings = new();

    public float roadBorderWidth = 0.5f;    

    Mesh mesh;

    public void Generate()
    {
        mesh = new Mesh();

        List<Vector3> vertices = new();
        List<int> tris = new();
        List<Color> colors = new();

        foreach (var road in roads)
        {


            AddQuad(road.RotRect.Vertices, Color.white, ref vertices, ref tris, ref colors);

            
            if (road.next != null && !road.next.isFailed)
            {
                List<Vector2> v = new();
                v.Add(road.RotRect.Vertices[3]);
                v.Add(road.RotRect.Vertices[2]);

                v.Add(road.next.RotRect.Vertices[1]);
                v.Add(road.next.RotRect.Vertices[0]);

                AddQuad(v, Color.red, ref vertices, ref tris, ref colors); 
            }

           
        }


        /*
        foreach (var road in roads.Where(n => !n.qa.isMotorway))
        {
            QueryAttribute qa = road.qa;
            qa.width *= 1 - roadBorderWidth;

            Color c = Color.gray2;
          
            RoadSegment inner = new(0, road.ra, qa);
            RotatedRect r = new(inner, 0);
            AddQuad(r.Vertices, c, ref vertices, ref tris, ref colors);
            
            
            if (road.next != null)
            {
                RoadSegment other = new(0, road.next.ra, qa);
                RotatedRect otherRect = new(other, 0);

                List<Vector2> v = new();
                v.Add(r.Vertices[3]);
                v.Add(r.Vertices[2]);

                v.Add(otherRect.Vertices[1]);
                v.Add(otherRect.Vertices[0]);

                AddQuad(v, c, ref vertices, ref tris, ref colors);
            }



        }

        foreach (var road in roads.Where(n => n.qa.isMotorway))
        {
            QueryAttribute qa = road.qa;
            qa.width *= 1-roadBorderWidth;

            if (qa.width < 8f)
            {
                Debug.Log(qa.width);
                Debug.Log("zero width?");
            }

            Color c = Color.white;

            RoadSegment inner = new(0, road.ra, qa);
            RotatedRect r = new(inner, 0);
            AddQuad(r.Vertices, c, ref vertices, ref tris, ref colors);


            if (road.next != null)
            {
                RoadSegment other = new(0, road.next.ra, qa);
                RotatedRect otherRect = new(other, 0);

                List<Vector2> v = new();
                v.Add(r.Vertices[3]);
                v.Add(r.Vertices[2]);

                v.Add(otherRect.Vertices[1]);
                v.Add(otherRect.Vertices[0]);

                AddQuad(v, c, ref vertices, ref tris, ref colors);
            }


        }*/


        foreach (var building in buildings)
        {
            AddQuad(building.Vertices, Color.brown, ref vertices, ref tris, ref colors);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(colors);

        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }





    void AddQuad(
        List<Vector2> v,
        Color color,
        ref List<Vector3> verts,
        ref List<int> tris,
        ref List<Color> cols
    )
    {
        int start = verts.Count;

        foreach (var vertex in v)
        {
            verts.Add(vertex);
        }

        cols.Add(color);
        cols.Add(color);
        cols.Add(color);
        cols.Add(color);

        tris.Add(start + 0);
        tris.Add(start + 1);
        tris.Add(start + 2);

        tris.Add(start + 0);
        tris.Add(start + 2);
        tris.Add(start + 3);
    }
}
