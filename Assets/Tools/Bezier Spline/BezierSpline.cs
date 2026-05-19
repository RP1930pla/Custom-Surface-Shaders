using NUnit.Framework;
using System;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class CuadraticSpline 
{

    [SerializeField]
    public Vector3[] controlPoints = new Vector3[4];

    //FIla x Columna
    [HideInInspector]
    public float4x4 splineMatrix = 
        new Matrix4x4(
            new float4(1,0,0,0),
            new float4(-3,3,0,0),
            new float4(3,-6,3,0),
            new float4(-1,3,-3,1)
        ).transpose;

    int pointsAmount;

    NativeArray<PointsList> pointsOnSplineNative;
    public struct PointsList
    {
        public float3 position;
        public float3 velocity;
    }

    [HideInInspector]
    [SerializeField]
    public PointsList[] pointsOnSpline;

    public float3 SamplePosition(float t)
    {
        float4x3 controlPointMatrix =
        new float4x3(
            new float4(controlPoints[0].x, controlPoints[1].x, controlPoints[2].x, controlPoints[3].x),
            new float4(controlPoints[0].y, controlPoints[1].y, controlPoints[2].y, controlPoints[3].y),
            new float4(controlPoints[0].z, controlPoints[1].z, controlPoints[2].z, controlPoints[3].z)
            );


        float4 A = new float4(1, t, Mathf.Pow(t, 2), Mathf.Pow(t, 3));
        float4 Mat = math.mul(A, splineMatrix);

        Mat.xyz = math.mul(Mat, controlPointMatrix);

        return Mat.xyz;

    }

    public float3 SampleVelocity(float t) 
    {
        float4x3 controlPointMatrix =
        new float4x3(
            new float4(controlPoints[0].x, controlPoints[1].x, controlPoints[2].x, controlPoints[3].x),
            new float4(controlPoints[0].y, controlPoints[1].y, controlPoints[2].y, controlPoints[3].y),
            new float4(controlPoints[0].z, controlPoints[1].z, controlPoints[2].z, controlPoints[3].z)
        );


        float4 A = new float4(0, 1, 2 * t, 3 * math.pow(t,2));
        float4 Mat = math.mul(A, splineMatrix);

        Mat.xyz = math.mul(Mat, controlPointMatrix);

        return Mat.xyz;
    }

    public float3 SampleAcceleration(float t)
    {
        float4x3 controlPointMatrix =
        new float4x3(
            new float4(controlPoints[0].x, controlPoints[1].x, controlPoints[2].x, controlPoints[3].x),
            new float4(controlPoints[0].y, controlPoints[1].y, controlPoints[2].y, controlPoints[3].y),
            new float4(controlPoints[0].z, controlPoints[1].z, controlPoints[2].z, controlPoints[3].z)
        );


        float4 A = new float4(0,0,2, 6*t);
        float4 Mat = math.mul(A, splineMatrix);

        Mat.xyz = math.mul(Mat, controlPointMatrix);

        return Mat.xyz;
    }


    public void InitializeNativeArrays() 
    {
        if (pointsOnSplineNative.IsCreated) return;
        pointsOnSplineNative = new NativeArray<PointsList>(pointsAmount, Allocator.Persistent);
    }
    public void DisposeNativeArrayMemory() 
    {
        if (pointsOnSplineNative.IsCreated) pointsOnSplineNative.Dispose();
    }

    public void UpdatePointsUsingJob() 
    {
        if (!pointsOnSplineNative.IsCreated) return;
        var updatePointJob = new UpdatePointsOnSplineJob
        {
            points = pointsOnSplineNative,
            p0 = controlPoints[0],
            p1 = controlPoints[1],
            p2 = controlPoints[2],
            p3 = controlPoints[3],
            splineMatrix = splineMatrix
        };

        JobHandle updatePointsJobHandle = updatePointJob.Schedule(pointsOnSplineNative.Length, 64);
        updatePointsJobHandle.Complete();

        pointsOnSpline = pointsOnSplineNative.ToArray();
    }

    [BurstCompile]
    struct UpdatePointsOnSplineJob : IJobParallelFor 
    {
        public NativeArray<PointsList> points;
        
        public float3 p0;
        public float3 p1;
        public float3 p2;
        public float3 p3;

        public float4x4 splineMatrix;

        public float3 SamplePosition(float t)
        {
            float4x3 controlPointMatrix =
            new float4x3(
                new float4(p0.x, p1.x, p2.x, p3.x),
                new float4(p0.y, p1.y, p2.y, p3.y),
                new float4(p0.z, p1.z, p2.z, p3.z)
                );


            float4 A = new float4(1, t, Mathf.Pow(t, 2), Mathf.Pow(t, 3));
            float4 Mat = math.mul(A, splineMatrix);

            Mat.xyz = math.mul(Mat, controlPointMatrix);

            return Mat.xyz;

        }

        public float3 SampleVelocity(float t)
        {
            float4x3 controlPointMatrix =
               new float4x3(
                   new float4(p0.x, p1.x, p2.x, p3.x),
                   new float4(p0.y, p1.y, p2.y, p3.y),
                   new float4(p0.z, p1.z, p2.z, p3.z)
                   );


            float4 A = new float4(0, 1, 2 * t, 3 * math.pow(t, 2));
            float4 Mat = math.mul(A, splineMatrix);

            Mat.xyz = math.mul(Mat, controlPointMatrix);

            return Mat.xyz;
        }

        public void Execute(int index) 
        {
            PointsList point = points[index];

            float segmentTime = 1f / points.Length;
            float time = index * segmentTime;
            point.position = SamplePosition(time);
            point.velocity = SampleVelocity(time);
            points[index] = point;
        }
    }

    public CuadraticSpline(float3 origin, int amountOfPoints) 
    {
        controlPoints[0] = origin;
        controlPoints[1] = origin + new float3(1f, 0, 1f);

        controlPoints[3] = origin + new float3(10, 0, 0);
        controlPoints[2] = controlPoints[3] - new Vector3(1f, 0, -1f);
        pointsAmount = amountOfPoints;
        pointsOnSpline = new PointsList[amountOfPoints];
    }



}

