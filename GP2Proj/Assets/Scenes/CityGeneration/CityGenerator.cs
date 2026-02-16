using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
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
                new QueryAttribute(true)
            );
        priorityQueue.Add(segment);
        Generate();
    }

    public void Generate()
    {
        while ( priorityQueue.Count > 0 && segmentList.Count < maxSegments) 
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
        }

        segmentList.Last().line.startColor = Color.gold;
        segmentList.Last().line.endColor = Color.gold;
        List<RoadSegment> closeRoads = segmentList.Where((s) => (segmentList.Last().rectangle.Overlaps(s.rectangle))).ToList();

        foreach (RoadSegment segment in closeRoads) 
        { 
            if (segment.line != segmentList.Last().line)
            {
                segment.line.startColor = Color.red;
                segment.line.endColor = Color.red;  
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

        return Mathf.Clamp(std * s + mean, minValue, maxValue);
    }
    public void GlobalGoals(RoadSegment lastSegment)
    {
        List<RoadSegment> branches = new List<RoadSegment>();
        Vector2 point = lastSegment.ra.endLocation;
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
                    angle = 1*RandomAngle(straightAngleMean, straightAngleSD);   
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
                    angle = -1*RandomAngle(branchAngleMean, straightAngleSD);
                }

                branches.Add(lastSegment.BranchRoad(angle, lastSegment.ra.distance, true));
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



            branches.Add(lastSegment.BranchRoad(angle, defaultSegmentLength, false, delay));
        }

        foreach (RoadSegment branch in branches)
        {
            priorityQueue.Add(branch);
        }
    }

    public bool LocalConstraints(RoadSegment segment)
    {
        List<RoadSegment> closeRoads = segmentList.Where((s) => segment.rectangle.Overlaps(s.rectangle)).ToList();
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

        public RoadSegment(int _t, RoadAttribute _ra, QueryAttribute _qa)
        {
            t = _t;
            ra = _ra;
            qa = _qa;
            branches = new List<RoadSegment>();

            rectangle.xMin = Mathf.Min(ra.startLocation.x, ra.endLocation.x) - .25f*Mathf.Abs(ra.startLocation.x-ra.endLocation.x);
            rectangle.xMax = Mathf.Max(ra.startLocation.x, ra.endLocation.x) - .25f * Mathf.Abs(ra.startLocation.x - ra.endLocation.x);
            rectangle.yMin = Mathf.Min(ra.startLocation.y, ra.endLocation.y) - .25f * Mathf.Abs(ra.startLocation.y - ra.endLocation.y);
            rectangle.yMax = Mathf.Max(ra.startLocation.y, ra.endLocation.y) - .25f * Mathf.Abs(ra.startLocation.y - ra.endLocation.y);
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

        public RoadSegment BranchRoad(float angle, float length, bool motorway = false, int delay = 1)
        {
            RoadAttribute newRa = new RoadAttribute(ra.endLocation, length, ra.angle + angle);
            QueryAttribute newQuery = new QueryAttribute(motorway);
            RoadSegment newSegment = new RoadSegment(t + delay, newRa, newQuery);
            branches.Add(newSegment);
            newSegment.parent = this;
            return newSegment;
        }

        public void DrawSegment()
        {
            line.positionCount += 1;
            line.SetPosition(line.positionCount-1, ra.endLocation);
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

    public QueryAttribute(bool motorway = false, bool severed = false)
    {
        isMotorway = motorway;
        isSevered = severed;
    }

}