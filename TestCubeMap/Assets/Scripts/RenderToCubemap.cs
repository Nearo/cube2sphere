using UnityEngine;
using System.Collections;
using System.IO;


class SaveCubeMap2SphericalMap
{
    //static private SaveCubeMap2SphericalMap mInstance = null;

    //define cube 6 plane
    static private Vector3[] mPlaneUp = new Vector3[6];
    static private Vector3[] mPlaneRight = new Vector3[6];
    static private Vector3[] mPlaneForward = new Vector3[6];
    static private float mToPlaneDis = Mathf.Cos(Mathf.PI * 0.25f);
    
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

        currentID = (int)CubemapFace.PositiveY;
        mPlaneForward[currentID] = Vector3.up;
        mPlaneUp[currentID] = Vector3.forward * -1;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        currentID = (int)CubemapFace.PositiveZ;
        mPlaneForward[currentID] = Vector3.forward;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        currentID = (int)CubemapFace.NegativeX;
        mPlaneForward[currentID] = Vector3.right * -1;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        currentID = (int)CubemapFace.NegativeY;
        mPlaneForward[currentID] = Vector3.up * -1;
        mPlaneUp[currentID] = Vector3.forward;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        currentID = (int)CubemapFace.NegativeZ;
        mPlaneForward[currentID] = Vector3.forward * -1;
        mPlaneUp[currentID] = Vector3.up;
        mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);
    }

    static public Vector3 SphericalMapXY2Dir(float i, float j, float SphericalImageSizeX, float SphericalImageSizeY)
    {
        //http://paulbourke.net//geometry/transformationprojection/index.html#cube2cyl

        float x = 2 * i / SphericalImageSizeX - 1;
        float y = 2 * j / SphericalImageSizeY - 1;

        float theta = x * Mathf.PI;
        float phi = y * Mathf.PI * 0.5f;

        x = Mathf.Cos(phi) * Mathf.Cos(theta);
        y = Mathf.Sin(phi);
        float z = Mathf.Cos(phi) * Mathf.Sin(theta);

        Vector3 vec = new Vector3(x, y, z);
        vec.Normalize();
        return vec;
    }

    static public Vector3 _getPlaneVU00(int cubeMapFaceID)
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

    static public Texture2D CreateSphericalMapFromCubeMap(Cubemap cubemap)
    {
        int sphericalMapSizeX = 5000;// cubemap.width * 2;
        int sphericalMapSizeY = 2500;// cubemap.height * 1;

        Color[][] facePixels = new Color[6][];
        for (int a = 0; a < 6; a++)
        {
            Color[] pixels = cubemap.GetPixels((CubemapFace)a);

            //facePixels[a] = new Color[cubemap.width * cubemap.height];
            //pixels.CopyTo(facePixels[a], 0);

            facePixels[a] = pixels;
        }

        Debug.Log("CubeMap transfer to SphericalMap start");

        Texture2D sphereTex = new Texture2D(sphericalMapSizeX, sphericalMapSizeY, TextureFormat.RGB24, false);
        //for (int x = 0; x < sphereTex.width; x++)
        //    for (int y = 0; y < sphereTex.height; y++)
        //        sphereTex.SetPixel(x, y, Color.black);

        for (int x = 0; x < sphericalMapSizeX; x++)
        {
            for (int y = 0; y < sphericalMapSizeY; y++)
            {
                Vector3 vec = SphericalMapXY2Dir(x, y, sphericalMapSizeX, sphericalMapSizeY);

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

                //get collision point
                float cosTheta = maxDot;
                float collisionDis = mToPlaneDis / cosTheta;
                Vector3 collisionPoint = vec * collisionDis;

                //get uv align plane face
                Vector3 planeUV00 = _getPlaneVU00(maxDotFace);
                Vector3 offsetVec = collisionPoint - planeUV00;
                float disU = Vector3.Dot(offsetVec, mPlaneRight[maxDotFace]);
                float disV = Vector3.Dot(offsetVec, mPlaneUp[maxDotFace] * -1);
                disU /= mToPlaneDis * 2;
                disV /= mToPlaneDis * 2;

                disU = Mathf.Clamp01(disU);
                disV = Mathf.Clamp01(disV);

                disU = (cubemap.width - 1) * disU;
                disV = (cubemap.height - 1) * disV;

                //bilinear interpolation sampling
                int locU = Mathf.FloorToInt(disU);
                int locV = Mathf.FloorToInt(disV);
                int locU1 = Mathf.Clamp(locU + 1, 0, cubemap.width - 1);
                int locV1 = Mathf.Clamp(locV + 1, 0, cubemap.height - 1);

                float ratioU = disU - (float)locU;
                float ratioV = disV - (float)locV;

                Color[] pixels = facePixels[maxDotFace];
                Color interpolateU1 = Color.Lerp(pixels[locV * cubemap.width + locU],
                                                 pixels[locV * cubemap.width + locU1], ratioU);
                Color interpolateU2 = Color.Lerp(pixels[locV1 * cubemap.width + locU],
                                                 pixels[locV1 * cubemap.width + locU1], ratioU);
                Color facecolor = Color.Lerp(interpolateU1, interpolateU2, ratioV);

                //point sampling
                //int loc = Mathf.FloorToInt(disV + 0.5f) * cubemap.width + Mathf.FloorToInt(disU + 0.5f);
                //Color facecolor = facePixels[maxDotFace][Mathf.Clamp(loc, 0, facePixels[maxDotFace].Length - 1)];

                sphereTex.SetPixel(x, y, facecolor);
            }
        }
        
        //var bytes = sphereTex.EncodeToJPG();
        //File.WriteAllBytes(Application.dataPath + "/cube2sphere.jpg", bytes);
        //DestroyImmediate(sphereTex);

        Debug.Log("CubeMap transfer to SphericalMap done!!");
        return sphereTex;
    }
}

