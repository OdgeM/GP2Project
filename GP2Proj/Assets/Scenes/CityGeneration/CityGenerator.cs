using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;


public class CityGenerator : MonoBehaviour
{
    public float defaultSegmentLength = 300;
    public float motorwaySegmentLength = 400;
    public float defaultSegmentWidth = 6;
    public float motorwaySegmentWidth = 16;
    public float branchAngleMean = 15;
    public float branchAngleSD = 1;
    public float straightAngleMean = 3;
    public float straightAngleSD = 15;
    public int motorwayBranchDelay = 15;

    public float rectMultiplier = .25f;

    public Material lineMaterial;

    public float defaultBranchProbability = .4f;
    public float motorwayBranchProbability = .05f;
    public int maxSegments = 2000;

    public int seed = 12345;

    public List<RoadSegment> priorityQueue = new();
    public List<RoadSegment> segmentList = new();

    public List<Node> Nodes = new();
    public List<Edge> Edges = new();

    public float mergeThreshold = 0.1f;
    public float minLotArea = 10f;

    void Start()
    {
        Random.InitState(seed);

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




        /*
        RoadSegment segment = new RoadSegment
           (
               0,
               new RoadAttribute(new Vector2(0, 0), motorwaySegmentLength, 0),
               new QueryAttribute(true, motorwaySegmentWidth)
           );
        priorityQueue.Add(segment);*/


        Generate();

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
            var Rastered = RasterisePolygon(block, 50, out Origin);

            foreach (var c in Rastered)
            {
                lots = GenerateAlignedLots(block, c, 10, 40, 75, 20, 50, lots);
                
            }

            DrawLots(lots);

        }
    }
    bool PointInPolygon(Vector2 p, List<Vector2> poly)
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

                if (PointInPolygon(worldPos, poly) && segmentList.Count(n => n.RotRect.Collides(rotatedRect)) == 0 && distanceFromRoad < 80)
                {
                    CellCentres.Add(worldPos);
                }
                ;
            }
        }
        return CellCentres;
    }

    List<RotatedRect> GenerateAlignedLots(
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
                return  Vector2.Distance((a + b) / 2, position);
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
           

            // Accept lot
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


    void DrawGrid(bool[,] grid, Vector2 origin, float cellSize)
    {
        int w = grid.GetLength(0);
        int h = grid.GetLength(1);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (!grid[x, y]) continue;

                Vector2 center = origin + new Vector2(
                    (x + 0.5f) * cellSize,
                    (y + 0.5f) * cellSize
                );

                float s = cellSize * 0.5f;

                Vector2 a = center + new Vector2(-s, -s);
                Vector2 b = center + new Vector2(s, -s);
                Vector2 c = center + new Vector2(s, s);
                Vector2 d = center + new Vector2(-s, s);

                List<Vector2> list = new List<Vector2>() { a, b, c, d };

                Debug.DrawLine(a, b, Color.green, 100f);
                Debug.DrawLine(b, c, Color.green, 100f);
                Debug.DrawLine(c, d, Color.green, 100f);
                Debug.DrawLine(d, a, Color.green, 100f);
            }
        }
    }

    public void Generate()
    {
        while (priorityQueue.Count > 0)
        {
            priorityQueue = priorityQueue.OrderByDescending(o => o.t).ToList();
            RoadSegment segment = priorityQueue.Last();
            priorityQueue.RemoveAt(priorityQueue.Count - 1);


            bool state = LocalConstraints(segment);

            if (state)
            {
                if (segmentList.Count < maxSegments)
                {
                    GlobalGoals(segment);
                }

                AddSegment(segment);
            }
        }


    }


    public void AddSegment(RoadSegment segment)
    {


        segmentList.Add(segment);

        if (segment.isBranch)
        {

            GameObject go = new GameObject();
            go.AddComponent<LineRenderer>();
            segment.line = go.GetComponent<LineRenderer>();
            float width = defaultSegmentWidth;
            if (segment.qa.isMotorway)
            {
                width = motorwaySegmentWidth;
            }

            segment.line.startWidth = width;
            segment.line.endWidth = width;
            segment.line.material = lineMaterial;
            segment.line.SetPosition(0, segment.ra.startLocation);
            segment.line.SetPosition(1, segment.ra.endLocation);


            //segment.RotRect.draw();
            //Color colour = Random.ColorHSV();
            //segment.line.startColor = colour;
            //segment.line.endColor = colour;




        }
        else
        {
            segment.line = segment.parent.line;
            segment.DrawSegment();
        }


        Node NodeStart = GetOrCreateNode(segment.ra.startLocation);
        Node NodeEnd = GetOrCreateNode(segment.ra.endLocation);

        Edge e = new Edge { a = NodeStart, b = NodeEnd };

        Edges.Add(e);

        NodeStart.Edges.Add(e);
        NodeEnd.Edges.Add(e);

        segment.edge = e;

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

                while (true)
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
                        angle = RandomAngle(straightAngleMean, straightAngleSD);
                    }
                    else
                    {
                        angle = 1 * RandomAngle(straightAngleMean, straightAngleSD);
                    }
                }

                branches.Add(lastSegment.ContinueRoad(angle));

                // Maybe Branch Motorway
                if (Random.value < motorwayBranchProbability)
                {
                    if (Random.value < .5f)
                    {
                        angle = RandomAngle(branchAngleMean, straightAngleSD);
                    }
                    else
                    {
                        angle = -1 * RandomAngle(branchAngleMean, straightAngleSD);
                    }

                    branches.Add(lastSegment.BranchRoad(angle, lastSegment.ra.distance, motorwaySegmentWidth, true));
                }
            }
            else if (Random.value < .75f)
            {
                branches.Add(lastSegment.ContinueRoad());
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
        List<RoadSegment> closeRoads = segmentList.Where((s) => segment.RotRect.Collides(s.RotRect)).ToList();

        Vector2? closestIntersection = null;
        RoadSegment otherSegment = null;
        foreach (RoadSegment s in closeRoads)
        {
            if (s != segment && s != segment.parent && s.parent != segment && s != segment.parent.parent)
            {
                if (segment.ra.startLocation == new Vector2(840, -100) && s.ra.endLocation == new Vector2(780, -100))
                {
                    Intersect(segment, s, true);
                }

                Vector2? intersection = Intersect(segment, s);
                if (intersection.HasValue && Vector2.Distance(intersection.Value, segment.ra.startLocation) > 0.01f)
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

            /*
            if (Mathf.Abs(Vector2.Dot((otherSegment.ra.endLocation-otherSegment.ra.startLocation).normalized, (segment.ra.endLocation - segment.ra.startLocation).normalized)) > 0.95f)
            {
                return false;
            }*/

            segment.qa.isSevered = true;
            segment.ra.endLocation = closestIntersection.Value;
            segment.ra.distance = Vector2.Distance(closestIntersection.Value, segment.ra.startLocation);

            segment.RotRect = new(segment);

            SplitSegment(otherSegment, closestIntersection.Value);
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


    public class RotatedRect
    {
        public List<Vector2> Vertices;
        public List<Vector2> Edges;
        public List<Vector2> Normals;

        public RotatedRect(RoadSegment rs, float Lookahead = .25f)
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

public class HalfEdge
{
    public Node start;
    public Node end;

    public HalfEdge(Node s, Node e)
    {
        start = s;
        end = e;
    }

    public override int GetHashCode()
    {
        return start.GetHashCode() ^ end.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is HalfEdge other)
        {
            return other.start == start && other.end == end;
        }
        return false;
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

}

