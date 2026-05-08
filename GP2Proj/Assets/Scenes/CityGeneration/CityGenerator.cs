using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;


public class CityGenerator : MonoBehaviour
{
    public Texture WaterMap;

    public float defaultSegmentLength = 300;
    public float motorwaySegmentLength = 120;
    public float defaultSegmentWidth = 6;
    public float motorwaySegmentWidth = 16;
    public float branchAngleMean = 15;
    public float branchAngleSD = 1;
    public float motorwayBranchAngleMean = 15;
    public float motorwayBranchAngleSD = 1;
    public float straightAngleMean = 3;
    public float straightAngleSD = 15;
    public float motorwayStraightAngleMean = 3;
    public float motorwayStraightAngleSD = 15;
    public int motorwayBranchDelay = 15;

    public float IntersectionThreshold = 5;

    public float rectMultiplier = .25f;

    public Material lineMaterial;

    public float defaultBranchProbability = .4f;
    public float motorwayBranchProbability = .05f;
    public int maxSegments = 2000;

    public int seed = 12345;

    public List<RoadSegment> priorityQueue = new();
    public List<RoadSegment> segmentList = new();

    public List<RotatedRect> buildings = new();

    public List<Node> Nodes = new();
    public List<Edge> Edges = new();

    public float mergeThreshold = 0.1f;
    public float minLotArea = 10f;

    public int width = 1920;
    public int height = 1080;
    private List<RoadSegment> Bounding;


    void Start()
    {
       


        /*
        RoadSegment segment = new RoadSegment
           (
               0,
               new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 0),
               new QueryAttribute(true, motorwaySegmentWidth)
           );
        priorityQueue.Add(segment);*/


    }
    public void CreateCity()
    {
        Random.InitState(seed);

        Bounding = new();

        RoadSegment boundingSegment = new
            (
                0,
                new RoadAttribute(new Vector2(-width / 2, height / 2), width, 0),
                new QueryAttribute()
            );

        Bounding.Add(boundingSegment);

        boundingSegment = new
            (
                0,
                new RoadAttribute(new Vector2(-width / 2, height / 2), height, -90),
                new QueryAttribute()
            );

        Bounding.Add(boundingSegment);

        boundingSegment = new
           (
               0,
               new RoadAttribute(new Vector2(-width / 2, -height / 2), width, 0),
               new QueryAttribute()
           );

        Bounding.Add(boundingSegment);


        boundingSegment = new
           (
               0,
               new RoadAttribute(new Vector2(width / 2, height / 2), height, -90),
               new QueryAttribute()
           );

        Bounding.Add(boundingSegment);



        foreach (var b in Bounding)
        {
            b.RotRect.draw();
        }


        // Initialise Priority Queue
        RoadSegment segment = new RoadSegment
            (
                0,
                new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 0),
                new QueryAttribute(true, motorwaySegmentWidth)
            );
        priorityQueue.Add(segment);

