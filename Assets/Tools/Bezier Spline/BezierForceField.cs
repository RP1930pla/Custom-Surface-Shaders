using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System;
using static UnityEditor.FilePathAttribute;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BezierForceField : MonoBehaviour
{
    public Bounds bounds;
    BezierSpline[] bezierSplines;
    public int maxVoxelQuantity = 64;

    public Vector3 voxelGridSize;
    NativeArray<float3> voxelGridPositions;
    NativeArray<float4> voxelGridVelocity;

    
    NativeArray<CuadraticSpline.PointsList> splineListNative;

    struct CombinedSplines 
    {
        public CuadraticSpline.PointsList[] lists;
    }

    CombinedSplines[] splineList;

    [HideInInspector]
    public float voxelSize;
    public Vector3 scale;
    public Texture3D voxelTexture;

    public Vector3 Rotation;

    public void InitializeNativeArrays()
    {
        if (voxelGridPositions.IsCreated) voxelGridPositions.Dispose();
        if (voxelGridVelocity.IsCreated) voxelGridVelocity.Dispose();

        voxelGridPositions = new NativeArray<float3>((int)(voxelGridSize.x * voxelGridSize.y * voxelGridSize.z), Allocator.Persistent);
        voxelGridVelocity = new NativeArray<float4>((int)(voxelGridSize.x * voxelGridSize.y * voxelGridSize.z), Allocator.Persistent);
    }
    public void DisposeNativeArrayMemory()
    {
        if (voxelGridPositions.IsCreated) voxelGridPositions.Dispose();
        if (voxelGridVelocity.IsCreated) voxelGridVelocity.Dispose();
        if (splineListNative.IsCreated) splineListNative.Dispose();
    }

    private void OnDisable()
    {
        DisposeNativeArrayMemory();
    }
    private void OnEnable()
    {
        
    }

    void GetVoxelSize() 
    {
        float biggestSide = Mathf.Max(bounds.size.z, bounds.size.y, bounds.size.z);
        voxelSize = biggestSide / maxVoxelQuantity;
    }

    int3 Retrieve3DIndex(int i, Vector3 maxSize) 
    {
        int3 coord;
        coord.x = (int)((int) (i % (maxSize.x * maxSize.y * maxSize.z)) / (maxSize.z * maxSize.y));
        coord.y = (int)((i % (maxSize.y * maxSize.z)) / maxSize.z);
        coord.z = (int)(i % maxSize.z);
        return coord;
    }

    void UpdateVoxelGridPositions() 
    {
        int x = Mathf.RoundToInt(bounds.size.x / voxelSize);
        int y = Mathf.RoundToInt(bounds.size.y / voxelSize);
        int z = Mathf.RoundToInt(bounds.size.z / voxelSize);

        voxelGridSize = new Vector3(x, y, z);

        InitializeNativeArrays();

        for (int i = 0; i < voxelGridPositions.Length; i++) 
        {
            float3 index = Retrieve3DIndex(i, voxelGridSize);
            float halfSize = (voxelSize / 2);
            float3 position = bounds.min + (Vector3.one * halfSize);
            position = position + (voxelSize * index);
            //position += voxelSize * index;

            voxelGridPositions[i] = position;

        }

    }

    // Update is called once per frame
    void Update()
    {
        //GetVoxelSize();
        //UpdateVoxelGridPositions();
        
    }


    private void OnDrawGizmosSelected()
    {
        DrawBounds();
        //DrawVoxelGridGizmos();
    }

    void DrawVoxelGridGizmos()
    {
        if (voxelGridPositions.IsCreated) 
        {
            for (int i = 0; i < voxelGridPositions.Length; i++) 
            {
                Gizmos.DrawWireCube(voxelGridPositions[i], Vector3.one * voxelSize);
            }
        }
    }


    [ContextMenu("Process Texture / Multithreaded")]
    void UpdateTexture() 
    {
        GetSplinePoints();
        if (bezierSplines == null) return;

        GetVoxelSize();
        UpdateVoxelGridPositions();
        if (!voxelGridPositions.IsCreated) return;
        if (!voxelGridVelocity.IsCreated) return;
        if (!splineListNative.IsCreated) return;

        var updateVoxelVelocityJob = new UpdateVoxelGridVelocityJob
        {
            voxelPositions = voxelGridPositions,
            voxelVelocities = voxelGridVelocity,
            splineList = splineListNative,
        };

        JobHandle dependencyJobHandle = default;

        JobHandle updateVoxelVelocityJobHandle = updateVoxelVelocityJob.Schedule(voxelGridPositions.Length, 64);
        updateVoxelVelocityJobHandle.Complete();

        Color[] voxelColors = new Color[voxelGridPositions.Length];
        for (int i = 0; i < voxelGridVelocity.Length; i++)
        {
            voxelColors[i] = new Color(voxelGridVelocity[i].x, voxelGridVelocity[i].y, voxelGridVelocity[i].z, voxelGridVelocity[i].w);
        }

        voxelTexture = new Texture3D((int)voxelGridSize.z, (int)voxelGridSize.y, (int)voxelGridSize.x, TextureFormat.RGBAFloat, false);
        voxelTexture.SetPixels(voxelColors);
        voxelTexture.Apply();

        Shader.SetGlobalTexture("_Bezier_Volume", voxelTexture);
        Shader.SetGlobalVector("_MinVolume", bounds.min);
        Shader.SetGlobalVector("_MaxVolume", bounds.max);
    }

    [ContextMenu("Process Texture/ Single")]
    void UpdateTextureSingleThread() 
    {
        GetSplinePoints();
        if (bezierSplines == null) return;

        GetVoxelSize();
        UpdateVoxelGridPositions();
        if (!voxelGridPositions.IsCreated) return;
        if (!voxelGridVelocity.IsCreated) return;
        if (!splineListNative.IsCreated) return;

        for (int j = 0; j < voxelGridPositions.Length; j++)
        {
            float dt = 100000;
            int closestIndex = 0;
            float3 currentPosition = voxelGridPositions[j];
            for (int i = 0; i < splineListNative.Length; i++)
            {
                float ndt = math.distance(splineListNative[i].position, currentPosition);
                if (ndt < dt)
                {
                    dt = ndt;
                    closestIndex = i;
                    //splineSelected = i;
                }
            }
            float remappedDistance = math.remap(0, 50, 0, 1, dt);

            //voxelGridVelocity[j] = math.normalize(splineListNative[closestIndex].velocity);
            //voxelGridVelocity[j] = remappedDistance;
        }


        Color[] voxelColors = new Color[voxelGridPositions.Length];
        for (int i = 0; i < voxelGridVelocity.Length; i++)
        {
            voxelColors[i] = new Color(voxelGridVelocity[i].x, voxelGridVelocity[i].y, voxelGridVelocity[i].z, voxelGridVelocity[i].w);
        }

        voxelTexture = new Texture3D((int)voxelGridSize.x, (int)voxelGridSize.y, (int)voxelGridSize.z, TextureFormat.RGBA32_SIGNED, false);
        voxelTexture.SetPixels(voxelColors);
        voxelTexture.Apply();
    }

    void GetSplinePoints() 
    {
        bezierSplines = FindObjectsByType<BezierSpline>(FindObjectsSortMode.None);

        if (splineListNative.IsCreated) splineListNative.Dispose();

        int maxPoints = 0;

        if (bezierSplines != null) 
        {
            for (int i = 0; i < bezierSplines.Length; i++) 
            {
                maxPoints += bezierSplines[i].spline.pointsAmount;
            }
        }

        Debug.Log("Max Points: " + maxPoints);

        if (bezierSplines != null)
        {
            splineListNative = new NativeArray<CuadraticSpline.PointsList>(maxPoints, Allocator.Persistent);
            Debug.Log(bezierSplines.Length);

            List<CuadraticSpline.PointsList> points = new List<CuadraticSpline.PointsList> ();
            //CuadraticSpline.PointsList[] points = new CuadraticSpline.PointsList[maxPoints];
            //Debug.Log("Points Lenght: " + points.Length);
            int copyToIndex = 0;
            for (int i = 0;i < bezierSplines.Length; i++) 
            {
                    Debug.Log("Length of first Array" + bezierSplines[i].spline.pointsOnSpline.Length);
                    points.AddRange(bezierSplines[i].spline.pointsOnSpline);

            }
            splineListNative.CopyFrom(points.ToArray());
        }


    }
    

    void DrawBounds() 
    {
        Color color = Color.coral;
        color.a = 0.2f;
        Gizmos.color = color;
        bounds.center = transform.position;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }

    [BurstCompile]
    struct UpdateVoxelGridVelocityJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float3> voxelPositions;

        public NativeArray<float4> voxelVelocities;

        [ReadOnly]
        public NativeArray<CuadraticSpline.PointsList> splineList;
        public void Execute(int index) 
        {

            float dt = 100000;
            int closestIndex = 0;
            int splineSelected = 0;

            //for (int i = 0; i < splineList.Length; i++) 
            //{
            //    for (int j = 0; j < splineList[i].lists.Length; j++) 
            //    {
            //        float ndt = math.distancesq(splineList[i].lists[j].position, voxelPositions[index]);
            //        if (ndt < dt) 
            //        {
            //            dt = ndt;
            //            closestIndex = j;
            //            splineSelected = i;
            //        }
            //    }
            //}

            float4 voxelVelocity = voxelVelocities[index];
            float3 currentPosition = voxelPositions[index];
                for (int i = 0; i < splineList.Length; i++) 
                {
                    float ndt = math.distance(splineList[i].position, currentPosition);
                    if (ndt < dt)
                    {
                        dt = ndt;
                        closestIndex = i;
                        //splineSelected = i;
                    }
                }

            float remappedDistance = 1 - math.remap(0, 1, 0, 1, dt);
            remappedDistance = math.clamp(remappedDistance, 0, 1);
            remappedDistance = math.smoothstep(0.0f, 0.5f, remappedDistance);
            //voxelVelocity = 1-remappedDistance;
            voxelVelocity = new float4(splineList[closestIndex].velocity.x, splineList[closestIndex].velocity.y, splineList[closestIndex].velocity.z, remappedDistance);
            voxelVelocities[index] = math.normalize(voxelVelocity) ;




        }
    }

}

