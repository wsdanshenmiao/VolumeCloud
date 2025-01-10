using System;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeCloud : MonoBehaviour
{
    [SerializeField] Shader volumeCloudShader;

    Material volumeCloudMaterial;
    Camera renderCamera;

    [Header("体积云参数")]
    [Range(.2f, 1)]
    // 步进步频
    public float rayMarchingStride = .5f;
    // 体积云的范围
    public BoxCollider cloudRange;
    // 噪声图
    public Texture3D noiceTextrue;
    // 纹理坐标的缩放
    public float noiceTexScale = 1;
    // 采样噪声图的缩放
    public Vector4 noiceSampleOffset = Vector4.zero;

    void Awake()
    {
        renderCamera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        // 获取深度图来重构世界坐标
        renderCamera.depthTextureMode |= DepthTextureMode.Depth;
    }

    void Start()
    {
        volumeCloudMaterial = new Material(volumeCloudShader);
    }

    Vector3[] testPoint = new Vector3[4];
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Debug.Log("OnRenderImage");
        Matrix4x4 frustumCorners = Matrix4x4.identity;
        // Vector3[] frustumPoints = new Vector3[4];
        // // 获取摄像机的四个点来重建世界空间坐标
        // // The order of the corners is lower left, upper left, upper right, lower right
        // renderCamera.CalculateFrustumCorners(
        //     renderCamera.rect, renderCamera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumPoints);
        //
        // float scale = frustumPoints[0].magnitude / renderCamera.nearClipPlane;
        // for (int i = 0; i < frustumPoints.Length; ++i){
        //     frustumCorners.SetRow(i, frustumPoints[i].normalized * scale);
        //     testPoint[i] = frustumPoints[i];
        // }
        Transform cameraTrans = renderCamera.transform;
        float halfHeight = renderCamera.nearClipPlane * Mathf.Tan(renderCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 toTop = cameraTrans.up * halfHeight;
        Vector3 toRight = cameraTrans.right * halfHeight * renderCamera.aspect;

        Vector3[] frustumPoints = new Vector3[4];
        // 左下角的点
        frustumPoints[0] = cameraTrans.forward * renderCamera.nearClipPlane - toTop - toRight;
        // 左上角的点
        frustumPoints[1] = cameraTrans.forward * renderCamera.nearClipPlane + toTop - toRight;
        // 右上角的点
        frustumPoints[2] = cameraTrans.forward * renderCamera.nearClipPlane + toTop + toRight;
        // 右下角的点
        frustumPoints[3] = cameraTrans.forward * renderCamera.nearClipPlane - toTop + toRight;
        float scale = frustumPoints[0].magnitude / renderCamera.nearClipPlane;
        for (int i = 0; i < frustumPoints.Length; ++i){
            frustumCorners.SetRow(i, frustumPoints[i].normalized * scale);
        }

        Vector3 localScale = cloudRange.transform.localScale;
        Vector3 positiveScale = new Vector3(cloudRange.size.x * Mathf.Abs(localScale.x), 
            cloudRange.size.y * Mathf.Abs(localScale.y), 
            cloudRange.size.z * Mathf.Abs(localScale.z));
        Vector4 cloudBoxMin = cloudRange.transform.position + cloudRange.center - 0.5f * positiveScale;
        Vector4 cloudBoxMax = cloudRange.transform.position + cloudRange.center + 0.5f * positiveScale;

        volumeCloudMaterial.SetMatrix("_FrustumCorners", frustumCorners);
        volumeCloudMaterial.SetFloat("_RayMarchingStride", rayMarchingStride);
        volumeCloudMaterial.SetVector("_CloudBoxMin", cloudBoxMin);
        volumeCloudMaterial.SetVector("_CloudBoxMax", cloudBoxMax);
        volumeCloudMaterial.SetTexture("_NoiceTexture", noiceTextrue);
        volumeCloudMaterial.SetFloat("_NoiceTexScale", noiceTexScale);
        volumeCloudMaterial.SetVector("_NoicSampleOffset", noiceSampleOffset);

        Graphics.Blit(src, dest, volumeCloudMaterial);
    }

    /// <summary>
    /// 测试求交算法
    /// </summary>
    Vector2 testInsert;
    void TestRayInsert()
    {
        Vector3 localScale = cloudRange.transform.localScale;
        Vector3 positiveScale = new Vector3(cloudRange.size.x * Mathf.Abs(localScale.x), 
            cloudRange.size.y * Mathf.Abs(localScale.y), 
            cloudRange.size.z * Mathf.Abs(localScale.z));
        Vector4 cloudBoxMin = cloudRange.transform.position + cloudRange.center - 0.5f * positiveScale;
        Vector4 cloudBoxMax = cloudRange.transform.position + cloudRange.center + 0.5f * positiveScale;
        Vector3 invDir = new Vector3(1 / transform.forward.x, 1 / transform.forward.y, 1 / transform.forward.z);
        testInsert = RayInsertBox(cloudBoxMin, cloudBoxMax, transform.position, invDir);
    }
    Vector2 RayInsertBox(Vector3 boxMin, Vector3 boxMax, Vector3 origin, Vector3 invDir)
    {
        // 三个轴的 tEnter 和 tExit
        bool[] dirIsNeg = new bool[3];
        Vector3 tMins = (boxMin - origin);    // invDir为(1/x,1/y,1/z)
        Vector3 tMaxs = (boxMax - origin);
        for(int i =0;i<3;++i){
            dirIsNeg[i] = invDir[i] > 0;
            tMins[i] *= invDir[i];
            tMaxs[i] *= invDir[i];
        }
        for (int i = 0; i < 3; ++i) {
            if (!dirIsNeg[i]) {  // 若该轴为负从tMax进，tMin出,需要交换
                float tmp = tMins[i];
                tMins[i] = tMaxs[i];
                tMaxs[i] = tmp;
            }
        }
        float tMin = Mathf.Max(tMins.x, Mathf.Max(tMins.y, tMins.z));   // 取进入时间的最大值
        float tMax = Mathf.Min(tMaxs.x, Mathf.Min(tMaxs.y, tMaxs.z));   // 取离开时间的最小值

        if (tMin > tMax || tMax < 0) {
            return new Vector2(0, tMin);
        }
        return new Vector2(1, tMin);
    }

    void OnDrawGizmosSelected()
    {
        // Gizmos.DrawLine(transform.position, transform.position + transform.forward * testInsert.y);
        Gizmos.DrawWireCube(cloudRange.transform.position + cloudRange.center, cloudRange.size);
        // foreach(var point in testPoint){
        //     Gizmos.DrawSphere(transform.position + point, 1);
        // }
    }

}
