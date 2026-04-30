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
    }

    public void Generate()
    {
        while ( priorityQueue.Count > 0) 
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


            segment.RotRect.draw();
            //Color colour = Random.ColorHSV();
            //segment.line.startColor = colour;
            //segment.line.endColor = colour;
        }
        else
        {
            segment.line = segment.parent.line;
            segment.DrawSegment();
        } 



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
                if (intersection.HasValue && intersection != segment.ra.startLocation)
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
        }


        return true;
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


        public RoadSegment(int _t, RoadAttribute _ra, QueryAttribute _qa)
        {
            t = _t;
            ra = _ra;
            qa = _qa;
            parent = this;
            branches = new List<RoadSegment>();

            Vector2 widthFactor = (new Vector2(1, 1) - (_ra.endLocation - _ra.startLocation).normalized) * _qa.width/2;

            RotRect = new(this);



            rectangle.xMin = Mathf.Min(ra.startLocation.x, ra.endLocation.x) - widthFactor.x - .25f * (Mathf.Abs(ra.startLocation.x - ra.endLocation.x) + widthFactor.x);
            rectangle.xMax = Mathf.Max(ra.startLocation.x, ra.endLocation.x) + widthFactor.x +  .25f * (Mathf.Abs(ra.startLocation.x - ra.endLocation.x) + widthFactor.x);
            rectangle.yMin = Mathf.Min(ra.startLocation.y, ra.endLocation.y) - widthFactor.y - .25f * (Mathf.Abs(ra.startLocation.y - ra.endLocation.y) + widthFactor.y);
            rectangle.yMax = Mathf.Max(ra.startLocation.y, ra.endLocation.y) + widthFactor.y +  .25f * (Mathf.Abs(ra.startLocation.y - ra.endLocation.y) + widthFactor.y);

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
            line.SetPosition(line.positionCount-1, ra.endLocation);

            RotRect.draw();
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
                

                if ((t0 >= 0 && t0 <= 1) ||(t1 >= 0 && t1<=1))
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

        public RotatedRect(RoadSegment rs)
        {
            Vector2 Norm = (rs.ra.endLocation - rs.ra.startLocation).normalized;
            Vector2 widthFactor = Vector2.Perpendicular(Norm)  * rs.qa.width / 2;

            Vertices = new();
            Edges = new();
            Normals = new();

            Vertices.Add(rs.ra.startLocation +  widthFactor);
            Vertices.Add(rs.ra.startLocation - widthFactor);
            Vertices.Add(rs.ra.endLocation - widthFactor + Norm * .25f * rs.ra.distance);
            Vertices.Add(rs.ra.endLocation + widthFactor + Norm * .25f * rs.ra.distance);

            Edges.Add(Vertices[1] - Vertices[0]);
            Edges.Add(Vertices[2] - Vertices[1]);
            Edges.Add(Vertices[3] - Vertices[2]);
            Edges.Add(Vertices[0] - Vertices[1]);

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

                bool overlap = (projection1.y-projection1.x + projection2.y - projection2.x) > (Mathf.Max(projection2.y, projection1.y) - Mathf.Min(projection2.x, projection1.y));

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

        public Vector2 Project( Vector2 Axes)
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
        endLocation = startLocation + new Vector2(Mathf.Cos(_angle*Mathf.Deg2Rad), Mathf.Sin(_angle*Mathf.Deg2Rad)) * distance;
    }
}

public struct QueryAttribute
{
    public bool isMotorway;
    public bool isSevered;
    public float width;

    public QueryAttribute(bool motorway = false, float width = -1,  bool severed = false)
    {
        this.width = width;
        isMotorway = motorway;
        isSevered = severed;
    }

}

