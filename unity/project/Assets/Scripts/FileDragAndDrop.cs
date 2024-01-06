using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using B83.Win32;
using System;
using System.Diagnostics;
using GaussianSplatting.Runtime;
using System.Security.Cryptography;
using System.IO;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using System.Text;
using Unity.Burst.Intrinsics;
using Unity.Profiling.LowLevel;
using Unity.Profiling;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

//using static GaussianSplatting.Editor.GaussianSplatAssetCreator;
//######   #######  #     #     #     ######   #######         #     #        #            #     #  #######  ######   #######     #     #         #####  
//#     #  #        #  #  #    # #    #     #  #              # #    #        #            ##   ##  #     #  #     #     #       # #    #        #     # 
//#     #  #        #  #  #   #   #   #     #  #             #   #   #        #            # # # #  #     #  #     #     #      #   #   #        #       
//######   #####    #  #  #  #     #  ######   #####        #     #  #        #            #  #  #  #     #  ######      #     #     #  #         #####  
//#     #  #        #  #  #  #######  #   #    #            #######  #        #            #     #  #     #  #   #       #     #######  #              # 
//#     #  #        #  #  #  #     #  #    #   #            #     #  #        #            #     #  #     #  #    #      #     #     #  #        #     #
//######   #######   ## ##   #     #  #     #  #######      #     #  #######  #######      #     #  #######  #     #     #     #     #  #######   #####  

// This is all terrible coding, do not try to understand and do not copy for your own sanity
public class FileDragAndDrop : MonoBehaviour
{

    enum DataQuality
    {
        VeryHigh,
        High,
        Medium,
        Low,
        VeryLow,
        Custom,
    }

    enum ColorFormat
    {
        Float32x4,
        Float16x4,
        Norm8x4,
        BC7,
    }

    const string kCamerasJson = "cameras.json";
    const string kPrefQuality = "nesnausk.GaussianSplatting.CreatorQuality";
    const string kPrefOutputFolder = "nesnausk.GaussianSplatting.CreatorOutputFolder";
    string m_ErrorMessage;
    string m_PrevPlyPath;
    int m_PrevVertexCount;
    long m_PrevFileSize;

    [SerializeField] string m_InputFile;
    [SerializeField] bool m_ImportCameras = true;

    [SerializeField] string m_OutputFolder = "Assets/GaussianAssets";
    [SerializeField] DataQuality m_Quality = DataQuality.VeryHigh;
    [SerializeField] GaussianSplatAsset.VectorFormat m_FormatPos;
    [SerializeField] GaussianSplatAsset.VectorFormat m_FormatScale;
    [SerializeField] GaussianSplatAsset.SHFormat m_FormatSH;
    [SerializeField] ColorFormat m_FormatColor;


    List<string> log = new List<string>();
    void OnEnable()
    {
        // must be installed on the main thread to get the right thread id.
        UnityDragAndDropHook.InstallHook();
        UnityDragAndDropHook.OnDroppedFiles += OnFiles;
    }
    void OnDisable()
    {
        UnityDragAndDropHook.UninstallHook();
    }

    void OnFiles(List<string> aFiles, Vector2 aPos)
    {
        // do something with the dropped file names. aPos will contain the 
        // mouse position within the window where the files has been dropped.
        string str = "Dropped " + aFiles.Count + " files at: " + aPos + "\n\t" +
            aFiles.Aggregate((a, b) => a + "\n\t" + b);

        int iters = 7000;

        foreach (var file in aFiles)
        {
            log.Add(file);
            string baseName = Path.GetFileNameWithoutExtension(file);
            string args = "/k " + @"dist\conv_train\conv_train.exe " + "-s " + file + " " + "--iterations=" + iters.ToString() + " -m " + "output/" + baseName;
            Process conv = new Process();
            conv.StartInfo.FileName = "cmd.exe";
            conv.StartInfo.Arguments = args;
            conv.Start();
            conv.WaitForExit();
            log.Add("Done");


            string path = "output/" + baseName + "/point_cloud/iteration_7000/point_cloud.ply";
            m_InputFile = path;
            m_OutputFolder = @"./Assets/Resources";
            m_Quality = DataQuality.VeryHigh;
  

            ApplyQualityLevel();
            CreateAsset(path,baseName);
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("clear log"))
            log.Clear();
        foreach (var s in log)
            GUILayout.Label(s);
    }


    // Mortals beware