[ExecuteInEditMode]
[Serializable]
public class BezierSpline : MonoBehaviour
{
    [SerializeField]
    public CuadraticSpline spline;
    private Bounds bounds;
    private void OnEnable()
    {
        if (spline == null) 
        {
            spline = new CuadraticSpline(transform.position, 30);
        }
        spline.InitializeNativeArrays();
    }

    private void OnDisable()
    {
        spline.DisposeNativeArrayMemory();
    }

    //private void Update()
    //{
    //    spline.UpdatePointsUsingJob();
    //}

    private void OnDrawGizmos()
    {
        if (spline == null) 
        {
            return;
        }
        spline.UpdatePointsUsingJob();

        Gizmos.color = Color.white;
        //Gizmos.DrawLineList(spline.pointsList);

        for (int i = 0; i < spline.pointsOnSpline.Length; i++) 
        {
            int clampedNext = Mathf.Clamp(i+1, 0, spline.pointsOnSpline.Length-1);
            Gizmos.DrawLine(spline.pointsOnSpline[i].position, spline.pointsOnSpline[clampedNext].position);
        }

        //Gizmos.DrawLine(spline.pointsOnSpline[spline.pointsOnSpline.Length-1].position, spline.controlPoints[3]);

        DrawControlPointsLines();
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawCube(bounds.center, bounds.size);
    }

    public void UpdateBounds() 
    {
        bounds = new Bounds();
        bounds.size = Vector3.zero;
        for (int i = 0; i < spline.pointsOnSpline.Length; i++)
        {
            bounds.Encapsulate(spline.pointsOnSpline[i].position);
        }

        bounds.Encapsulate(spline.controlPoints[3]);
        
    }

    void DrawControlPointsLines() 
    {
        for (int i = 0; i < spline.controlPoints.Length; i++)
        {
            if (i == 1 || i == 2)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(new Vector3(spline.controlPoints[i].x, spline.controlPoints[i].y, spline.controlPoints[i].z), 0.05f);
            }
            else
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(new Vector3(spline.controlPoints[i].x, spline.controlPoints[i].y, spline.controlPoints[i].z), 0.1f);
            }
        }

        Gizmos.color = Color.orange;
        Gizmos.DrawLine(spline.controlPoints[0], spline.controlPoints[1]);
        Gizmos.DrawLine(spline.controlPoints[2], spline.controlPoints[3]);
    }

}

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineEditor : Editor 
{
    BezierSpline script;
    public override void OnInspectorGUI()
    {
        script = (BezierSpline)target;
        base.OnInspectorGUI();
    }

    private void OnSceneGUI()
    {
        Quaternion qt = Quaternion.identity;
        //Handles.matrix = script.transform.localToWorldMatrix;
        if (script.spline != null && script != null) 
        {
            script.spline.controlPoints[0] = script.transform.position;
            script.spline.controlPoints[1] = Handles.PositionHandle(script.spline.controlPoints[1], qt);
            script.spline.controlPoints[2] = Handles.PositionHandle(script.spline.controlPoints[2], qt);
            script.spline.controlPoints[3] = Handles.PositionHandle(script.spline.controlPoints[3], qt);
            script.UpdateBounds();
        }

        //Handles.PositionHandle(script.spline.controlPoints[1], qt);
    }

}