[CustomEditor(typeof(BezierForceField))]
public class BezierForceFieldEditor : Editor 
{
    BezierForceField script;
    public override void OnInspectorGUI()
    {
        script = (BezierForceField)target;
        base.OnInspectorGUI();
    }

    private void OnSceneViewGUI(SceneView sv)
    {
        script = (BezierForceField)target;
        if (script.voxelTexture != null && script != null)
        {
            //Transform transform = script.transform;
            //Vector3 size = new Vector3((int)script.voxelGridSize.x, script.voxelGridSize.y, script.voxelGridSize.z) * script.voxelSize;
            //size.x = size.x / ((int)script.voxelGridSize.x * 0.1f);
            //size.y = size.y / ((int)script.voxelGridSize.y * 0.1f);
            //size.z = size.z / ((int)script.voxelGridSize.z * 0.1f);
            //size *= 4;
            ////size.Scale(script.scale);

            //Matrix4x4 LtW = Matrix4x4.TRS(script.bounds.center, Quaternion.Euler(script.Rotation), size);
            
            //Handles.matrix = LtW;
            //Handles.DrawTexture3DVolume(script.voxelTexture, 1, 1);
            //Transform transform = script.transform;
            //Vector3 size = new Vector3((int)script.voxelGridSize.x, script.voxelGridSize.y, script.voxelGridSize.z) * script.voxelSize;
            //size.x = size.x / ((int)script.voxelGridSize.x * 0.1f);
            //size.y = size.y / ((int)script.voxelGridSize.y * 0.1f);
            //size.z = size.z / ((int)script.voxelGridSize.z * 0.1f);
            //size *= 4;
            ////size.Scale(script.scale);

            //Matrix4x4 LtW = Matrix4x4.TRS(script.bounds.center, Quaternion.Euler(script.Rotation), size);
            
            //Handles.matrix = LtW;
            //Handles.DrawTexture3DVolume(script.voxelTexture, 1, 1);
        }
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneViewGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneViewGUI;
    }
}