    void ApplyQualityLevel()
    {
        switch (m_Quality)
        {
            case DataQuality.Custom:
                break;
            case DataQuality.VeryLow: // 18.62x smaller, 32.27 PSNR
                m_FormatPos = GaussianSplatAsset.VectorFormat.Norm11;
                m_FormatScale = GaussianSplatAsset.VectorFormat.Norm6;
                m_FormatColor = ColorFormat.BC7;
                m_FormatSH = GaussianSplatAsset.SHFormat.Cluster4k;
                break;
            case DataQuality.Low: // 14.01x smaller, 35.17 PSNR
                m_FormatPos = GaussianSplatAsset.VectorFormat.Norm11;
                m_FormatScale = GaussianSplatAsset.VectorFormat.Norm6;
                m_FormatColor = ColorFormat.Norm8x4;
                m_FormatSH = GaussianSplatAsset.SHFormat.Cluster16k;
                break;
            case DataQuality.Medium: // 5.14x smaller, 47.46 PSNR
                m_FormatPos = GaussianSplatAsset.VectorFormat.Norm11;
                m_FormatScale = GaussianSplatAsset.VectorFormat.Norm11;
                m_FormatColor = ColorFormat.Norm8x4;
                m_FormatSH = GaussianSplatAsset.SHFormat.Norm6;
                break;
            case DataQuality.High: // 2.94x smaller, 57.77 PSNR
                m_FormatPos = GaussianSplatAsset.VectorFormat.Norm16;
                m_FormatScale = GaussianSplatAsset.VectorFormat.Norm16;
                m_FormatColor = ColorFormat.Float16x4;
                m_FormatSH = GaussianSplatAsset.SHFormat.Norm11;
                break;
            case DataQuality.VeryHigh: // 1.05x smaller
                m_FormatPos = GaussianSplatAsset.VectorFormat.Float32;
                m_FormatScale = GaussianSplatAsset.VectorFormat.Float32;
                m_FormatColor = ColorFormat.Float32x4;
                m_FormatSH = GaussianSplatAsset.SHFormat.Float32;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public struct InputSplatData
    {
        public Vector3 pos;
        public Vector3 nor;
        public Vector3 dc0;
        public Vector3 sh1, sh2, sh3, sh4, sh5, sh6, sh7, sh8, sh9, shA, shB, shC, shD, shE, shF;
        public float opacity;
        public Vector3 scale;
        public Quaternion rot;
    }



    unsafe void CreateAsset(string path,string baseName)
    {
        m_ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(m_InputFile))
        {
            log.Add($"Select input PLY file {m_InputFile}");
            return;
        }

        if (string.IsNullOrWhiteSpace(m_OutputFolder))
        {
            log.Add($"Output folder must be within project, was '{m_OutputFolder}'");
            return;
        }
        Directory.CreateDirectory(m_OutputFolder);

        GaussianSplatAsset.CameraInfo[] cameras = LoadJsonCamerasFile(m_InputFile, m_ImportCameras);
        using NativeArray<InputSplatData> inputSplats = LoadPLYSplatFile(m_InputFile);
        if (inputSplats.Length == 0)
        {
            //EditorUtility.ClearProgressBar();
            return;
        }

        float3 boundsMin, boundsMax;
        var boundsJob = new CalcBoundsJob
        {
            m_BoundsMin = &boundsMin,
            m_BoundsMax = &boundsMax,
            m_SplatData = inputSplats
        };
        boundsJob.Schedule().Complete();

        ReorderMorton(inputSplats, boundsMin, boundsMax);

        // cluster SHs
        NativeArray<int> splatSHIndices = default;
        NativeArray<GaussianSplatAsset.SHTableItemFloat16> clusteredSHs = default;
        if (m_FormatSH >= GaussianSplatAsset.SHFormat.Cluster64k)
        {
            ClusterSHs(inputSplats, m_FormatSH, out clusteredSHs, out splatSHIndices);
        }


        GaussianSplatAsset asset = ScriptableObject.CreateInstance<GaussianSplatAsset>();
        asset.name = baseName;
        asset.m_Cameras = cameras;
        asset.m_BoundsMin = boundsMin;
        asset.m_BoundsMax = boundsMax;

        asset.m_SplatCount = inputSplats.Length;
        asset.m_FormatVersion = GaussianSplatAsset.kCurrentVersion;
        asset.m_PosFormat = m_FormatPos;
        asset.m_ScaleFormat = m_FormatScale;
        asset.m_SHFormat = m_FormatSH;
        asset.m_DataHash = new Hash128((uint)asset.m_SplatCount, (uint)asset.m_FormatVersion, 0, 0);
        string pathChunk = $"{m_OutputFolder}/{baseName}_chk.bytes";
        string pathPos = $"{m_OutputFolder}/{baseName}_pos.bytes";
        string pathOther = $"{m_OutputFolder}/{baseName}_oth.bytes";
        string pathCol = $"{m_OutputFolder}/{baseName}_col.bytes";
        string pathSh = $"{m_OutputFolder}/{baseName}_shs.bytes";
        LinearizeData(inputSplats);
        CreateChunkData(inputSplats, pathChunk, ref asset.m_DataHash, m_FormatSH);
        CreatePositionsData(inputSplats, pathPos, ref asset.m_DataHash);
        CreateOtherData(inputSplats, pathOther, ref asset.m_DataHash, splatSHIndices);
        CreateColorData(inputSplats, pathCol, ref asset.m_DataHash, out asset.m_ColorWidth, out asset.m_ColorHeight, out asset.m_ColorFormat);
        CreateSHData(inputSplats, pathSh, ref asset.m_DataHash, clusteredSHs);

        splatSHIndices.Dispose();
        clusteredSHs.Dispose();

        // files are created, import them so we can get to the imported objects, ugh
        //AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

        asset.m_ChunkData = Resources.Load<TextAsset>($"{baseName}_chk");
        asset.m_PosData = Resources.Load<TextAsset>($"{baseName}_pos");
        asset.m_OtherData = Resources.Load<TextAsset>($"{baseName}_oth");
        asset.m_ColorData = Resources.Load<TextAsset>($"{baseName}_col");
        asset.m_SHData = Resources.Load<TextAsset>($"{baseName}_shs");

        var assetPath = $"{m_OutputFolder}/{baseName}.json";

        string json_obj = JsonUtility.ToJson(asset);
        File.WriteAllText(assetPath, json_obj);  
        //var savedAsset = CreateOrReplaceAsset(asset, assetPath);

        //AssetDatabase.SaveAssets();
        //EditorUtility.ClearProgressBar();

        //Selection.activeObject = savedAsset;
    }

    unsafe NativeArray<InputSplatData> LoadPLYSplatFile(string plyPath)
    {
        NativeArray<InputSplatData> data = default;
        if (!File.Exists(plyPath))
        {
            m_ErrorMessage = $"Did not find {plyPath} file";
            return data;
        }

        int splatCount;
        int vertexStride;
        NativeArray<byte> verticesRawData;
        try
        {
            PLYFileReader.ReadFile(plyPath, out splatCount, out vertexStride, out _, out verticesRawData);
        }
        catch (Exception ex)
        {
            m_ErrorMessage = ex.Message;
            return data;
        }

        if (UnsafeUtility.SizeOf<InputSplatData>() != vertexStride)
        {
            m_ErrorMessage = $"PLY vertex size mismatch, expected {UnsafeUtility.SizeOf<InputSplatData>()} but file has {vertexStride}";
            return data;
        }

        // reorder SHs
        NativeArray<float> floatData = verticesRawData.Reinterpret<float>(1);
        ReorderSHs(splatCount, (float*)floatData.GetUnsafePtr());

        return verticesRawData.Reinterpret<InputSplatData>(1);
    }

    [BurstCompile]
    static unsafe void ReorderSHs(int splatCount, float* data)
    {
        int splatStride = UnsafeUtility.SizeOf<InputSplatData>() / 4;
        int shStartOffset = 9, shCount = 15;
        float* tmp = stackalloc float[shCount * 3];
        int idx = shStartOffset;
        for (int i = 0; i < splatCount; ++i)
        {
            for (int j = 0; j < shCount; ++j)
            {
                tmp[j * 3 + 0] = data[idx + j];
                tmp[j * 3 + 1] = data[idx + j + shCount];
                tmp[j * 3 + 2] = data[idx + j + shCount * 2];
            }

            for (int j = 0; j < shCount * 3; ++j)
            {
                data[idx + j] = tmp[j];
            }

            idx += splatStride;
        }
    }

    [BurstCompile]
    struct CalcBoundsJob : IJob
    {
        [NativeDisableUnsafePtrRestriction] public unsafe float3* m_BoundsMin;
        [NativeDisableUnsafePtrRestriction] public unsafe float3* m_BoundsMax;
        [ReadOnly] public NativeArray<InputSplatData> m_SplatData;

        public unsafe void Execute()
        {
            float3 boundsMin = float.PositiveInfinity;
            float3 boundsMax = float.NegativeInfinity;

            for (int i = 0; i < m_SplatData.Length; ++i)
            {
                float3 pos = m_SplatData[i].pos;
                boundsMin = math.min(boundsMin, pos);
                boundsMax = math.max(boundsMax, pos);
            }
            *m_BoundsMin = boundsMin;
            *m_BoundsMax = boundsMax;
        }
    }


    [BurstCompile]
    struct ReorderMortonJob : IJobParallelFor
    {
        const float kScaler = (float)((1 << 21) - 1);
        public float3 m_BoundsMin;
        public float3 m_InvBoundsSize;
        [ReadOnly] public NativeArray<InputSplatData> m_SplatData;
        public NativeArray<(ulong, int)> m_Order;

        public void Execute(int index)
        {
            float3 pos = ((float3)m_SplatData[index].pos - m_BoundsMin) * m_InvBoundsSize * kScaler;
            uint3 ipos = (uint3)pos;
            ulong code = GaussianUtils.MortonEncode3(ipos);
            m_Order[index] = (code, index);
        }
    }

    struct OrderComparer : IComparer<(ulong, int)>
    {
        public int Compare((ulong, int) a, (ulong, int) b)
        {
            if (a.Item1 < b.Item1) return -1;
            if (a.Item1 > b.Item1) return +1;
            return a.Item2 - b.Item2;
        }
    }

    static void ReorderMorton(NativeArray<InputSplatData> splatData, float3 boundsMin, float3 boundsMax)
    {
        ReorderMortonJob order = new ReorderMortonJob
        {
            m_SplatData = splatData,
            m_BoundsMin = boundsMin,
            m_InvBoundsSize = 1.0f / (boundsMax - boundsMin),
            m_Order = new NativeArray<(ulong, int)>(splatData.Length, Allocator.TempJob)
        };
        order.Schedule(splatData.Length, 4096).Complete();
        order.m_Order.Sort(new OrderComparer());

        NativeArray<InputSplatData> copy = new(order.m_SplatData, Allocator.TempJob);
        for (int i = 0; i < copy.Length; ++i)
            order.m_SplatData[i] = copy[order.m_Order[i].Item2];
        copy.Dispose();

        order.m_Order.Dispose();
    }

    [BurstCompile]
    static unsafe void GatherSHs(int splatCount, InputSplatData* splatData, float* shData)
    {
        for (int i = 0; i < splatCount; ++i)
        {
            UnsafeUtility.MemCpy(shData, ((float*)splatData) + 9, 15 * 3 * sizeof(float));
            splatData++;
            shData += 15 * 3;
        }
    }

    [BurstCompile]
    struct ConvertSHClustersJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> m_Input;
        public NativeArray<GaussianSplatAsset.SHTableItemFloat16> m_Output;
        public void Execute(int index)
        {
            var addr = index * 15;
            GaussianSplatAsset.SHTableItemFloat16 res;
            res.sh1 = new half3(m_Input[addr + 0]);
            res.sh2 = new half3(m_Input[addr + 1]);
            res.sh3 = new half3(m_Input[addr + 2]);
            res.sh4 = new half3(m_Input[addr + 3]);
            res.sh5 = new half3(m_Input[addr + 4]);
            res.sh6 = new half3(m_Input[addr + 5]);
            res.sh7 = new half3(m_Input[addr + 6]);
            res.sh8 = new half3(m_Input[addr + 7]);
            res.sh9 = new half3(m_Input[addr + 8]);
            res.shA = new half3(m_Input[addr + 9]);
            res.shB = new half3(m_Input[addr + 10]);
            res.shC = new half3(m_Input[addr + 11]);
            res.shD = new half3(m_Input[addr + 12]);
            res.shE = new half3(m_Input[addr + 13]);
            res.shF = new half3(m_Input[addr + 14]);
            res.shPadding = default;
            m_Output[index] = res;
        }
    }
    static bool ClusterSHProgress(float val)
    {
        return true;
    }

    static unsafe void ClusterSHs(NativeArray<InputSplatData> splatData, GaussianSplatAsset.SHFormat format, out NativeArray<GaussianSplatAsset.SHTableItemFloat16> shs, out NativeArray<int> shIndices)
    {
        shs = default;
        shIndices = default;

        int shCount = GaussianSplatAsset.GetSHCount(format, splatData.Length);
        if (shCount >= splatData.Length) // no need to cluster, just use raw data
            return;

        const int kShDim = 15 * 3;
        const int kBatchSize = 2048;
        float passesOverData = format switch
        {
            GaussianSplatAsset.SHFormat.Cluster64k => 0.3f,
            GaussianSplatAsset.SHFormat.Cluster32k => 0.4f,
            GaussianSplatAsset.SHFormat.Cluster16k => 0.5f,
            GaussianSplatAsset.SHFormat.Cluster8k => 0.8f,
            GaussianSplatAsset.SHFormat.Cluster4k => 1.2f,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };

        float t0 = Time.realtimeSinceStartup;
        NativeArray<float> shData = new(splatData.Length * kShDim, Allocator.Persistent);
        GatherSHs(splatData.Length, (InputSplatData*)splatData.GetUnsafeReadOnlyPtr(), (float*)shData.GetUnsafePtr());

        NativeArray<float> shMeans = new(shCount * kShDim, Allocator.Persistent);
        shIndices = new(splatData.Length, Allocator.Persistent);

        KMeansClustering.Calculate(kShDim, shData, kBatchSize, passesOverData, ClusterSHProgress, shMeans, shIndices);
        shData.Dispose();

        shs = new NativeArray<GaussianSplatAsset.SHTableItemFloat16>(shCount, Allocator.Persistent);

        ConvertSHClustersJob job = new ConvertSHClustersJob
        {
            m_Input = shMeans.Reinterpret<float3>(4),
            m_Output = shs
        };
        job.Schedule(shCount, 256).Complete();
        shMeans.Dispose();
        float t1 = Time.realtimeSinceStartup;
    }

    [BurstCompile]
    struct LinearizeDataJob : IJobParallelFor
    {
        public NativeArray<InputSplatData> splatData;
        public void Execute(int index)
        {
            var splat = splatData[index];

            // rot
            var q = splat.rot;
            var qq = GaussianUtils.NormalizeSwizzleRotation(new float4(q.x, q.y, q.z, q.w));
            qq = GaussianUtils.PackSmallest3Rotation(qq);
            splat.rot = new Quaternion(qq.x, qq.y, qq.z, qq.w);

            // scale
            splat.scale = GaussianUtils.LinearScale(splat.scale);
            // transform scale to be more uniformly distributed
            splat.scale = math.pow(splat.scale, 1.0f / 8.0f);

            // color
            splat.dc0 = GaussianUtils.SH0ToColor(splat.dc0);
            splat.opacity = GaussianUtils.SquareCentered01(GaussianUtils.Sigmoid(splat.opacity));

            splatData[index] = splat;
        }
    }