//[ExecuteInEditMode]
public class RenderToCubemap : MonoBehaviour
{
    public Cubemap cubemap;
    public Material currentMaterial;
    public float updateRate = 100000f;
    [SerializeField]
    private Transform renderFromPosition;
    private float updateRateRec = 0;

    void LateUpdate()
    {
        //Debug.Log ("Time.time - updateRateRec = " + (Time.time - updateRateRec).ToString());
        if (Time.time - updateRateRec > updateRate)
        {
            Debug.Log("Time.time - updateRateRec = " + (Time.time - updateRateRec).ToString() + "updateRate = " + updateRate.ToString() + "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");
            updateRateRec = Time.time;

            RenderMe();
            currentMaterial.SetTexture("_Cube", cubemap);
            GetComponent<Renderer>().material = currentMaterial;
        }

        if (Input.GetKeyUp("space"))
        {
            //SaveCubemap();
            _saveCubeMap2SphericalMap();
        }

        bool writeDebugFile = false;
        if (Input.GetKeyUp("d"))
            writeDebugFile = true;
        _renderDebugSphericalSampleRayInfo(16, 16, writeDebugFile);

        //Debug.Log("Mathf.Sin(Mathf.PI * 0.5f)=" + Mathf.Sin(Mathf.PI * 0.25f).ToString("0.000")  );
        //Debug.Log("Mathf.Cos(Mathf.PI * 0.5f)=" + Mathf.Cos(Mathf.PI * 0.25f).ToString("0.000")  );
    }