        segment = new RoadSegment
            (
                0,
                new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 90),
                new QueryAttribute(true, motorwaySegmentWidth)
            );
        priorityQueue.Add(segment);

        segment = new RoadSegment
            (
                0,
                new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 180),
                new QueryAttribute(true, motorwaySegmentWidth)
            );
        priorityQueue.Add(segment);

        segment = new RoadSegment
            (
                0,
                new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 270),
                new QueryAttribute(true, motorwaySegmentWidth)
            );
        priorityQueue.Add(segment);

        Generate();
        Debug.Log(segmentList.Count());
        foreach (var seg in segmentList)
        {
            seg.RotRect = new(seg, 0);
        }

        RemoveDeadEnds();
        SortEdges();

        var blocks = ExtractFaces();
        blocks = RemoveOuterFace(blocks);

        foreach (var block in blocks)
        {
            List<RotatedRect> lots = new();
            Vector2 Origin = new();
            var Rastered = RasterisePolygon(block, 5, out Origin);

            foreach (var c in Rastered)
            {
                lots = GenerateLot(block, c, 1, 4, 7.5f, 2, 5, lots);
                
            }
            buildings.AddRange(lots);
        }
    }
    public bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;

        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 pi = poly[i];
            Vector2 pj = poly[j];

            if (((pi.y > p.y) != (pj.y > p.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + 0.00001f) + pi.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    List<Vector2> RasterisePolygon(List<Vector2> poly, float cellSize, out Vector2 origin)
    {
        // Find bounding box
        float minX = poly.Min(p => p.x);
        float maxX = poly.Max(p => p.x);
        float minY = poly.Min(p => p.y);
        float maxY = poly.Max(p => p.y);

        origin = new Vector2(minX, minY);

        int width = Mathf.CeilToInt((maxX - minX) / cellSize);
        int height = Mathf.CeilToInt((maxY - minY) / cellSize);

        List<Vector2> CellCentres = new();

        Vector2 Centre = new Vector2(poly.Select(n => n.x).Average(), poly.Select(n => n.y).Average());

        bool[,] grid = new bool[width, height];

        float s = cellSize * .5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Cell center
                Vector2 worldPos = origin + new Vector2(
                    (x + 0.5f) * cellSize,
                    (y + 0.5f) * cellSize
                );



                Vector2 a = worldPos + new Vector2(-s, -s);
                Vector2 b = worldPos + new Vector2(s, -s);
                Vector2 c = worldPos + new Vector2(s, s);
                Vector2 d = worldPos + new Vector2(-s, s);

                List<Vector2> list = new List<Vector2>() { a, b, c, d };
                RotatedRect rotatedRect = new(list);

                float distanceFromRoad = Nodes.Select(n => Vector2.Distance(n.Position, worldPos)).Min();

                if (PointInPolygon(worldPos, poly) && segmentList.Count(n => n.RotRect.Collides(rotatedRect)) == 0 && distanceFromRoad < 8)
                {
                    CellCentres.Add(worldPos);
                }
                ;
            }
        }
        return CellCentres;
    }

    List<RotatedRect> GenerateLot(
    List<Vector2> poly,
    Vector2 position,
    int attempts,
    float minWidth,
    float maxWidth,
    float minDepth,
    float maxDepth,
    List<RotatedRect> lots)
    {
        for (int i = 0; i < attempts; i++)
        {


            List<float> edges = poly.Select((n, index) =>
            {
                Vector2 a = n;
                Vector2 b = poly[(index + 1) % poly.Count];
                return Vector2.Distance((a + b) / 2, position);
            }
            ).ToList();

            int edgeIndex = edges.FindIndex(n => n == edges.Min());

            Vector2 a = poly[edgeIndex];
            Vector2 b = poly[(edgeIndex + 1) % poly.Count];

            Vector2 edgeDir = (b - a).normalized;
            Vector2 normal = new Vector2(-edgeDir.y, edgeDir.x);

            float edgeLength = Vector2.Distance(a, b);
            if (edgeLength < minWidth) continue;


            float t = UnityEngine.Random.Range(0f, 1f);
            Vector2 edgePoint = Vector2.Lerp(a, b, t);


            float width = UnityEngine.Random.Range(minWidth, Mathf.Min(maxWidth, edgeLength));
            float depth = UnityEngine.Random.Range(minDepth, maxDepth);

            Vector2 center = position;

            float angle = Mathf.Atan2(edgeDir.y, edgeDir.x) * Mathf.Rad2Deg;

            RotatedRect rect = new RotatedRect(center, new Vector2(width, depth), angle);

            bool inside = true;
            foreach (var corner in rect.Vertices)
            {
                if (!PointInPolygon(corner, poly))
                {
                    inside = false;
                    break;
                }
            }

            if (!inside) continue;

            bool overlaps = false;
            foreach (var other in lots)
            {
                if (rect.Collides(other))
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps) continue;

            foreach (var other in segmentList)
            {
                if (rect.Collides(other.RotRect))
                {
                    overlaps = true;
                    break;
                }
            }
            if (overlaps) continue; 


            lots.Add(rect);
        }

        return lots;
    }

    void DrawLots(List<RotatedRect> lots)
    {
        foreach (var r in lots)
        {
            var corners = r.Vertices;

            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(corners[i], corners[(i + 1) % 4], Color.green, 100f);
            }
        }
    }



    public void Generate()
    {
        while (priorityQueue.Count > 0 && segmentList.Count < maxSegments)
        {
            priorityQueue = priorityQueue.OrderByDescending(o => o.t).ToList();
            RoadSegment segment = priorityQueue.Last();
            priorityQueue.RemoveAt(priorityQueue.Count - 1);


            bool state = LocalConstraints(segment);

            if (state)
            {
                GlobalGoals(segment);

                AddSegment(segment);
            }
            else
            {
                segment.isFailed = true;
            }




        }

        


    }


    public void AddSegment(RoadSegment segment)
    {


        segmentList.Add(segment);


        Node NodeStart = GetOrCreateNode(segment.ra.startLocation);
        Node NodeEnd = GetOrCreateNode(segment.ra.endLocation);

        Edge e = new Edge { a = NodeStart, b = NodeEnd };

        Edges.Add(e);

        NodeStart.Edges.Add(e);
        NodeEnd.Edges.Add(e);

        segment.edge = e;

        if (segment.next == null || segment.next.isFailed)
        {
            var PossibleNext = segmentList.Where(n => n.ra.startLocation == segment.ra.endLocation);
            if (PossibleNext.Count() > 0)
            {
                segment.next = PossibleNext.First();
            }
        }

    }

    public Node GetOrCreateNode(Vector2 pos)
    {
        if (Nodes.Count(n => n.Position == pos) > 0)
        {
            return Nodes.Where(n => n.Position == pos).FirstOrDefault();
        }

        Node newNode = new Node() { Position = pos };
        Nodes.Add(newNode);
        return newNode;
    }

    void SortEdges()
    {
        foreach (var node in Nodes)
        {
            node.Edges = node.Edges.OrderBy(e =>
            {
                Vector2 dir = (e.Other(node).Position - node.Position).normalized;
                return Mathf.Atan2(dir.y, dir.x);
            }).ToList();
        }
    }

    void RemoveDeadEnds()
    {
        Queue<Node> queue = new Queue<Node>();

        foreach (var node in Nodes)
        {
            if (node.Edges.Count <= 1)
                queue.Enqueue(node);
        }

        while (queue.Count > 0)
        {
            Node n = queue.Dequeue();

            if (n.Edges.Count == 0)
                continue;

            Edge e = n.Edges[0];
            Node other = e.Other(n);

            // Remove edge
            Edges.Remove(e);
            other.Edges.Remove(e);
            n.Edges.Remove(e);

            if (other.Edges.Count == 1)
                queue.Enqueue(other);
        }

    }

    List<List<Vector2>> ExtractFaces()
    {
        var visited = new HashSet<HalfEdge>();
        var faces = new List<List<Vector2>>();

        foreach (var edge in Edges)
        {
            var halfEdges = new[]
            {
                new HalfEdge(edge.a, edge.b),
                new HalfEdge(edge.b, edge.a)
            };

            foreach (var he in halfEdges)
            {
                if (visited.Contains(he)) continue;
                List<Vector2> face = new List<Vector2>();
                HalfEdge current = he;
                int test = 0;
                while (test++ < Edges.Count-2)
                {
                    visited.Add(current);
                    face.Add(current.start.Position);

                    Node node = current.end;
                    Edge nextEdge = GetNextEdge(node, current);
                    Node nextNode = nextEdge.Other(node);

                    current = new HalfEdge(node, nextNode);

                    if (current.start == he.start && current.end == he.end)
                    {
                        break;
                    }
                }

                if (test == Edges.Count - 2)
                {
                    Debug.Log("HERE");
                }

                if (face.Count > 2)
                    faces.Add(face);
            }
        }
        return faces;
    }

    void DrawPolygon(List<Vector2> poly, Color color)
    {
        for (int i = 0; i < poly.Count; i++)
        {
            Debug.DrawLine(poly[i], poly[(i + 1) % poly.Count], color, 100f);
        }
    }

    Edge GetNextEdge(Node node, HalfEdge incoming)
    {
        var list = node.Edges;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Other(node) == incoming.start)
            {
                int nextIndex = (i - 1 + list.Count) % list.Count;
                return list[nextIndex];
            }
        }
        return list[0];
    }

    List<List<Vector2>> RemoveOuterFace(List<List<Vector2>> faces)
    {
        float maxArea = float.MinValue;
        int index = -1;

        for (int i = 0; i < faces.Count; i++)
        {
            float area = Mathf.Abs(PolygonArea(faces[i]));
            if (area > maxArea)
            {
                maxArea = area;
                index = i;
            }
        }

        if (index >= 0)
            faces.RemoveAt(index);

        return faces;
    }
    float PolygonArea(List<Vector2> poly)
    {
        float area = 0;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % poly.Count];
            area += (a.x * b.y - b.x * a.y);
        }
        return area * 0.5f;
    }





    public static float RandomAngle(float mean = 0.0f, float std = 10f)
    {
        float u, v, S;



        do
        {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float s = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float maxValue = mean + 3 * std;
        float minValue = mean - 3 * std;

        float value = Mathf.Clamp(std * s + mean, minValue, maxValue);

        if (Random.value > 0.5)
        {
            value = 360 - value;
        }

        return value;
    }
    public void GlobalGoals(RoadSegment lastSegment)
    {
        List<RoadSegment> branches = new List<RoadSegment>();
        Vector2 point = lastSegment.ra.endLocation;
        if (!lastSegment.qa.isSevered)
        {
            if (lastSegment.qa.isMotorway)
            {
                // Continue Motorway
                float angle = 0;

                if (Random.value < 0.3f)
                {
                    if (Random.value < 0.5f)
                    {
                        angle = RandomAngle(motorwayStraightAngleMean, motorwayStraightAngleSD);
                    }
                    else
                    {
                        angle = -1 * RandomAngle(motorwayStraightAngleMean, motorwayStraightAngleSD);
                    }
                }

                branches.Add(lastSegment.ContinueRoad(angle));

                // Maybe Branch Motorway
                if (Random.value < motorwayBranchProbability)
                {
                    if (Random.value < .5f)
                    {
                        angle = RandomAngle(motorwayBranchAngleMean, motorwayBranchAngleSD);
                    }
                    else
                    {
                        angle = -1 * RandomAngle(motorwayBranchAngleMean, motorwayBranchAngleSD);
                    }

                    branches.Add(lastSegment.BranchRoad(angle, lastSegment.ra.distance, motorwaySegmentWidth, true));
                }
            }
            else if (Random.value < .75f)
            {
                float angle = RandomAngle(straightAngleMean, straightAngleSD);
                if (Random.value < .5f)
                    angle *= -1;
                branches.Add(lastSegment.ContinueRoad(angle));
            }

            if (Random.value < defaultBranchProbability)
            {
                float angle = RandomAngle(branchAngleMean, branchAngleSD);
                if (Random.value < 0.5)
                {
                    angle *= -1;
                }

                int delay = 1;


                if (lastSegment.qa.isMotorway)
                {
                    delay = motorwayBranchDelay;
                }



                branches.Add(lastSegment.BranchRoad(angle, defaultSegmentLength, defaultSegmentWidth, false, delay));
            }

            foreach (RoadSegment branch in branches)
            {
                priorityQueue.Add(branch);
            }
        }
    }

    public bool LocalConstraints(RoadSegment segment)
    {
        if (Mathf.Abs(segment.ra.endLocation.x) > width / 2 || Mathf.Abs(segment.ra.endLocation.y) > height / 2)
        {
            foreach (var b in Bounding)
            {
              Vector2? intersection = Intersect(b, segment);
              if (intersection.HasValue)
                {
                    if (intersection.Value == segment.ra.startLocation)
                    {
                        return false;
                    }
                    segment.ra.endLocation = intersection.Value;
                    segment.RotRect = new(segment);
                    segment.qa.isSevered = true;
                    break;
                }
            }

        }



        if (Nodes.Count() > 0)
        {
            var node = Nodes.Where(n => Vector2.Distance(n.Position, segment.ra.startLocation) > 1);
            node = node.OrderBy(n => Vector2.Distance(n.Position, segment.ra.endLocation));

            if (segment.ra.startLocation == new Vector2(-480, (float)-4.196293e-05))
            {
                Debug.Log(node.First().Position);
                Debug.Log(node.First().Position);
                Debug.Log(Vector2.Distance(node.First().Position, segment.ra.startLocation));
                Debug.Log(Vector2.Distance(node.First().Position, segment.ra.endLocation));
            }

            if (Vector2.Distance(node.First().Position, segment.ra.endLocation) < IntersectionThreshold)
            {
                segment.ra.endLocation = node.First().Position;
            }

            segment.RotRect = new(segment);
        }


        List<RoadSegment> closeRoads = segmentList.Where((s) => segment.RotRect.Collides(s.RotRect)).ToList();




        Vector2? closestIntersection = null;
        RoadSegment otherSegment = null;
        foreach (RoadSegment s in closeRoads)
        {
            if (s != segment && s != segment.parent && s.parent != segment)
            {
    

                Vector2? intersection = Intersect(segment, s);
                if (intersection.HasValue && Vector2.Distance(intersection.Value, segment.ra.startLocation) > 1f)
                {


                    float length = Vector2.Distance(intersection.Value, segment.ra.startLocation);
                    if (closestIntersection == null || Vector2.Distance(closestIntersection.Value, segment.ra.startLocation) > length)
                    {
                        otherSegment = s;
                        closestIntersection = intersection;
                    }
                }
            }
        }

        if (closestIntersection.HasValue)
        {

            if (Nodes.Count() > 0)
            {
                var node = Nodes.Where(n => Vector2.Distance(n.Position, segment.ra.startLocation) > 1);
                node = node.OrderBy(n => Vector2.Distance(n.Position, closestIntersection.Value));

                if (segment.ra.startLocation == new Vector2(-480, (float)-4.196293e-05))
                {
                    Debug.Log(node.First().Position);
                    Debug.Log(node.First().Position);
                    Debug.Log(Vector2.Distance(node.First().Position, segment.ra.startLocation));
                    Debug.Log(Vector2.Distance(node.First().Position, segment.ra.endLocation));
                }

                if (Vector2.Distance(node.First().Position, closestIntersection.Value) < IntersectionThreshold)
                {
                    closestIntersection = node.First().Position;
                }


            }



            /*
            if (Mathf.Abs(Vector2.Dot((otherSegment.ra.endLocation-otherSegment.ra.startLocation).normalized, (segment.ra.endLocation - segment.ra.startLocation).normalized)) > 0.95f)
            {
                return false;
            }*/
            segment.ra.endLocation = closestIntersection.Value;

            segment.qa.isSevered = true;
            segment.ra.distance = Vector2.Distance(closestIntersection.Value, segment.ra.startLocation);
            segment.RotRect = new(segment);




            SplitSegment(otherSegment, closestIntersection.Value);
        }

        if (segmentList.Count(n => n.ra.startLocation == segment.ra.startLocation && n.ra.endLocation == segment.ra.endLocation) > 0)
        {
            var other = segmentList.Where(n => n.ra.startLocation == segment.ra.startLocation && n.ra.endLocation == segment.ra.endLocation).First();
            if (segment.qa.isMotorway && !other.qa.isMotorway)
            {
                Debug.Log("HERE?");
                other.SetMotorway(true, motorwaySegmentWidth);
            }


            return false;
        }

        var PossibleDupes = segmentList.Where(n => n.ra.startLocation == segment.ra.startLocation || n.ra.endLocation == segment.ra.endLocation || n.ra.startLocation == segment.ra.endLocation || n.ra.endLocation == segment.ra.endLocation);

        Vector2 thisLine = segment.ra.endLocation - segment.ra.startLocation;

        foreach (var p in PossibleDupes)
        {

            Vector2 otherLine = p.ra.endLocation - p.ra.startLocation;

            float angle = Mathf.Acos(Vector2.Dot(otherLine, thisLine) / (otherLine.magnitude * thisLine.magnitude));
            if (angle < .1 || Mathf.Abs((Mathf.PI - angle)) < .1)
            {
                Debug.Log("=-=-=-=-=-=");
                Debug.Log(segment.ra.startLocation);
                Debug.Log(segment.ra.endLocation);
                float directionComp = Vector2.Distance(thisLine.normalized, otherLine.normalized); ;
                if (p.ra.startLocation == segment.ra.startLocation)
                {
                    // Pointing same direction = overlap
                    if (directionComp < 0.5f)
                    {
                        return false;
                    }

                }
                else
                {
                    // Pointing different direction = overlap

                    if (directionComp > 0.5f)
                    {

                        return false;
                    }
                }

            }

        }



        return true;
    }

    public void SplitSegment(RoadSegment rs, Vector2 point)
    {
        Edge edge = rs.edge;

        Node mid = GetOrCreateNode(point);

        edge.a.Edges.Remove(edge);
        edge.b.Edges.Remove(edge);
        Edges.Remove(edge);

        Edge e1 = new Edge { a = edge.a, b = mid };
        Edge e2 = new Edge { a = mid, b = edge.b };

        Edges.Add(e1);
        Edges.Add(e2);

        e1.a.Edges.Add(e1);
        e1.b.Edges.Add(e1);

        e2.a.Edges.Add(e2);
        e2.b.Edges.Add(e2);


    }

    public class RoadSegment
    {
        public int t;
        public RoadAttribute ra;
        public QueryAttribute qa;
        public RoadSegment parent;
        public RoadSegment next;
        public List<RoadSegment> branches;
        public bool isActive = false;
        public bool isFailed = false;
        public bool isBranch = true;
        public LineRenderer line;
        public Rect rectangle = new();
        public RotatedRect RotRect;

        public Edge edge;


        public RoadSegment(int _t, RoadAttribute _ra, QueryAttribute _qa)
        {
            t = _t;
            ra = _ra;
            qa = _qa;
            parent = this;
            branches = new List<RoadSegment>();

            Vector2 widthFactor = (new Vector2(1, 1) - (_ra.endLocation - _ra.startLocation).normalized) * _qa.width / 2;

            RotRect = new(this);



            rectangle.xMin = Mathf.Min(ra.startLocation.x, ra.endLocation.x) - widthFactor.x - .25f * (Mathf.Abs(ra.startLocation.x - ra.endLocation.x) + widthFactor.x);
            rectangle.xMax = Mathf.Max(ra.startLocation.x, ra.endLocation.x) + widthFactor.x + .25f * (Mathf.Abs(ra.startLocation.x - ra.endLocation.x) + widthFactor.x);
            rectangle.yMin = Mathf.Min(ra.startLocation.y, ra.endLocation.y) - widthFactor.y - .25f * (Mathf.Abs(ra.startLocation.y - ra.endLocation.y) + widthFactor.y);
            rectangle.yMax = Mathf.Max(ra.startLocation.y, ra.endLocation.y) + widthFactor.y + .25f * (Mathf.Abs(ra.startLocation.y - ra.endLocation.y) + widthFactor.y);

            // Debug.DrawLine(new Vector2(rectangle.xMin, rectangle.yMin), new Vector2(rectangle.xMax, rectangle.yMin), Color.red, Mathf.Infinity);
        }

        public void SetMotorway(bool motorway, float width)
        {
            qa.SetMotorway(motorway, width);
            RotRect = new(this);
        }

        public RoadSegment ContinueRoad(float angle = 0)
        {
            RoadAttribute newRa = new RoadAttribute(ra.endLocation, ra.distance, ra.angle + angle);
            RoadSegment newSegment = new RoadSegment(t + 1, newRa, qa);
            next = newSegment;
            newSegment.parent = this;
            newSegment.isBranch = false;
            return newSegment;
        }


        public RoadSegment BranchRoad(float angle, float length, float width, bool motorway = false, int delay = 1)
        {
            RoadAttribute newRa = new RoadAttribute(ra.endLocation, length, ra.angle + angle);
            QueryAttribute newQuery = new QueryAttribute(motorway, width);
            RoadSegment newSegment = new RoadSegment(t + delay, newRa, newQuery);

            if (next == null)
            {
                next = newSegment;
            }

            branches.Add(newSegment);
            newSegment.parent = this;
            return newSegment;
        }

        public void DrawSegment()
        {
            line.positionCount += 1;
            line.SetPosition(line.positionCount - 1, ra.endLocation);

            //RotRect.draw();
        }


    }
    Vector2? Intersect(RoadSegment v1, RoadSegment v2, bool debug = false)
    {
        Vector2 q = v2.ra.startLocation;
        Vector2 p = v1.ra.startLocation;

        Vector2 r = v1.ra.endLocation - p;
        Vector2 s = v2.ra.endLocation - q;

        Vector2 qp = q - p;



        /*
        Debug.Log("q: " + q);
        Debug.Log("p: " + p);*/

        float rs = cross2d(r, s);
        //Debug.Log("rs: " + rs);


        if (Mathf.Abs(rs) <= 1)
        {

            if (Mathf.Abs(cross2d(qp, r)) <= 1)
            {

                float t0 = Vector2.Dot(qp, r) / Vector2.Dot(r, r);
                float t1 = t0 + Vector2.Dot(s, r) / Vector2.Dot(r, r);

                float max = Mathf.Max(t0, t1);
                float min = Mathf.Min(t0, t1);


                if ((t0 >= 0 && t0 <= 1) || (t1 >= 0 && t1 <= 1))
                {
                    Debug.Log("HERE");

                    if (Vector2.Distance(q, p) < Vector2.Distance(v2.ra.endLocation, p))
                    {
                        return q;
                    }
                    else
                    {
                        return v2.ra.endLocation;
                    }
                }
            }
            else
            {
                return null;
            }


        }
        else
        {

            float t = cross2d(qp, s / rs);
            float u = cross2d(qp, r / rs);
            if ((u <= 1 && u >= 0))
            {
                return p + t * r;
            }


        }

        return null;
    }

    float cross2d(Vector2 v, Vector2 w)
    {
        return v.x * w.y - v.y * w.x;
    }





    public readonly struct HalfEdge : IEquatable<HalfEdge>
    {
        public readonly Node start;
        public readonly Node end;

        public HalfEdge(Node s, Node e)
        {
            start = s;
            end = e;
        }

        public bool Equals(HalfEdge other)
        {
            return ReferenceEquals(other.start, start)
                && ReferenceEquals(other.end, end);
        }

        public override bool Equals(object obj)
        {
            return obj is HalfEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(start, end);
        }
    }
}






public struct RoadAttribute
{
    public Vector2 startLocation;
    public Vector2 endLocation;
    public float distance;
    public float angle;

    public RoadAttribute(Vector2 _location, float _distance, float _angle)
    {
        startLocation = _location;
        distance = _distance;
        angle = _angle;
        endLocation = startLocation + new Vector2(Mathf.Cos(_angle * Mathf.Deg2Rad), Mathf.Sin(_angle * Mathf.Deg2Rad)) * distance;
    }
}

public struct QueryAttribute
{
    public bool isMotorway;
    public bool isSevered;
    public float width;

    public QueryAttribute(bool motorway = false, float width = -1, bool severed = false)
    {
        this.width = width;
        isMotorway = motorway;
        isSevered = severed;
    }

    public void SetMotorway(bool motorway, float width)
    {
        isMotorway = motorway;
        this.width = width;
    }
}