    static void LinearizeData(NativeArray<InputSplatData> splatData)
    {
        LinearizeDataJob job = new LinearizeDataJob();
        job.splatData = splatData;
        job.Schedule(splatData.Length, 4096).Complete();
    }

    [BurstCompile]
    struct CalcChunkDataJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<InputSplatData> splatData;
        public NativeArray<GaussianSplatAsset.ChunkInfo> chunks;
        public bool keepRawSHs;

        public void Execute(int chunkIdx)
        {
            float3 chunkMinpos = float.PositiveInfinity;
            float3 chunkMinscl = float.PositiveInfinity;
            float4 chunkMincol = float.PositiveInfinity;
            float3 chunkMinshs = float.PositiveInfinity;
            float3 chunkMaxpos = float.NegativeInfinity;
            float3 chunkMaxscl = float.NegativeInfinity;
            float4 chunkMaxcol = float.NegativeInfinity;
            float3 chunkMaxshs = float.NegativeInfinity;

            int splatBegin = math.min(chunkIdx * GaussianSplatAsset.kChunkSize, splatData.Length);
            int splatEnd = math.min((chunkIdx + 1) * GaussianSplatAsset.kChunkSize, splatData.Length);

            // calculate data bounds inside the chunk
            for (int i = splatBegin; i < splatEnd; ++i)
            {
                InputSplatData s = splatData[i];
                chunkMinpos = math.min(chunkMinpos, s.pos);
                chunkMinscl = math.min(chunkMinscl, s.scale);
                chunkMincol = math.min(chunkMincol, new float4(s.dc0, s.opacity));
                chunkMinshs = math.min(chunkMinshs, s.sh1);
                chunkMinshs = math.min(chunkMinshs, s.sh2);
                chunkMinshs = math.min(chunkMinshs, s.sh3);
                chunkMinshs = math.min(chunkMinshs, s.sh4);
                chunkMinshs = math.min(chunkMinshs, s.sh5);
                chunkMinshs = math.min(chunkMinshs, s.sh6);
                chunkMinshs = math.min(chunkMinshs, s.sh7);
                chunkMinshs = math.min(chunkMinshs, s.sh8);
                chunkMinshs = math.min(chunkMinshs, s.sh9);
                chunkMinshs = math.min(chunkMinshs, s.shA);
                chunkMinshs = math.min(chunkMinshs, s.shB);
                chunkMinshs = math.min(chunkMinshs, s.shC);
                chunkMinshs = math.min(chunkMinshs, s.shD);
                chunkMinshs = math.min(chunkMinshs, s.shE);
                chunkMinshs = math.min(chunkMinshs, s.shF);

                chunkMaxpos = math.max(chunkMaxpos, s.pos);
                chunkMaxscl = math.max(chunkMaxscl, s.scale);
                chunkMaxcol = math.max(chunkMaxcol, new float4(s.dc0, s.opacity));
                chunkMaxshs = math.max(chunkMaxshs, s.sh1);
                chunkMaxshs = math.max(chunkMaxshs, s.sh2);
                chunkMaxshs = math.max(chunkMaxshs, s.sh3);
                chunkMaxshs = math.max(chunkMaxshs, s.sh4);
                chunkMaxshs = math.max(chunkMaxshs, s.sh5);
                chunkMaxshs = math.max(chunkMaxshs, s.sh6);
                chunkMaxshs = math.max(chunkMaxshs, s.sh7);
                chunkMaxshs = math.max(chunkMaxshs, s.sh8);
                chunkMaxshs = math.max(chunkMaxshs, s.sh9);
                chunkMaxshs = math.max(chunkMaxshs, s.shA);
                chunkMaxshs = math.max(chunkMaxshs, s.shB);
                chunkMaxshs = math.max(chunkMaxshs, s.shC);
                chunkMaxshs = math.max(chunkMaxshs, s.shD);
                chunkMaxshs = math.max(chunkMaxshs, s.shE);
                chunkMaxshs = math.max(chunkMaxshs, s.shF);
            }

            // store chunk info
            GaussianSplatAsset.ChunkInfo info = default;
            info.posX = new float2(chunkMinpos.x, chunkMaxpos.x);
            info.posY = new float2(chunkMinpos.y, chunkMaxpos.y);
            info.posZ = new float2(chunkMinpos.z, chunkMaxpos.z);
            info.sclX = math.f32tof16(chunkMinscl.x) | (math.f32tof16(chunkMaxscl.x) << 16);
            info.sclY = math.f32tof16(chunkMinscl.y) | (math.f32tof16(chunkMaxscl.y) << 16);
            info.sclZ = math.f32tof16(chunkMinscl.z) | (math.f32tof16(chunkMaxscl.z) << 16);
            info.colR = math.f32tof16(chunkMincol.x) | (math.f32tof16(chunkMaxcol.x) << 16);
            info.colG = math.f32tof16(chunkMincol.y) | (math.f32tof16(chunkMaxcol.y) << 16);
            info.colB = math.f32tof16(chunkMincol.z) | (math.f32tof16(chunkMaxcol.z) << 16);
            info.colA = math.f32tof16(chunkMincol.w) | (math.f32tof16(chunkMaxcol.w) << 16);
            info.shR = math.f32tof16(chunkMinshs.x) | (math.f32tof16(chunkMaxshs.x) << 16);
            info.shG = math.f32tof16(chunkMinshs.y) | (math.f32tof16(chunkMaxshs.y) << 16);
            info.shB = math.f32tof16(chunkMinshs.z) | (math.f32tof16(chunkMaxshs.z) << 16);
            chunks[chunkIdx] = info;

            // adjust data to be 0..1 within chunk bounds
            for (int i = splatBegin; i < splatEnd; ++i)
            {
                InputSplatData s = splatData[i];
                s.pos = ((float3)s.pos - chunkMinpos) / (chunkMaxpos - chunkMinpos);
                s.scale = ((float3)s.scale - chunkMinscl) / (chunkMaxscl - chunkMinscl);
                s.dc0 = ((float3)s.dc0 - chunkMincol.xyz) / (chunkMaxcol.xyz - chunkMincol.xyz);
                s.opacity = (s.opacity - chunkMincol.w) / (chunkMaxcol.w - chunkMincol.w);
                if (!keepRawSHs)
                {
                    s.sh1 = ((float3)s.sh1 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh2 = ((float3)s.sh2 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh3 = ((float3)s.sh3 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh4 = ((float3)s.sh4 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh5 = ((float3)s.sh5 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh6 = ((float3)s.sh6 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh7 = ((float3)s.sh7 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh8 = ((float3)s.sh8 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.sh9 = ((float3)s.sh9 - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shA = ((float3)s.shA - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shB = ((float3)s.shB - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shC = ((float3)s.shC - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shD = ((float3)s.shD - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shE = ((float3)s.shE - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                    s.shF = ((float3)s.shF - chunkMinshs) / (chunkMaxshs - chunkMinshs);
                }
                splatData[i] = s;
            }
        }
    }

    static void CreateChunkData(NativeArray<InputSplatData> splatData, string filePath, ref Hash128 dataHash, GaussianSplatAsset.SHFormat shFormat)
    {
        int chunkCount = (splatData.Length + GaussianSplatAsset.kChunkSize - 1) / GaussianSplatAsset.kChunkSize;
        CalcChunkDataJob job = new CalcChunkDataJob
        {
            splatData = splatData,
            chunks = new(chunkCount, Allocator.TempJob),
            keepRawSHs = shFormat == GaussianSplatAsset.SHFormat.Float32
        };

        job.Schedule(chunkCount, 8).Complete();

        dataHash.Append(ref job.chunks);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        fs.Write(job.chunks.Reinterpret<byte>(UnsafeUtility.SizeOf<GaussianSplatAsset.ChunkInfo>()));

        job.chunks.Dispose();
    }

    static GraphicsFormat ColorFormatToGraphics(ColorFormat format)
    {
        return format switch
        {
            ColorFormat.Float32x4 => GraphicsFormat.R32G32B32A32_SFloat,
            ColorFormat.Float16x4 => GraphicsFormat.R16G16B16A16_SFloat,
            ColorFormat.Norm8x4 => GraphicsFormat.R8G8B8A8_UNorm,
            ColorFormat.BC7 => GraphicsFormat.RGBA_BC7_UNorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    [BurstCompile]
    struct ConvertColorJob : IJobParallelFor
    {
        public int width, height;
        [ReadOnly] public NativeArray<float4> inputData;
        [NativeDisableParallelForRestriction] public NativeArray<byte> outputData;
        public ColorFormat format;
        public int formatBytesPerPixel;

        public unsafe void Execute(int y)
        {
            int srcIdx = y * width;
            byte* dstPtr = (byte*)outputData.GetUnsafePtr() + y * width * formatBytesPerPixel;
            for (int x = 0; x < width; ++x)
            {
                float4 pix = inputData[srcIdx];

                switch (format)
                {
                    case ColorFormat.Float32x4:
                        {
                            *(float4*)dstPtr = pix;
                        }
                        break;
                    case ColorFormat.Float16x4:
                        {
                            half4 enc = new half4(pix);
                            *(half4*)dstPtr = enc;
                        }
                        break;
                    case ColorFormat.Norm8x4:
                        {
                            pix = math.saturate(pix);
                            uint enc = (uint)(pix.x * 255.5f) | ((uint)(pix.y * 255.5f) << 8) | ((uint)(pix.z * 255.5f) << 16) | ((uint)(pix.w * 255.5f) << 24);
                            *(uint*)dstPtr = enc;
                        }
                        break;
                }

                srcIdx++;
                dstPtr += formatBytesPerPixel;
            }
        }
    }

    static ulong EncodeFloat3ToNorm16(float3 v) // 48 bits: 16.16.16
    {
        return (ulong)(v.x * 65535.5f) | ((ulong)(v.y * 65535.5f) << 16) | ((ulong)(v.z * 65535.5f) << 32);
    }
    static uint EncodeFloat3ToNorm11(float3 v) // 32 bits: 11.10.11
    {
        return (uint)(v.x * 2047.5f) | ((uint)(v.y * 1023.5f) << 11) | ((uint)(v.z * 2047.5f) << 21);
    }
    static ushort EncodeFloat3ToNorm655(float3 v) // 16 bits: 6.5.5
    {
        return (ushort)((uint)(v.x * 63.5f) | ((uint)(v.y * 31.5f) << 6) | ((uint)(v.z * 31.5f) << 11));
    }
    static ushort EncodeFloat3ToNorm565(float3 v) // 16 bits: 5.6.5
    {
        return (ushort)((uint)(v.x * 31.5f) | ((uint)(v.y * 63.5f) << 5) | ((uint)(v.z * 31.5f) << 11));
    }

    static uint EncodeQuatToNorm10(float4 v) // 32 bits: 10.10.10.2
    {
        return (uint)(v.x * 1023.5f) | ((uint)(v.y * 1023.5f) << 10) | ((uint)(v.z * 1023.5f) << 20) | ((uint)(v.w * 3.5f) << 30);
    }

    static unsafe void EmitEncodedVector(float3 v, byte* outputPtr, GaussianSplatAsset.VectorFormat format)
    {
        v = math.saturate(v);
        switch (format)
        {
            case GaussianSplatAsset.VectorFormat.Float32:
                {
                    *(float*)outputPtr = v.x;
                    *(float*)(outputPtr + 4) = v.y;
                    *(float*)(outputPtr + 8) = v.z;
                }
                break;
            case GaussianSplatAsset.VectorFormat.Norm16:
                {
                    ulong enc = EncodeFloat3ToNorm16(v);
                    *(uint*)outputPtr = (uint)enc;
                    *(ushort*)(outputPtr + 4) = (ushort)(enc >> 32);
                }
                break;
            case GaussianSplatAsset.VectorFormat.Norm11:
                {
                    uint enc = EncodeFloat3ToNorm11(v);
                    *(uint*)outputPtr = enc;
                }
                break;
            case GaussianSplatAsset.VectorFormat.Norm6:
                {
                    ushort enc = EncodeFloat3ToNorm655(v);
                    *(ushort*)outputPtr = enc;
                }
                break;
        }
    }

    [BurstCompile]
    struct CreatePositionsDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InputSplatData> m_Input;
        public GaussianSplatAsset.VectorFormat m_Format;
        public int m_FormatSize;
        [NativeDisableParallelForRestriction] public NativeArray<byte> m_Output;

        public unsafe void Execute(int index)
        {
            byte* outputPtr = (byte*)m_Output.GetUnsafePtr() + index * m_FormatSize;
            EmitEncodedVector(m_Input[index].pos, outputPtr, m_Format);
        }
    }

    [BurstCompile]
    struct CreateOtherDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InputSplatData> m_Input;
        [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> m_SplatSHIndices;
        public GaussianSplatAsset.VectorFormat m_ScaleFormat;
        public int m_FormatSize;
        [NativeDisableParallelForRestriction] public NativeArray<byte> m_Output;

        public unsafe void Execute(int index)
        {
            byte* outputPtr = (byte*)m_Output.GetUnsafePtr() + index * m_FormatSize;

            // rotation: 4 bytes
            {
                Quaternion rotQ = m_Input[index].rot;
                float4 rot = new float4(rotQ.x, rotQ.y, rotQ.z, rotQ.w);
                uint enc = EncodeQuatToNorm10(rot);
                *(uint*)outputPtr = enc;
                outputPtr += 4;
            }

            // scale: 6, 4 or 2 bytes
            EmitEncodedVector(m_Input[index].scale, outputPtr, m_ScaleFormat);
            outputPtr += GaussianSplatAsset.GetVectorSize(m_ScaleFormat);

            // SH index
            if (m_SplatSHIndices.IsCreated)
                *(ushort*)outputPtr = (ushort)m_SplatSHIndices[index];
        }
    }

    static int NextMultipleOf(int size, int multipleOf)
    {
        return (size + multipleOf - 1) / multipleOf * multipleOf;
    }

    void CreatePositionsData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash)
    {
        int dataLen = inputSplats.Length * GaussianSplatAsset.GetVectorSize(m_FormatPos);
        dataLen = NextMultipleOf(dataLen, 8); // serialized as ulong
        NativeArray<byte> data = new(dataLen, Allocator.TempJob);

        CreatePositionsDataJob job = new CreatePositionsDataJob
        {
            m_Input = inputSplats,
            m_Format = m_FormatPos,
            m_FormatSize = GaussianSplatAsset.GetVectorSize(m_FormatPos),
            m_Output = data
        };
        job.Schedule(inputSplats.Length, 8192).Complete();

        dataHash.Append(data);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        fs.Write(data);

        data.Dispose();
    }

    void CreateOtherData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash, NativeArray<int> splatSHIndices)
    {
        int formatSize = GaussianSplatAsset.GetOtherSizeNoSHIndex(m_FormatScale);
        if (splatSHIndices.IsCreated)
            formatSize += 2;
        int dataLen = inputSplats.Length * formatSize;

        dataLen = NextMultipleOf(dataLen, 8); // serialized as ulong
        NativeArray<byte> data = new(dataLen, Allocator.TempJob);

        CreateOtherDataJob job = new CreateOtherDataJob
        {
            m_Input = inputSplats,
            m_SplatSHIndices = splatSHIndices,
            m_ScaleFormat = m_FormatScale,
            m_FormatSize = formatSize,
            m_Output = data
        };
        job.Schedule(inputSplats.Length, 8192).Complete();

        dataHash.Append(data);

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        fs.Write(data);

        data.Dispose();
    }

    static int SplatIndexToTextureIndex(uint idx)
    {
        uint2 xy = GaussianUtils.DecodeMorton2D_16x16(idx);
        uint width = GaussianSplatAsset.kTextureWidth / 16;
        idx >>= 8;
        uint x = (idx % width) * 16 + xy.x;
        uint y = (idx / width) * 16 + xy.y;
        return (int)(y * GaussianSplatAsset.kTextureWidth + x);
    }

    [BurstCompile]
    struct CreateColorDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InputSplatData> m_Input;
        [NativeDisableParallelForRestriction] public NativeArray<float4> m_Output;

        public void Execute(int index)
        {
            var splat = m_Input[index];
            int i = SplatIndexToTextureIndex((uint)index);
            m_Output[i] = new float4(splat.dc0.x, splat.dc0.y, splat.dc0.z, splat.opacity);
        }
    }

    void CreateColorData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash, out int width, out int height, out GraphicsFormat format)
    {
        (width, height) = GaussianSplatAsset.CalcTextureSize(inputSplats.Length);
        NativeArray<float4> data = new(width * height, Allocator.TempJob);

        CreateColorDataJob job = new CreateColorDataJob();
        job.m_Input = inputSplats;
        job.m_Output = data;
        job.Schedule(inputSplats.Length, 8192).Complete();

        dataHash.Append(data);
        dataHash.Append((int)m_FormatColor);

        format = ColorFormatToGraphics(m_FormatColor);
        int dstSize = (int)GraphicsFormatUtility.ComputeMipmapSize(width, height, format);

        if (GraphicsFormatUtility.IsCompressedFormat(format))
        {
            Texture2D tex = new Texture2D(width, height, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.DontInitializePixels | TextureCreationFlags.DontUploadUponCreate);
            tex.SetPixelData(data, 0);
            //EditorUtility.CompressTexture(tex, GraphicsFormatUtility.GetTextureFormat(format), 100);
            NativeArray<byte> cmpData = tex.GetPixelData<byte>(0);
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(cmpData);

            DestroyImmediate(tex);
        }
        else
        {
            ConvertColorJob jobConvert = new ConvertColorJob
            {
                width = width,
                height = height,
                inputData = data,
                format = m_FormatColor,
                outputData = new NativeArray<byte>(dstSize, Allocator.TempJob),
                formatBytesPerPixel = dstSize / width / height
            };
            jobConvert.Schedule(height, 1).Complete();
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            fs.Write(jobConvert.outputData);
            jobConvert.outputData.Dispose();
        }

        data.Dispose();
    }

    [BurstCompile]
    struct CreateSHDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<InputSplatData> m_Input;
        public GaussianSplatAsset.SHFormat m_Format;
        public NativeArray<byte> m_Output;
        public unsafe void Execute(int index)
        {
            var splat = m_Input[index];

            switch (m_Format)
            {
                case GaussianSplatAsset.SHFormat.Float32:
                    {
                        GaussianSplatAsset.SHTableItemFloat32 res;
                        res.sh1 = splat.sh1;
                        res.sh2 = splat.sh2;
                        res.sh3 = splat.sh3;
                        res.sh4 = splat.sh4;
                        res.sh5 = splat.sh5;
                        res.sh6 = splat.sh6;
                        res.sh7 = splat.sh7;
                        res.sh8 = splat.sh8;
                        res.sh9 = splat.sh9;
                        res.shA = splat.shA;
                        res.shB = splat.shB;
                        res.shC = splat.shC;
                        res.shD = splat.shD;
                        res.shE = splat.shE;
                        res.shF = splat.shF;
                        res.shPadding = default;
                        ((GaussianSplatAsset.SHTableItemFloat32*)m_Output.GetUnsafePtr())[index] = res;
                    }
                    break;
                case GaussianSplatAsset.SHFormat.Float16:
                    {
                        GaussianSplatAsset.SHTableItemFloat16 res;
                        res.sh1 = new half3(splat.sh1);
                        res.sh2 = new half3(splat.sh2);
                        res.sh3 = new half3(splat.sh3);
                        res.sh4 = new half3(splat.sh4);
                        res.sh5 = new half3(splat.sh5);
                        res.sh6 = new half3(splat.sh6);
                        res.sh7 = new half3(splat.sh7);
                        res.sh8 = new half3(splat.sh8);
                        res.sh9 = new half3(splat.sh9);
                        res.shA = new half3(splat.shA);
                        res.shB = new half3(splat.shB);
                        res.shC = new half3(splat.shC);
                        res.shD = new half3(splat.shD);
                        res.shE = new half3(splat.shE);
                        res.shF = new half3(splat.shF);
                        res.shPadding = default;
                        ((GaussianSplatAsset.SHTableItemFloat16*)m_Output.GetUnsafePtr())[index] = res;
                    }
                    break;
                case GaussianSplatAsset.SHFormat.Norm11:
                    {
                        GaussianSplatAsset.SHTableItemNorm11 res;
                        res.sh1 = EncodeFloat3ToNorm11(splat.sh1);
                        res.sh2 = EncodeFloat3ToNorm11(splat.sh2);
                        res.sh3 = EncodeFloat3ToNorm11(splat.sh3);
                        res.sh4 = EncodeFloat3ToNorm11(splat.sh4);
                        res.sh5 = EncodeFloat3ToNorm11(splat.sh5);
                        res.sh6 = EncodeFloat3ToNorm11(splat.sh6);
                        res.sh7 = EncodeFloat3ToNorm11(splat.sh7);
                        res.sh8 = EncodeFloat3ToNorm11(splat.sh8);
                        res.sh9 = EncodeFloat3ToNorm11(splat.sh9);
                        res.shA = EncodeFloat3ToNorm11(splat.shA);
                        res.shB = EncodeFloat3ToNorm11(splat.shB);
                        res.shC = EncodeFloat3ToNorm11(splat.shC);
                        res.shD = EncodeFloat3ToNorm11(splat.shD);
                        res.shE = EncodeFloat3ToNorm11(splat.shE);
                        res.shF = EncodeFloat3ToNorm11(splat.shF);
                        ((GaussianSplatAsset.SHTableItemNorm11*)m_Output.GetUnsafePtr())[index] = res;
                    }
                    break;
                case GaussianSplatAsset.SHFormat.Norm6:
                    {
                        GaussianSplatAsset.SHTableItemNorm6 res;
                        res.sh1 = EncodeFloat3ToNorm565(splat.sh1);
                        res.sh2 = EncodeFloat3ToNorm565(splat.sh2);
                        res.sh3 = EncodeFloat3ToNorm565(splat.sh3);
                        res.sh4 = EncodeFloat3ToNorm565(splat.sh4);
                        res.sh5 = EncodeFloat3ToNorm565(splat.sh5);
                        res.sh6 = EncodeFloat3ToNorm565(splat.sh6);
                        res.sh7 = EncodeFloat3ToNorm565(splat.sh7);
                        res.sh8 = EncodeFloat3ToNorm565(splat.sh8);
                        res.sh9 = EncodeFloat3ToNorm565(splat.sh9);
                        res.shA = EncodeFloat3ToNorm565(splat.shA);
                        res.shB = EncodeFloat3ToNorm565(splat.shB);
                        res.shC = EncodeFloat3ToNorm565(splat.shC);
                        res.shD = EncodeFloat3ToNorm565(splat.shD);
                        res.shE = EncodeFloat3ToNorm565(splat.shE);
                        res.shF = EncodeFloat3ToNorm565(splat.shF);
                        res.shPadding = default;
                        ((GaussianSplatAsset.SHTableItemNorm6*)m_Output.GetUnsafePtr())[index] = res;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    static void EmitSimpleDataFile<T>(NativeArray<T> data, string filePath, ref Hash128 dataHash) where T : unmanaged
    {
        dataHash.Append(data);
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        fs.Write(data.Reinterpret<byte>(UnsafeUtility.SizeOf<T>()));
    }

    void CreateSHData(NativeArray<InputSplatData> inputSplats, string filePath, ref Hash128 dataHash, NativeArray<GaussianSplatAsset.SHTableItemFloat16> clusteredSHs)
    {
        if (clusteredSHs.IsCreated)
        {
            EmitSimpleDataFile(clusteredSHs, filePath, ref dataHash);
        }
        else
        {
            int dataLen = (int)GaussianSplatAsset.CalcSHDataSize(inputSplats.Length, m_FormatSH);
            NativeArray<byte> data = new(dataLen, Allocator.TempJob);
            CreateSHDataJob job = new CreateSHDataJob
            {
                m_Input = inputSplats,
                m_Format = m_FormatSH,
                m_Output = data
            };
            job.Schedule(inputSplats.Length, 8192).Complete();
            EmitSimpleDataFile(data, filePath, ref dataHash);
            data.Dispose();
        }
    }

    static GaussianSplatAsset.CameraInfo[] LoadJsonCamerasFile(string curPath, bool doImport)
    {
        if (!doImport)
            return null;

        string camerasPath;
        while (true)
        {
            var dir = Path.GetDirectoryName(curPath);
            if (!Directory.Exists(dir))
                return null;
            camerasPath = $"{dir}/{kCamerasJson}";
            if (File.Exists(camerasPath))
                break;
            curPath = dir;
        }

        if (!File.Exists(camerasPath))
            return null;

        string json = File.ReadAllText(camerasPath);
        var jsonCameras = JSONParser.FromJson<List<JsonCamera>>(json);
        if (jsonCameras == null || jsonCameras.Count == 0)
            return null;

        var result = new GaussianSplatAsset.CameraInfo[jsonCameras.Count];
        for (var camIndex = 0; camIndex < jsonCameras.Count; camIndex++)
        {
            var jsonCam = jsonCameras[camIndex];
            var pos = new Vector3(jsonCam.position[0], jsonCam.position[1], jsonCam.position[2]);
            // the matrix is a "view matrix", not "camera matrix" lol
            var axisx = new Vector3(jsonCam.rotation[0][0], jsonCam.rotation[1][0], jsonCam.rotation[2][0]);
            var axisy = new Vector3(jsonCam.rotation[0][1], jsonCam.rotation[1][1], jsonCam.rotation[2][1]);
            var axisz = new Vector3(jsonCam.rotation[0][2], jsonCam.rotation[1][2], jsonCam.rotation[2][2]);

            axisy *= -1;
            axisz *= -1;

            var cam = new GaussianSplatAsset.CameraInfo
            {
                pos = pos,
                axisX = axisx,
                axisY = axisy,
                axisZ = axisz,
                fov = 25 //@TODO
            };
            result[camIndex] = cam;
        }

        return result;
    }

    [Serializable]
    public class JsonCamera
    {
        public int id;
        public string img_name;
        public int width;
        public int height;
        public float[] position;
        public float[][] rotation;
        public float fx;
        public float fy;
    }


    public static class PLYFileReader
    {
        public static void ReadFileHeader(string filePath, out int vertexCount, out int vertexStride, out List<string> attrNames)
        {
            vertexCount = 0;
            vertexStride = 0;
            attrNames = new List<string>();
            if (!File.Exists(filePath))
                return;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            ReadHeaderImpl(filePath, out vertexCount, out vertexStride, out attrNames, fs);
        }

        static void ReadHeaderImpl(string filePath, out int vertexCount, out int vertexStride, out List<string> attrNames, FileStream fs)
        {
            // C# arrays and NativeArrays make it hard to have a "byte" array larger than 2GB :/
            if (fs.Length >= 2 * 1024 * 1024 * 1024L)
                throw new IOException($"PLY {filePath} read error: currently files larger than 2GB are not supported");

            // read header
            vertexCount = 0;
            vertexStride = 0;
            attrNames = new List<string>();
            const int kMaxHeaderLines = 9000;
            for (int lineIdx = 0; lineIdx < kMaxHeaderLines; ++lineIdx)
            {
                var line = ReadLine(fs);
                if (line == "end_header" || line.Length == 0)
                    break;
                var tokens = line.Split(' ');
                if (tokens.Length == 3 && tokens[0] == "element" && tokens[1] == "vertex")
                    vertexCount = int.Parse(tokens[2]);
                if (tokens.Length == 3 && tokens[0] == "property")
                {
                    ElementType type = tokens[1] switch
                    {
                        "float" => ElementType.Float,
                        "double" => ElementType.Double,
                        "uchar" => ElementType.UChar,
                        _ => ElementType.None
                    };
                    vertexStride += TypeToSize(type);
                    attrNames.Add(tokens[2]);
                }
            }
            //Debug.Log($"PLY {filePath} vtx {vertexCount} stride {vertexStride} attrs #{attrNames.Count} {string.Join(',', attrNames)}");
        }

        public static void ReadFile(string filePath, out int vertexCount, out int vertexStride, out List<string> attrNames, out NativeArray<byte> vertices)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            ReadHeaderImpl(filePath, out vertexCount, out vertexStride, out attrNames, fs);

            vertices = new NativeArray<byte>(vertexCount * vertexStride, Allocator.Persistent);
            var readBytes = fs.Read(vertices);
            if (readBytes != vertices.Length)
                throw new IOException($"PLY {filePath} read error, expected {vertices.Length} data bytes got {readBytes}");
        }

        enum ElementType
        {
            None,
            Float,
            Double,
            UChar
        }

        static int TypeToSize(ElementType t)
        {
            return t switch
            {
                ElementType.None => 0,
                ElementType.Float => 4,
                ElementType.Double => 8,
                ElementType.UChar => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(t), t, null)
            };
        }

        static string ReadLine(FileStream fs)
        {
            var byteBuffer = new List<byte>();
            while (true)
            {
                int b = fs.ReadByte();
                if (b == -1 || b == '\n')
                    break;
                byteBuffer.Add((byte)b);
            }
            // if line had CRLF line endings, remove the CR part
            if (byteBuffer.Count > 0 && byteBuffer.Last() == '\r')
                byteBuffer.RemoveAt(byteBuffer.Count - 1);
            return Encoding.UTF8.GetString(byteBuffer.ToArray());
        }
    }

    [BurstCompile]
    public struct KMeansClustering
    {
        static ProfilerMarker s_ProfCalculate = new(ProfilerCategory.Render, "KMeans.Calculate", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfPlusPlus = new(ProfilerCategory.Render, "KMeans.InitialPlusPlus", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfInitialDistanceSum = new(ProfilerCategory.Render, "KMeans.Initialize.DistanceSum", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfInitialPickPoint = new(ProfilerCategory.Render, "KMeans.Initialize.PickPoint", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfInitialDistanceUpdate = new(ProfilerCategory.Render, "KMeans.Initialize.DistanceUpdate", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfAssignClusters = new(ProfilerCategory.Render, "KMeans.AssignClusters", MarkerFlags.SampleGPU);
        static ProfilerMarker s_ProfUpdateMeans = new(ProfilerCategory.Render, "KMeans.UpdateMeans", MarkerFlags.SampleGPU);

        public static bool Calculate(int dim, NativeArray<float> inputData, int batchSize, float passesOverData, Func<float, bool> progress, NativeArray<float> outClusterMeans, NativeArray<int> outDataLabels)
        {
            // Parameter checks
            if (dim < 1)
                throw new InvalidOperationException($"KMeans: dimensionality has to be >= 1, was {dim}");
            if (batchSize < 1)
                throw new InvalidOperationException($"KMeans: batch size has to be >= 1, was {batchSize}");
            if (passesOverData < 0.0001f)
                throw new InvalidOperationException($"KMeans: passes over data must be positive, was {passesOverData}");
            if (inputData.Length % dim != 0)
                throw new InvalidOperationException($"KMeans: input length must be multiple of dim={dim}, was {inputData.Length}");
            if (outClusterMeans.Length % dim != 0)
                throw new InvalidOperationException($"KMeans: output means length must be multiple of dim={dim}, was {outClusterMeans.Length}");
            int dataSize = inputData.Length / dim;
            int k = outClusterMeans.Length / dim;
            if (k < 1)
                throw new InvalidOperationException($"KMeans: cluster count length must be at least 1, was {k}");
            if (dataSize < k)
                throw new InvalidOperationException($"KMeans: input length ({inputData.Length}) must at least as long as clusters ({outClusterMeans.Length})");
            if (dataSize != outDataLabels.Length)
                throw new InvalidOperationException($"KMeans: output labels length must be {dataSize}, was {outDataLabels.Length}");

            using var prof = s_ProfCalculate.Auto();
            batchSize = math.min(dataSize, batchSize);
            uint rngState = 1;

            // Do initial cluster placement
            int initBatchSize = 10 * k;
            const int kInitAttempts = 3;
            if (!InitializeCentroids(dim, inputData, initBatchSize, ref rngState, kInitAttempts, outClusterMeans, progress))
                return false;

            NativeArray<float> counts = new(k, Allocator.TempJob);

            NativeArray<float> batchPoints = new(batchSize * dim, Allocator.TempJob);
            NativeArray<int> batchClusters = new(batchSize, Allocator.TempJob);

            bool cancelled = false;
            for (float calcDone = 0.0f, calcLimit = dataSize * passesOverData; calcDone < calcLimit; calcDone += batchSize)
            {
                if (progress != null && !progress(0.3f + calcDone / calcLimit * 0.4f))
                {
                    cancelled = true;
                    break;
                }

                // generate a batch of random input points
                MakeRandomBatch(dim, inputData, ref rngState, batchPoints);

                // find which of the current centroids each batch point is closest to
                {
                    using var profPart = s_ProfAssignClusters.Auto();
                    AssignClustersJob job = new AssignClustersJob
                    {
                        dim = dim,
                        data = batchPoints,
                        means = outClusterMeans,
                        indexOffset = 0,
                        clusters = batchClusters,
                    };
                    job.Schedule(batchSize, 1).Complete();
                }

                // update the centroids
                {
                    using var profPart = s_ProfUpdateMeans.Auto();
                    UpdateCentroidsJob job = new UpdateCentroidsJob
                    {
                        m_Clusters = outClusterMeans,
                        m_Dim = dim,
                        m_Counts = counts,
                        m_BatchSize = batchSize,
                        m_BatchClusters = batchClusters,
                        m_BatchPoints = batchPoints
                    };
                    job.Schedule().Complete();
                }
            }

            // finally find out closest clusters for all input points
            {
                using var profPart = s_ProfAssignClusters.Auto();
                const int kAssignBatchCount = 256 * 1024;
                AssignClustersJob job = new AssignClustersJob
                {
                    dim = dim,
                    data = inputData,
                    means = outClusterMeans,
                    indexOffset = 0,
                    clusters = outDataLabels,
                };
                for (int i = 0; i < dataSize; i += kAssignBatchCount)
                {
                    if (progress != null && !progress(0.7f + (float)i / dataSize * 0.3f))
                    {
                        cancelled = true;
                        break;
                    }
                    job.indexOffset = i;
                    job.Schedule(math.min(kAssignBatchCount, dataSize - i), 512).Complete();
                }
            }

            counts.Dispose();
            batchPoints.Dispose();
            batchClusters.Dispose();
            return !cancelled;
        }

        static unsafe float DistanceSquared(int dim, NativeArray<float> a, int aIndex, NativeArray<float> b, int bIndex)
        {
            aIndex *= dim;
            bIndex *= dim;
            float d = 0;
            if (X86.Avx.IsAvxSupported)
            {
                // 8x wide with AVX
                int i = 0;
                float* aptr = (float*)a.GetUnsafeReadOnlyPtr() + aIndex;
                float* bptr = (float*)b.GetUnsafeReadOnlyPtr() + bIndex;
                for (; i + 7 < dim; i += 8)
                {
                    v256 va = X86.Avx.mm256_loadu_ps(aptr);
                    v256 vb = X86.Avx.mm256_loadu_ps(bptr);
                    v256 vd = X86.Avx.mm256_sub_ps(va, vb);
                    vd = X86.Avx.mm256_mul_ps(vd, vd);

                    vd = X86.Avx.mm256_hadd_ps(vd, vd);
                    d += vd.Float0 + vd.Float1 + vd.Float4 + vd.Float5;

                    aptr += 8;
                    bptr += 8;
                }
                // remainder
                for (; i < dim; ++i)
                {
                    float delta = *aptr - *bptr;
                    d += delta * delta;
                    aptr++;
                    bptr++;
                }
            }
            else if (Arm.Neon.IsNeonSupported)
            {
                // 4x wide with NEON
                int i = 0;
                float* aptr = (float*)a.GetUnsafeReadOnlyPtr() + aIndex;
                float* bptr = (float*)b.GetUnsafeReadOnlyPtr() + bIndex;
                for (; i + 3 < dim; i += 4)
                {
                    v128 va = Arm.Neon.vld1q_f32(aptr);
                    v128 vb = Arm.Neon.vld1q_f32(bptr);
                    v128 vd = Arm.Neon.vsubq_f32(va, vb);
                    vd = Arm.Neon.vmulq_f32(vd, vd);

                    d += Arm.Neon.vaddvq_f32(vd);

                    aptr += 4;
                    bptr += 4;
                }
                // remainder
                for (; i < dim; ++i)
                {
                    float delta = *aptr - *bptr;
                    d += delta * delta;
                    aptr++;
                    bptr++;
                }

            }
            else
            {
                for (var i = 0; i < dim; ++i)
                {
                    float delta = a[aIndex + i] - b[bIndex + i];
                    d += delta * delta;
                }
            }

            return d;
        }

        static unsafe void CopyElem(int dim, NativeArray<float> src, int srcIndex, NativeArray<float> dst, int dstIndex)
        {
            UnsafeUtility.MemCpy((float*)dst.GetUnsafePtr() + dstIndex * dim,
                (float*)src.GetUnsafeReadOnlyPtr() + srcIndex * dim, dim * 4);
        }

        [BurstCompile]
        struct ClosestDistanceInitialJob : IJobParallelFor
        {
            public int dim;
            [ReadOnly] public NativeArray<float> data;
            [ReadOnly] public NativeArray<float> means;
            public NativeArray<float> minDistSq;
            public int pointIndex;
            public void Execute(int index)
            {
                if (index == pointIndex)
                    return;
                minDistSq[index] = DistanceSquared(dim, data, index, means, 0);
            }
        }

        [BurstCompile]
        struct ClosestDistanceUpdateJob : IJobParallelFor
        {
            public int dim;
            [ReadOnly] public NativeArray<float> data;
            [ReadOnly] public NativeArray<float> means;
            [ReadOnly] public NativeBitArray taken;
            public NativeArray<float> minDistSq;
            public int meanIndex;
            public void Execute(int index)
            {
                if (taken.IsSet(index))
                    return;
                float distSq = DistanceSquared(dim, data, index, means, meanIndex);
                minDistSq[index] = math.min(minDistSq[index], distSq);
            }
        }

        [BurstCompile]
        struct CalcDistSqJob : IJobParallelFor
        {
            public const int kBatchSize = 1024;
            public int dataSize;
            [ReadOnly] public NativeBitArray taken;
            [ReadOnly] public NativeArray<float> minDistSq;
            public NativeArray<float> partialSums;

            public void Execute(int batchIndex)
            {
                int iStart = math.min(batchIndex * kBatchSize, dataSize);
                int iEnd = math.min((batchIndex + 1) * kBatchSize, dataSize);
                float sum = 0;
                for (int i = iStart; i < iEnd; ++i)
                {
                    if (taken.IsSet(i))
                        continue;
                    sum += minDistSq[i];
                }

                partialSums[batchIndex] = sum;
            }
        }

        [BurstCompile]
        static int PickPointIndex(int dataSize, ref NativeArray<float> partialSums, ref NativeBitArray taken, ref NativeArray<float> minDistSq, float rval)
        {
            // Skip batches until we hit the ones that might have value to pick from: binary search for the batch
            int indexL = 0;
            int indexR = partialSums.Length;
            while (indexL < indexR)
            {
                int indexM = (indexL + indexR) / 2;
                if (partialSums[indexM] < rval)
                    indexL = indexM + 1;
                else
                    indexR = indexM;
            }
            float acc = 0.0f;
            if (indexL > 0)
            {
                acc = partialSums[indexL - 1];
            }

            // Now search for the needed point
            int pointIndex = -1;
            for (int i = indexL * CalcDistSqJob.kBatchSize; i < dataSize; ++i)
            {
                if (taken.IsSet(i))
                    continue;
                acc += minDistSq[i];
                if (acc >= rval)
                {
                    pointIndex = i;
                    break;
                }
            }

            // If we have not found a point, pick the last available one
            if (pointIndex < 0)
            {
                for (int i = dataSize - 1; i >= 0; --i)
                {
                    if (taken.IsSet(i))
                        continue;
                    pointIndex = i;
                    break;
                }
            }

            if (pointIndex < 0)
                pointIndex = 0;

            return pointIndex;
        }

        static void KMeansPlusPlus(int dim, int k, NativeArray<float> data, NativeArray<float> means, NativeArray<float> minDistSq, ref uint rngState)
        {
            using var prof = s_ProfPlusPlus.Auto();

            int dataSize = data.Length / dim;

            NativeBitArray taken = new NativeBitArray(dataSize, Allocator.TempJob);

            // Select first mean randomly
            int pointIndex = (int)(pcg_random(ref rngState) % dataSize);
            taken.Set(pointIndex, true);
            CopyElem(dim, data, pointIndex, means, 0);

            // For each point: closest squared distance to the picked point
            {
                ClosestDistanceInitialJob job = new ClosestDistanceInitialJob
                {
                    dim = dim,
                    data = data,
                    means = means,
                    minDistSq = minDistSq,
                    pointIndex = pointIndex
                };
                job.Schedule(dataSize, 1024).Complete();
            }

            int sumBatches = (dataSize + CalcDistSqJob.kBatchSize - 1) / CalcDistSqJob.kBatchSize;
            NativeArray<float> partialSums = new(sumBatches, Allocator.TempJob);
            int resultCount = 1;
            while (resultCount < k)
            {
                // Find total sum of distances of not yet taken points
                float distSqTotal = 0;
                {
                    using var profPart = s_ProfInitialDistanceSum.Auto();
                    CalcDistSqJob job = new CalcDistSqJob
                    {
                        dataSize = dataSize,
                        taken = taken,
                        minDistSq = minDistSq,
                        partialSums = partialSums
                    };
                    job.Schedule(sumBatches, 1).Complete();
                    for (int i = 0; i < sumBatches; ++i)
                    {
                        distSqTotal += partialSums[i];
                        partialSums[i] = distSqTotal;
                    }
                }

                // Pick a non-taken point, with a probability proportional
                // to distance: points furthest from any cluster are picked more.
                {
                    using var profPart = s_ProfInitialPickPoint.Auto();
                    float rval = pcg_hash_float(rngState + (uint)resultCount, distSqTotal);
                    pointIndex = PickPointIndex(dataSize, ref partialSums, ref taken, ref minDistSq, rval);
                }

                // Take this point as a new cluster mean
                taken.Set(pointIndex, true);
                CopyElem(dim, data, pointIndex, means, resultCount);
                ++resultCount;

                if (resultCount < k)
                {
                    // Update distances of the points: since it tracks closest one,
                    // calculate distance to the new cluster and update if smaller.
                    using var profPart = s_ProfInitialDistanceUpdate.Auto();
                    ClosestDistanceUpdateJob job = new ClosestDistanceUpdateJob
                    {
                        dim = dim,
                        data = data,
                        means = means,
                        minDistSq = minDistSq,
                        taken = taken,
                        meanIndex = resultCount - 1
                    };
                    job.Schedule(dataSize, 256).Complete();
                }
            }

            taken.Dispose();
            partialSums.Dispose();
        }

        // For each data point, find cluster index that is closest to it
        [BurstCompile]
        struct AssignClustersJob : IJobParallelFor
        {
            public int indexOffset;
            public int dim;
            [ReadOnly] public NativeArray<float> data;
            [ReadOnly] public NativeArray<float> means;
            [NativeDisableParallelForRestriction] public NativeArray<int> clusters;
            [NativeDisableContainerSafetyRestriction][NativeDisableParallelForRestriction] public NativeArray<float> distances;

            public void Execute(int index)
            {
                index += indexOffset;
                int meansCount = means.Length / dim;
                float minDist = float.MaxValue;
                int minIndex = 0;
                for (int i = 0; i < meansCount; ++i)
                {
                    float dist = DistanceSquared(dim, data, index, means, i);
                    if (dist < minDist)
                    {
                        minIndex = i;
                        minDist = dist;
                    }
                }
                clusters[index] = minIndex;
                if (distances.IsCreated)
                    distances[index] = minDist;
            }
        }

        static void MakeRandomBatch(int dim, NativeArray<float> inputData, ref uint rngState, NativeArray<float> outBatch)
        {
            var job = new MakeBatchJob
            {
                m_Dim = dim,
                m_InputData = inputData,
                m_Seed = pcg_random(ref rngState),
                m_OutBatch = outBatch
            };
            job.Schedule().Complete();
        }

        [BurstCompile]
        struct MakeBatchJob : IJob
        {
            public int m_Dim;
            public NativeArray<float> m_InputData;
            public NativeArray<float> m_OutBatch;
            public uint m_Seed;
            public void Execute()
            {
                uint dataSize = (uint)(m_InputData.Length / m_Dim);
                int batchSize = m_OutBatch.Length / m_Dim;
                NativeHashSet<int> picked = new(batchSize, Allocator.Temp);
                while (picked.Count < batchSize)
                {
                    int index = (int)(pcg_hash(m_Seed++) % dataSize);
                    if (!picked.Contains(index))
                    {
                        CopyElem(m_Dim, m_InputData, index, m_OutBatch, picked.Count);
                        picked.Add(index);
                    }
                }
                picked.Dispose();
            }
        }

        [BurstCompile]
        struct UpdateCentroidsJob : IJob
        {
            public int m_Dim;
            public int m_BatchSize;
            [ReadOnly] public NativeArray<int> m_BatchClusters;
            public NativeArray<float> m_Counts;
            [ReadOnly] public NativeArray<float> m_BatchPoints;
            public NativeArray<float> m_Clusters;

            public void Execute()
            {
                for (int i = 0; i < m_BatchSize; ++i)
                {
                    int clusterIndex = m_BatchClusters[i];
                    m_Counts[clusterIndex]++;
                    float alpha = 1.0f / m_Counts[clusterIndex];

                    for (int j = 0; j < m_Dim; ++j)
                    {
                        m_Clusters[clusterIndex * m_Dim + j] = math.lerp(m_Clusters[clusterIndex * m_Dim + j],
                            m_BatchPoints[i * m_Dim + j], alpha);
                    }
                }
            }
        }

        static bool InitializeCentroids(int dim, NativeArray<float> inputData, int initBatchSize, ref uint rngState, int initAttempts, NativeArray<float> outClusters, Func<float, bool> progress)
        {
            using var prof = s_ProfPlusPlus.Auto();

            int k = outClusters.Length / dim;
            int dataSize = inputData.Length / dim;
            initBatchSize = math.min(initBatchSize, dataSize);

            NativeArray<float> centroidBatch = new(initBatchSize * dim, Allocator.TempJob);
            NativeArray<float> validationBatch = new(initBatchSize * dim, Allocator.TempJob);
            MakeRandomBatch(dim, inputData, ref rngState, centroidBatch);
            MakeRandomBatch(dim, inputData, ref rngState, validationBatch);

            NativeArray<int> tmpIndices = new(initBatchSize, Allocator.TempJob);
            NativeArray<float> tmpDistances = new(initBatchSize, Allocator.TempJob);
            NativeArray<float> curCentroids = new(k * dim, Allocator.TempJob);

            float minDistSum = float.MaxValue;

            bool cancelled = false;
            for (int ia = 0; ia < initAttempts; ++ia)
            {
                if (progress != null && !progress((float)ia / initAttempts * 0.3f))
                {
                    cancelled = true;
                    break;
                }

                KMeansPlusPlus(dim, k, centroidBatch, curCentroids, tmpDistances, ref rngState);

                {
                    using var profPart = s_ProfAssignClusters.Auto();
                    AssignClustersJob job = new AssignClustersJob
                    {
                        dim = dim,
                        data = validationBatch,
                        means = curCentroids,
                        indexOffset = 0,
                        clusters = tmpIndices,
                        distances = tmpDistances
                    };
                    job.Schedule(initBatchSize, 1).Complete();
                }

                float distSum = 0;
                foreach (var d in tmpDistances)
                    distSum += d;

                // is this centroid better?
                if (distSum < minDistSum)
                {
                    minDistSum = distSum;
                    outClusters.CopyFrom(curCentroids);
                }
            }

            centroidBatch.Dispose();
            validationBatch.Dispose();
            tmpDistances.Dispose();
            tmpIndices.Dispose();
            curCentroids.Dispose();
            return !cancelled;
        }

        // https://www.reedbeta.com/blog/hash-functions-for-gpu-rendering/
        static uint pcg_hash(uint input)
        {
            uint state = input * 747796405u + 2891336453u;
            uint word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
            return (word >> 22) ^ word;
        }

        static float pcg_hash_float(uint input, float upTo)
        {
            uint val = pcg_hash(input);
            float f = math.asfloat(0x3f800000 | (val >> 9)) - 1.0f;
            return f * upTo;
        }

        static uint pcg_random(ref uint rng_state)
        {
            uint state = rng_state;
            rng_state = rng_state * 747796405u + 2891336453u;
            uint word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
            return (word >> 22) ^ word;
        }
    }



}

public static class JSONParser
{
    [ThreadStatic] static Stack<List<string>> splitArrayPool;
    [ThreadStatic] static StringBuilder stringBuilder;
    [ThreadStatic] static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache;
    [ThreadStatic] static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache;

    public static T FromJson<T>(this string json)
    {
        // Initialize, if needed, the ThreadStatic variables
        if (propertyInfoCache == null) propertyInfoCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        if (fieldInfoCache == null) fieldInfoCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        if (stringBuilder == null) stringBuilder = new StringBuilder();
        if (splitArrayPool == null) splitArrayPool = new Stack<List<string>>();

        //Remove all whitespace not within strings to make parsing simpler
        stringBuilder.Length = 0;
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"')
            {
                i = AppendUntilStringEnd(true, i, json);
                continue;
            }
            if (char.IsWhiteSpace(c))
                continue;

            stringBuilder.Append(c);
        }

        //Parse the thing!
        return (T)ParseValue(typeof(T), stringBuilder.ToString());
    }

    static int AppendUntilStringEnd(bool appendEscapeCharacter, int startIdx, string json)
    {
        stringBuilder.Append(json[startIdx]);
        for (int i = startIdx + 1; i < json.Length; i++)
        {
            if (json[i] == '\\')
            {
                if (appendEscapeCharacter)
                    stringBuilder.Append(json[i]);
                stringBuilder.Append(json[i + 1]);
                i++;//Skip next character as it is escaped
            }
            else if (json[i] == '"')
            {
                stringBuilder.Append(json[i]);
                return i;
            }
            else
                stringBuilder.Append(json[i]);
        }
        return json.Length - 1;
    }

    //Splits { <value>:<value>, <value>:<value> } and [ <value>, <value> ] into a list of <value> strings
    static List<string> Split(string json)
    {
        List<string> splitArray = splitArrayPool.Count > 0 ? splitArrayPool.Pop() : new List<string>();
        splitArray.Clear();
        if (json.Length == 2)
            return splitArray;
        int parseDepth = 0;
        stringBuilder.Length = 0;
        for (int i = 1; i < json.Length - 1; i++)
        {
            switch (json[i])
            {
                case '[':
                case '{':
                    parseDepth++;
                    break;
                case ']':
                case '}':
                    parseDepth--;
                    break;
                case '"':
                    i = AppendUntilStringEnd(true, i, json);
                    continue;
                case ',':
                case ':':
                    if (parseDepth == 0)
                    {
                        splitArray.Add(stringBuilder.ToString());
                        stringBuilder.Length = 0;
                        continue;
                    }
                    break;
            }

            stringBuilder.Append(json[i]);
        }

        splitArray.Add(stringBuilder.ToString());

        return splitArray;
    }

    internal static object ParseValue(Type type, string json)
    {
        if (type == typeof(string))
        {
            if (json.Length <= 2)
                return string.Empty;
            StringBuilder parseStringBuilder = new StringBuilder(json.Length);
            for (int i = 1; i < json.Length - 1; ++i)
            {
                if (json[i] == '\\' && i + 1 < json.Length - 1)
                {
                    int j = "\"\\nrtbf/".IndexOf(json[i + 1]);
                    if (j >= 0)
                    {
                        parseStringBuilder.Append("\"\\\n\r\t\b\f/"[j]);
                        ++i;
                        continue;
                    }
                    if (json[i + 1] == 'u' && i + 5 < json.Length - 1)
                    {
                        UInt32 c = 0;
                        if (UInt32.TryParse(json.Substring(i + 2, 4), System.Globalization.NumberStyles.AllowHexSpecifier, null, out c))
                        {
                            parseStringBuilder.Append((char)c);
                            i += 5;
                            continue;
                        }
                    }
                }
                parseStringBuilder.Append(json[i]);
            }
            return parseStringBuilder.ToString();
        }
        if (type.IsPrimitive)
        {
            var result = Convert.ChangeType(json, type, System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
        if (type == typeof(decimal))
        {
            decimal result;
            decimal.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
            return result;
        }
        if (type == typeof(DateTime))
        {
            DateTime result;
            DateTime.TryParse(json.Replace("\"", ""), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result);
            return result;
        }
        if (json == "null")
        {
            return null;
        }
        if (type.IsEnum)
        {
            if (json[0] == '"')
                json = json.Substring(1, json.Length - 2);
            try
            {
                return Enum.Parse(type, json, false);
            }
            catch
            {
                return 0;
            }
        }
        if (type.IsArray)
        {
            Type arrayType = type.GetElementType();
            if (json[0] != '[' || json[json.Length - 1] != ']')
                return null;

            List<string> elems = Split(json);
            Array newArray = Array.CreateInstance(arrayType, elems.Count);
            for (int i = 0; i < elems.Count; i++)
                newArray.SetValue(ParseValue(arrayType, elems[i]), i);
            splitArrayPool.Push(elems);
            return newArray;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type listType = type.GetGenericArguments()[0];
            if (json[0] != '[' || json[json.Length - 1] != ']')
                return null;

            List<string> elems = Split(json);
            var list = (IList)type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elems.Count });
            for (int i = 0; i < elems.Count; i++)
                list.Add(ParseValue(listType, elems[i]));
            splitArrayPool.Push(elems);
            return list;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType, valueType;
            {
                Type[] args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
            }

            //Refuse to parse dictionary keys that aren't of type string
            if (keyType != typeof(string))
                return null;
            //Must be a valid dictionary element
            if (json[0] != '{' || json[json.Length - 1] != '}')
                return null;
            //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
            List<string> elems = Split(json);
            if (elems.Count % 2 != 0)
                return null;

            var dictionary = (IDictionary)type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elems.Count / 2 });
            for (int i = 0; i < elems.Count; i += 2)
            {
                if (elems[i].Length <= 2)
                    continue;
                string keyValue = elems[i].Substring(1, elems[i].Length - 2);
                object val = ParseValue(valueType, elems[i + 1]);
                dictionary[keyValue] = val;
            }
            return dictionary;
        }
        if (type == typeof(object))
        {
            return ParseAnonymousValue(json);
        }
        if (json[0] == '{' && json[json.Length - 1] == '}')
        {
            return ParseObject(type, json);
        }

        return null;
    }

    static object ParseAnonymousValue(string json)
    {
        if (json.Length == 0)
            return null;
        if (json[0] == '{' && json[json.Length - 1] == '}')
        {
            List<string> elems = Split(json);
            if (elems.Count % 2 != 0)
                return null;
            var dict = new Dictionary<string, object>(elems.Count / 2);
            for (int i = 0; i < elems.Count; i += 2)
                dict[elems[i].Substring(1, elems[i].Length - 2)] = ParseAnonymousValue(elems[i + 1]);
            return dict;
        }
        if (json[0] == '[' && json[json.Length - 1] == ']')
        {
            List<string> items = Split(json);
            var finalList = new List<object>(items.Count);
            for (int i = 0; i < items.Count; i++)
                finalList.Add(ParseAnonymousValue(items[i]));
            return finalList;
        }
        if (json[0] == '"' && json[json.Length - 1] == '"')
        {
            string str = json.Substring(1, json.Length - 2);
            return str.Replace("\\", string.Empty);
        }
        if (char.IsDigit(json[0]) || json[0] == '-')
        {
            if (json.Contains("."))
            {
                double result;
                double.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result);
                return result;
            }
            else
            {
                int result;
                int.TryParse(json, out result);
                return result;
            }
        }
        if (json == "true")
            return true;
        if (json == "false")
            return false;
        // handles json == "null" as well as invalid JSON
        return null;
    }

    static Dictionary<string, T> CreateMemberNameDictionary<T>(T[] members) where T : MemberInfo
    {
        Dictionary<string, T> nameToMember = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < members.Length; i++)
        {
            T member = members[i];
            if (member.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                continue;

            string name = member.Name;
            if (member.IsDefined(typeof(DataMemberAttribute), true))
            {
                DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)Attribute.GetCustomAttribute(member, typeof(DataMemberAttribute), true);
                if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                    name = dataMemberAttribute.Name;
            }

            nameToMember.Add(name, member);
        }

        return nameToMember;
    }

    static object ParseObject(Type type, string json)
    {
        object instance = FormatterServices.GetUninitializedObject(type);

        //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
        List<string> elems = Split(json);
        if (elems.Count % 2 != 0)
            return instance;

        Dictionary<string, FieldInfo> nameToField;
        Dictionary<string, PropertyInfo> nameToProperty;
        if (!fieldInfoCache.TryGetValue(type, out nameToField))
        {
            nameToField = CreateMemberNameDictionary(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy));
            fieldInfoCache.Add(type, nameToField);
        }
        if (!propertyInfoCache.TryGetValue(type, out nameToProperty))
        {
            nameToProperty = CreateMemberNameDictionary(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy));
            propertyInfoCache.Add(type, nameToProperty);
        }

        for (int i = 0; i < elems.Count; i += 2)
        {
            if (elems[i].Length <= 2)
                continue;
            string key = elems[i].Substring(1, elems[i].Length - 2);
            string value = elems[i + 1];

            FieldInfo fieldInfo;
            PropertyInfo propertyInfo;
            if (nameToField.TryGetValue(key, out fieldInfo))
                fieldInfo.SetValue(instance, ParseValue(fieldInfo.FieldType, value));
            else if (nameToProperty.TryGetValue(key, out propertyInfo))
                propertyInfo.SetValue(instance, ParseValue(propertyInfo.PropertyType, value), null);
        }

        return instance;
    }
}
