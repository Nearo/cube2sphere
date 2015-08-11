using UnityEngine;
using System.Collections;
using System.IO;


static class SaveCubeMap2SphericalMap
{
    //static private SaveCubeMap2SphericalMap mInstance = null;

    //define cube 6 plane
    static readonly private Vector3[] mPlaneUp = new Vector3[6];
    static readonly private Vector3[] mPlaneRight = new Vector3[6];
    static readonly private Vector3[] mPlaneForward = new Vector3[6];
    static readonly private float mToPlaneDis = Mathf.Cos(Mathf.PI * 0.25f);
    static readonly private Vector3[] mPlaneUV00 = new Vector3[6];

    //static public SaveCubeMap2SphericalMap GetInstance()
    //{
    //    if (mInstance == null)
    //        mInstance = new SaveCubeMap2SphericalMap();
    //    return mInstance;
    //}

    static SaveCubeMap2SphericalMap()
    {
        //initialize plane data for sampling
        int currentID = (int)CubemapFace.PositiveX;
        mPlaneForward[currentID] = Vector3.right;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);

        currentID = (int)CubemapFace.PositiveY;
        mPlaneForward[currentID] = Vector3.up;
        mPlaneUp[currentID] = Vector3.forward * -1;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);

        currentID = (int)CubemapFace.PositiveZ;
        mPlaneForward[currentID] = Vector3.forward;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);

        currentID = (int)CubemapFace.NegativeX;
        mPlaneForward[currentID] = Vector3.right * -1;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);

        currentID = (int)CubemapFace.NegativeY;
        mPlaneForward[currentID] = Vector3.up * -1;
        mPlaneUp[currentID] = Vector3.forward;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);

        currentID = (int)CubemapFace.NegativeZ;
        mPlaneForward[currentID] = Vector3.forward * -1;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
        mPlaneUV00[currentID] = _getPlaneVU00(currentID);
    }

    static private void _sphericalMapXY2Dir(float i, float j, float SphericalImageSizeX, float SphericalImageSizeY, out Vector3 dir)
    {
        //http://paulbourke.net//geometry/transformationprojection/index.html#cube2cyl

        float x = 2 * i / SphericalImageSizeX - 1;
        float y = 2 * j / SphericalImageSizeY - 1;

        float theta = x * Mathf.PI;
        float phi = y * Mathf.PI * 0.5f;

        x = Mathf.Cos(phi) * Mathf.Cos(theta);
        y = Mathf.Sin(phi);
        float z = Mathf.Cos(phi) * Mathf.Sin(theta);

        //Vector3 vec = new Vector3(x, y, z);
        //vec.Normalize(); no need to normalize
        dir.x = x;
        dir.y = y;
        dir.z = z;
    }

    static private Vector3 _getPlaneVU00(int cubeMapFaceID)
    {
        Vector3 planeUV00 = mPlaneForward[cubeMapFaceID] * mToPlaneDis;
        planeUV00 += mPlaneUp[cubeMapFaceID] * mToPlaneDis;
        planeUV00 += mPlaneRight[cubeMapFaceID] * mToPlaneDis * -1f;

        //bool isDrawDebug = false;
        //if (isDrawDebug)
        //{
        //    Debug.DrawLine(Vector3.zero, planeUV00, Color.white);

        //    Vector3 planeUV10 = mPlaneForward[cubeMapFaceID] * mToPlaneDis;
        //    planeUV10 += mPlaneUp[cubeMapFaceID] * mToPlaneDis;
        //    planeUV10 += mPlaneRight[cubeMapFaceID] * mToPlaneDis * 1f;
        //    Debug.DrawLine(Vector3.zero, planeUV10, Color.green);

        //    Vector3 planeUV01 = mPlaneForward[cubeMapFaceID] * mToPlaneDis;
        //    planeUV01 += mPlaneUp[cubeMapFaceID] * mToPlaneDis * -1;
        //    planeUV01 += mPlaneRight[cubeMapFaceID] * mToPlaneDis * -1f;
        //    Debug.DrawLine(Vector3.zero, planeUV01, Color.blue);

        //    Vector3 planeUV11 = mPlaneForward[cubeMapFaceID] * mToPlaneDis;
        //    planeUV11 += mPlaneUp[cubeMapFaceID] * mToPlaneDis * -1;
        //    planeUV11 += mPlaneRight[cubeMapFaceID] * mToPlaneDis * 1f;
        //    Debug.DrawLine(Vector3.zero, planeUV11, Color.yellow);
        //}

        return planeUV00;
    }

    static private void _processSpherePixel(int sphereX, int sphereY, int sphericalMapSizeX, int sphericalMapSizeY, Color[][] facePixels,
                                            int cubemapW, int cubemapH, out Color outColor)
    {
        //transfer xy on spherical map to 3D direction
        Vector3 vec;
        _sphericalMapXY2Dir(sphereX, sphereY, sphericalMapSizeX, sphericalMapSizeY, out vec);

        //get what face is been hit by direction, use max dot to get collide face.
        float maxDot = float.MinValue;
        int maxDotFace = -1;
        for (int a = 0; a < 6; a++)
        {
            float dot = Vector3.Dot(vec, mPlaneForward[a]);
            if (dot > maxDot)
            {
                maxDot = dot;
                maxDotFace = a;
            }
        }

        //get collision point on cube face
        float cosTheta = maxDot;// actually is (maxDot / 1);
        float collisionDis = mToPlaneDis / cosTheta; //cosTheta = mToPlaneDis / collisionDis
        Vector3 collisionPoint = vec * collisionDis;

        //get uv align plane face
        //Vector3 planeUV00 = _getPlaneVU00(maxDotFace);
        Vector3 offsetVec = collisionPoint - mPlaneUV00[maxDotFace]; //- planeUV00;
        float disU = Vector3.Dot(offsetVec, mPlaneRight[maxDotFace]);
        float disV = Vector3.Dot(offsetVec, mPlaneUp[maxDotFace] * -1);
        disU /= mToPlaneDis * 2;
        disV /= mToPlaneDis * 2;

        disU = Mathf.Clamp01(disU);
        disV = Mathf.Clamp01(disV);

        disU = (cubemapW - 1) * disU;
        disV = (cubemapH - 1) * disV;

        const bool bBilinear = true;
        outColor = Color.black;
        if (bBilinear)
        {
            //bilinear interpolation sampling
            int locU = Mathf.FloorToInt(disU);
            int locV = Mathf.FloorToInt(disV);
            int locU1 = Mathf.Clamp(locU + 1, 0, cubemapW - 1);
            int locV1 = Mathf.Clamp(locV + 1, 0, cubemapH - 1);

            float ratioU = disU - (float)locU;
            float ratioV = disV - (float)locV;

            Color[] pixels = facePixels[maxDotFace];
            Color interpolateU1 = Color.Lerp(pixels[locV * cubemapW + locU],
                                             pixels[locV * cubemapW + locU1], ratioU);
            Color interpolateU2 = Color.Lerp(pixels[locV1 * cubemapW + locU],
                                             pixels[locV1 * cubemapW + locU1], ratioU);
            outColor = Color.Lerp(interpolateU1, interpolateU2, ratioV);
        }
        else
        {
            //point sampling
            int loc = Mathf.FloorToInt(disV + 0.5f) * cubemapW + Mathf.FloorToInt(disU + 0.5f);
            outColor = facePixels[maxDotFace][Mathf.Clamp(loc, 0, facePixels[maxDotFace].Length - 1)];
        }
    }

    private class ThreadProcessData
    {
        public const bool useColor32 = false;
        public const int maxThreadAmount = 4;
        public int processLineStart;
        public int processLineEnd;
        public int sphericalMapSizeX;
        public int sphericalMapSizeY;
        public Color[][] facePixels;
        public int cubemapW;
        public int cubemapH;
        //public Texture2D sphereTex;
        public Color[] sphereColor;
        public Color32[] sphereColor32;

        public System.Threading.ManualResetEvent doneEvent; //https://msdn.microsoft.com/en-us/library/3dasc8as.aspx  wait for thread all done
    }

    public static void WorkA(object o)
    {
        ThreadProcessData data = o as ThreadProcessData;

        Debug.Log("transfer sphere line start: " + data.processLineStart + " ~ " + data.processLineEnd + " ; spherical Map Size (X,y) => (" + data.sphericalMapSizeX + " , " + data.sphericalMapSizeY + ")");

        Color color;
        for (int y = data.processLineStart - 1; y < data.processLineEnd; y++)
        {
            for (int x = 0; x < data.sphericalMapSizeX; x++)
            {
                _processSpherePixel(x, y, data.sphericalMapSizeX, data.sphericalMapSizeY, data.facePixels, data.cubemapW, data.cubemapH, out color);

                if (data.sphereColor != null)
                    data.sphereColor[y * data.sphericalMapSizeX + x] = color;
                if (data.sphereColor32 != null)
                    data.sphereColor32[y * data.sphericalMapSizeX + x] = new Color32( (byte)(color.r*255), (byte)(color.g * 255), (byte)(color.b * 255), 255);

                
                //data.sphereTex.SetPixel(x, y, color);
            }
        }

        Debug.Log("transfer sphere line done : " + data.processLineStart + " ~ " + data.processLineEnd + " ; spherical Map Size (X,y) => (" + data.sphericalMapSizeX + " , " + data.sphericalMapSizeY + ")");
        data.doneEvent.Set();
    }

    static public Texture2D CreateSphericalMapFromCubeMap(Cubemap cubemap)
    {
        int sphericalMapSizeX = 5000;// cubemap.width * 2;
        int sphericalMapSizeY = 2500;// cubemap.height * 1;

        Debug.Log("CubeMap transfer to SphericalMap start");

        

        Color[][] facePixels = new Color[6][];
        for (int a = 0; a < 6; a++)
        {
            facePixels[a] = cubemap.GetPixels((CubemapFace)a);

            //facePixels[a] = new Color[cubemap.width * cubemap.height];
            //pixels.CopyTo(facePixels[a], 0);
        }

        Color[] sphereColors = null;
        Color32[] sphereColors32 = null;
        if (ThreadProcessData.useColor32)
            sphereColors32 = new Color32[sphericalMapSizeX * sphericalMapSizeY];
        else
            sphereColors = new Color[sphericalMapSizeX * sphericalMapSizeY];

        //Texture2D sphereTex = new Texture2D(sphericalMapSizeX, sphericalMapSizeY, TextureFormat.RGB24, false);

        //for (int a = 0; a < sphereColors.Length; a++)
        //{
        //    sphereColors[a] = Color.red;
        //}

        //I prefer use 1 thread to process at least 1 line on spherical map.
        Debug.Assert(sphericalMapSizeY >= ThreadProcessData.maxThreadAmount);//I don't want to handle sphericalMapSizeY/maxThreadAmount=0

        int eachThreadProcessLine = Mathf.FloorToInt((float)sphericalMapSizeY / (float)ThreadProcessData.maxThreadAmount + 0.5f);
        ThreadProcessData[] threadProcessData = new ThreadProcessData[ThreadProcessData.maxThreadAmount];
        int processLineStart = 1;//start from 1 (not 0)
        int processLineEnd = processLineStart + eachThreadProcessLine - 1;
        for (int a = 0; a < ThreadProcessData.maxThreadAmount; a++)
        {
            threadProcessData[a] = new ThreadProcessData();
            threadProcessData[a].processLineStart = processLineStart;
            threadProcessData[a].processLineEnd = processLineEnd;
            threadProcessData[a].sphericalMapSizeX = sphericalMapSizeX;
            threadProcessData[a].sphericalMapSizeY = sphericalMapSizeY;
            threadProcessData[a].facePixels = facePixels;
            threadProcessData[a].cubemapW = cubemap.width;
            threadProcessData[a].cubemapH = cubemap.height;
            threadProcessData[a].sphereColor = sphereColors;
            threadProcessData[a].sphereColor32 = sphereColors32;

            processLineStart = processLineEnd + 1;
            processLineEnd = processLineStart + eachThreadProcessLine - 1;
            if (processLineEnd > sphericalMapSizeY)
                processLineEnd = sphericalMapSizeY;
        }

        System.Threading.ManualResetEvent[] doneEvents = new System.Threading.ManualResetEvent[threadProcessData.Length];
        int count = 0;
        foreach (ThreadProcessData data in threadProcessData)
        {
            doneEvents[count] = new System.Threading.ManualResetEvent(false);
            data.doneEvent = doneEvents[count];

            System.Threading.WaitCallback waitCallback = new System.Threading.WaitCallback(WorkA);
            System.Threading.ThreadPool.QueueUserWorkItem(waitCallback, data);

            count++;
        }

        // Wait for all threads in pool to calculate.
        System.Threading.WaitHandle.WaitAll(doneEvents);
        Debug.Log("All calculations are complete.");

        //sphereColor
        Texture2D sphereTex = null;
        if (ThreadProcessData.useColor32)
        {
            sphereTex = new Texture2D(sphericalMapSizeX, sphericalMapSizeY, TextureFormat.ARGB32, false);
            sphereTex.SetPixels32(sphereColors32);
        }
        else
        {
            sphereTex = new Texture2D(sphericalMapSizeX, sphericalMapSizeY, TextureFormat.RGB24, false);
            sphereTex.SetPixels(sphereColors);
        }

        //else
        //{
        //    //for (int x = 0; x < sphereTex.width; x++)
        //    //    for (int y = 0; y < sphereTex.height; y++)
        //    //        sphereTex.SetPixel(x, y, Color.black);

        //    for (int x = 0; x < sphericalMapSizeX; x++)
        //    {
        //        for (int y = 0; y < sphericalMapSizeY; y++)
        //        {
        //            Color color;
        //            _processSpherePixel(x, y, sphericalMapSizeX, sphericalMapSizeY, facePixels, cubemap.width, cubemap.height, out color);
        //            sphereTex.SetPixel(x, y, color);
        //        }
        //    }
        //}

        //var bytes = sphereTex.EncodeToJPG();
        //File.WriteAllBytes(Application.dataPath + "/cube2sphere.jpg", bytes);
        //DestroyImmediate(sphereTex);

        Debug.Log("CubeMap transfer to SphericalMap done!!");
        return sphereTex;
    }

    static public void RenderDebugSphericalSampleRayInfo(int sphericalImageSizeX, int sphericalImageSizeY, bool writeDebugFile)
    {
        System.Text.StringBuilder sb = null;
        if (writeDebugFile)
        {
            sb = new System.Text.StringBuilder();
            sb.AppendLine("test begin");
        }

        for (int i = 0; i < sphericalImageSizeX; i++)
        {
            for (int j = 0; j < sphericalImageSizeY; j++)
            {
                Vector3 vec;
                _sphericalMapXY2Dir(i, j, sphericalImageSizeX, sphericalImageSizeY, out vec);

                if (Mathf.Abs(Vector3.Dot(Vector3.right, vec)) > 0.5)
                    Debug.DrawLine(Vector3.zero, vec, Color.red);
                /*else if(Mathf.Abs(Vector3.Dot(Vector3.forward, vec) ) > 0.5)
					Debug.DrawLine (Vector3.zero, vec, Color.green);
				else if(Mathf.Abs(Vector3.Dot(Vector3.right, vec) ) > 0.5)
					Debug.DrawLine (Vector3.zero, vec, Color.blue);*/

                if (writeDebugFile)
                {
                    sb.AppendLine(vec.ToString());
                }
            }
        }

        if (writeDebugFile)
        {
            sb.AppendLine("test end");
            System.IO.File.WriteAllText(Application.dataPath + "/sampleray.txt",
                                    sb.ToString());

            Debug.Log("WriteDebugFile done!!!!!!!!!!!!!!!!!!!");
        }
    }
}