    void RenderMe()
    {

        GameObject go = new GameObject("CubemapCamera" + Random.seed);
        go.AddComponent<Camera>();

        go.GetComponent<Camera>().backgroundColor = Color.black;
        go.GetComponent<Camera>().cullingMask = ~(1 << 8);
        go.GetComponent<Camera>().transform.position = renderFromPosition.position;
        if (renderFromPosition.GetComponent<Renderer>())
            go.transform.position = renderFromPosition.GetComponent<Renderer>().bounds.center;

        go.GetComponent<Camera>().transform.rotation = Quaternion.identity;

        go.GetComponent<Camera>().RenderToCubemap(cubemap);

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

    //define cube 6 plane
    Vector3[] mPlaneUp = new Vector3[6];
    Vector3[] mPlaneRight = new Vector3[6];
    Vector3[] mPlaneForward = new Vector3[6];
    float mToPlaneDis = Mathf.Cos(Mathf.PI * 0.25f);



    void _saveCubeMap2SphericalMap()
    {
        Texture2D sphereTex = null;
        sphereTex = SaveCubeMap2SphericalMap.CreateSphericalMapFromCubeMap(cubemap);

        //int currentID = (int)CubemapFace.PositiveX;
        //mPlaneForward[currentID] = Vector3.right;
        //mPlaneUp[currentID] = Vector3.up;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);


        //currentID = (int)CubemapFace.PositiveY;
        //mPlaneForward[currentID] = Vector3.up;
        //mPlaneUp[currentID] = Vector3.forward * -1;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        //currentID = (int)CubemapFace.PositiveZ;
        //mPlaneForward[currentID] = Vector3.forward;
        //mPlaneUp[currentID] = Vector3.up;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        //currentID = (int)CubemapFace.NegativeX;
        //mPlaneForward[currentID] = Vector3.right * -1;
        //mPlaneUp[currentID] = Vector3.up;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        //currentID = (int)CubemapFace.NegativeY;
        //mPlaneForward[currentID] = Vector3.up * -1;
        //mPlaneUp[currentID] = Vector3.forward;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        //currentID = (int)CubemapFace.NegativeZ;
        //mPlaneForward[currentID] = Vector3.forward * -1;
        //mPlaneUp[currentID] = Vector3.up;
        //mPlaneRight[currentID] = Vector3.Cross(mPlaneUp[currentID], mPlaneForward[currentID]);

        //int sphericalMapSizeX = 5000;// cubemap.width * 2;
        //int sphericalMapSizeY = 2500;// cubemap.height * 1;
        

        //Color[][] facePixels = new Color[6][];
        //for (int a = 0; a < 6; a++)
        //{
        //    Color[] pixels = cubemap.GetPixels((CubemapFace)a);
        //    facePixels[a] = new Color[cubemap.width * cubemap.height];
        //    pixels.CopyTo(facePixels[a], 0);
        //}

        //Debug.Log("save cube map start");

        //sphereTex = new Texture2D(sphericalMapSizeX, sphericalMapSizeY, TextureFormat.RGB24, false);
        //for (int x = 0; x < sphereTex.width; x++)
        //    for (int y = 0; y < sphereTex.height; y++)
        //        sphereTex.SetPixel(x, y, Color.black);

        //for (int x = 0; x < sphericalMapSizeX; x++)
        //{
        //    for (int y = 0; y < sphericalMapSizeY; y++)
        //    {
        //        Vector3 vec = SaveCubeMap2SphericalMap.SphericalMapXY2Dir(x, y, sphericalMapSizeX, sphericalMapSizeY);

        //        float maxDot = float.MinValue;
        //        int maxDotFace = -1;
        //        for (int a = 0; a < 6; a++)
        //        {
        //            float dot = Vector3.Dot(vec, mPlaneForward[a]);
        //            if (dot > maxDot)
        //            {
        //                maxDot = dot;
        //                maxDotFace = a;
        //            }
        //        }

        //        //get collision point
        //        float cosTheta = maxDot;
        //        float collisionDis = mToPlaneDis / cosTheta;
        //        Vector3 collisionPoint = vec * collisionDis;

        //        //get uv align plane face
        //        Vector3 planeUV00 = SaveCubeMap2SphericalMap._getPlaneVU00(maxDotFace);
        //        Vector3 offsetVec = collisionPoint - planeUV00;
        //        float disU = Vector3.Dot(offsetVec, mPlaneRight[maxDotFace]);
        //        float disV = Vector3.Dot(offsetVec, mPlaneUp[maxDotFace] * -1);
        //        disU /= mToPlaneDis * 2;
        //        disV /= mToPlaneDis * 2;

        //        disU = Mathf.Clamp01(disU);
        //        disV = Mathf.Clamp01(disV);

        //        disU = (cubemap.width - 1) * disU;
        //        disV = (cubemap.height - 1) * disV;

        //        //bilinear interpolation sampling
        //        int locU = Mathf.FloorToInt(disU);
        //        int locV = Mathf.FloorToInt(disV);
        //        int locU1 = Mathf.Clamp(locU + 1, 0, cubemap.width - 1);
        //        int locV1 = Mathf.Clamp(locV + 1, 0, cubemap.height - 1);

        //        float ratioU = disU - (float)locU;
        //        float ratioV = disV - (float)locV;

        //        Color[] pixels = facePixels[maxDotFace];
        //        Color interpolateU1 = Color.Lerp(pixels[locV * cubemap.width + locU],
        //                                         pixels[locV * cubemap.width + locU1], ratioU);
        //        Color interpolateU2 = Color.Lerp(pixels[locV1 * cubemap.width + locU],
        //                                         pixels[locV1 * cubemap.width + locU1], ratioU);
        //        Color facecolor = Color.Lerp(interpolateU1, interpolateU2, ratioV);

        //        //point sampling
        //        //int loc = Mathf.FloorToInt(disV + 0.5f) * cubemap.width + Mathf.FloorToInt(disU + 0.5f);
        //        //Color facecolor = facePixels[maxDotFace][Mathf.Clamp(loc, 0, facePixels[maxDotFace].Length - 1)];

        //        sphereTex.SetPixel(x, y, facecolor);
        //    }
        //}


        var bytes = sphereTex.EncodeToJPG();
        File.WriteAllBytes(Application.dataPath + "/cube2sphere.jpg", bytes);

        DestroyImmediate(sphereTex);

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

    void _renderDebugSphericalSampleRayInfo(int sphericalImageSizeX, int sphericalImageSizeY, bool writeDebugFile)
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
                Vector3 vec = SaveCubeMap2SphericalMap.SphericalMapXY2Dir(i, j, sphericalImageSizeX, sphericalImageSizeY);

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