public class RenderToCubemap : MonoBehaviour
{
    public Cubemap cubemap;
    public float updateRate = 100000f;
    private float updateRateRec = 0;

    void LateUpdate()
    {
        //Debug.Log ("Time.time - updateRateRec = " + (Time.time - updateRateRec).ToString());
        if (Time.time - updateRateRec > updateRate)
        {
            Debug.Log("Time.time - updateRateRec = " + (Time.time - updateRateRec).ToString() + "updateRate = " + updateRate.ToString() + "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
            updateRateRec = Time.time;

            RenderMe();

            //currentMaterial.SetTexture("_Cube", cubemap);
            //GetComponent<Renderer>().material = currentMaterial;
        }

        if (Input.GetKeyUp("space"))
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();//-------------
            RenderMe();
            stopwatch.Stop();
            sb.AppendLine("RenderMe() cost : " + stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();


            stopwatch.Start();//-------------
            //SaveCubemap();
            Texture2D sphereTex = SaveCubeMap2SphericalMap.CreateSphericalMapFromCubeMap(cubemap);
            stopwatch.Stop();
            sb.AppendLine("CreateSphericalMapFromCubeMap() cost : " + stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();

            stopwatch.Start();//-------------
            var bytes = sphereTex.EncodeToJPG();
            File.WriteAllBytes(Application.dataPath + "/cube2sphere.jpg", bytes);
            stopwatch.Stop();
            sb.AppendLine("EncodeToJPG() cost : " + stopwatch.ElapsedMilliseconds);



            System.IO.File.WriteAllText(Application.dataPath + "/CostTime.txt", sb.ToString());
            Debug.Log("save cube map done!!");
            DestroyImmediate(sphereTex);
        }

        bool writeDebugFile = false;
        if (Input.GetKeyUp("d"))
            writeDebugFile = true;
        SaveCubeMap2SphericalMap.RenderDebugSphericalSampleRayInfo(16, 16, writeDebugFile);

        //Debug.Log("Mathf.Sin(Mathf.PI * 0.5f)=" + Mathf.Sin(Mathf.PI * 0.25f).ToString("0.000")  );
        //Debug.Log("Mathf.Cos(Mathf.PI * 0.5f)=" + Mathf.Cos(Mathf.PI * 0.25f).ToString("0.000")  );
    }

    void RenderMe()
    {
        GameObject go = new GameObject("CubemapCamera" + Random.seed);
        Camera camera = go.AddComponent<Camera>();
        camera.backgroundColor = Color.black;
        camera.cullingMask = ~(1 << 8);
        camera.transform.position = transform.position;
        camera.transform.rotation = Quaternion.identity;
        camera.RenderToCubemap(cubemap);

        //	Debug.Log ("go.GetComponent<Camera>().RenderToCubemap (cubemap);");
        DestroyImmediate(go);
    }

    void SaveCubemap()
    {
        Debug.Log("save cube map start");
        Texture2D tex = new Texture2D(cubemap.width, cubemap.height, TextureFormat.RGB24, false);

        _saveCubeMapFace2File(tex, CubemapFace.PositiveX);
        _saveCubeMapFace2File(tex, CubemapFace.PositiveY);
        _saveCubeMapFace2File(tex, CubemapFace.PositiveZ);
        _saveCubeMapFace2File(tex, CubemapFace.NegativeX);
        _saveCubeMapFace2File(tex, CubemapFace.NegativeY);
        _saveCubeMapFace2File(tex, CubemapFace.NegativeZ);

        DestroyImmediate(tex);

        Debug.Log("save cube map done!!");
    }
    
    void _saveCubeMapFace2File(Texture2D tex, CubemapFace face)
    {
        // Read screen contents into the texture        
        tex.SetPixels(cubemap.GetPixels(face));
        // Encode texture into PNG
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + cubemap.name + face.ToString() + ".png", bytes);
    }



}