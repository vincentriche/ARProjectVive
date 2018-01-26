using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.Util;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.Face;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Valve.VR;


public class CameraDetection : MonoBehaviour
{
    public SteamVR_TrackedObject trackedObj;
    private SteamVR_Controller.Device Controller
    {
        get
        {
            if (trackedObj.index == SteamVR_TrackedObject.EIndex.None)
                return null;
            return SteamVR_Controller.Input((int)trackedObj.index);
        }
    }

    public RawImage rawImageDisplay;
    public int camNumber = 0;
    public GameObject target;
    public Camera mainCamera;
    public float patternScale = 1.0f;
    public Vector2 pattern = new Vector2(7, 4);
    public Vector2 requestedResolution = new Vector2(640, 480);
    public FlipType flip = FlipType.Horizontal;
    public Vector3 localOffset;
    public float xOffsetAngle;
    public float zOffsetAngle;

    private Size patternSize;
    private MCvTermCriteria criteria = new MCvTermCriteria(100, 1e-5);

    public static Texture2D webcamTexture;
    private static Texture2D displayTexture;
    private Color32[] data;
    private byte[] bytes;
    private byte[] grayBytes;

    private Matrix<float> cvImageCorners;
    private Matrix<double> cvWorldCorners;
    private Matrix<double> cvIntrinsicParams;
    private Matrix<double> cvDistortionParams;


    void OnEnable()
    {
        // Set target scale
        patternSize = new Size((int)pattern.x, (int)pattern.y);
        target.transform.localScale = new Vector3(patternScale * (patternSize.Width + 1), patternScale * (patternSize.Height + 1), 1.0f);

        // Construct world corner points
        Vector2 offset = new Vector2(patternSize.Width / 2.0f * patternScale, patternSize.Height / 2.0f * patternScale);
        cvWorldCorners = new Matrix<double>(patternSize.Height * patternSize.Width, 1, 3);
        for (int iy = 0; iy < patternSize.Height; iy++)
        {
            for (int ix = 0; ix < patternSize.Width; ix++)
            {
                cvWorldCorners.Data[iy * patternSize.Width + ix, 0] = ix * patternScale - offset.x;
                cvWorldCorners.Data[iy * patternSize.Width + ix, 1] = iy * patternScale - offset.y;
                cvWorldCorners.Data[iy * patternSize.Width + ix, 2] = 0;
            }
        }


        webcamTexture = TrackedCameraScript.GetViveCameraTexture();
        if (webcamTexture == null)
        {
            return;
        }

        HmdVector2_t focalLength = new HmdVector2_t();
        HmdVector2_t opticCenter = new HmdVector2_t();
        OpenVR.TrackedCamera.GetCameraIntrinsics(0, EVRTrackedCameraFrameType.Undistorted, ref focalLength, ref opticCenter);

        // Initialize intrinsic parameters
        cvIntrinsicParams = new Matrix<double>(3, 3, 1);
        cvIntrinsicParams[0, 0] = focalLength.v0;
        cvIntrinsicParams[0, 1] = 0;
        cvIntrinsicParams[0, 2] = opticCenter.v0;
        cvIntrinsicParams[1, 0] = 0;
        cvIntrinsicParams[1, 1] = focalLength.v1;
        cvIntrinsicParams[1, 2] = opticCenter.v1;
        cvIntrinsicParams[2, 0] = 0;
        cvIntrinsicParams[2, 1] = 0;
        cvIntrinsicParams[2, 2] = 1;

        cvDistortionParams = new Matrix<double>(4, 1, 1);
        cvDistortionParams[0, 0] = 0.0f;
        cvDistortionParams[1, 0] = 0.0f;
        cvDistortionParams[2, 0] = 0.0f;
        cvDistortionParams[3, 0] = 0.0f;
    }

    void Update()
    {
        webcamTexture = TrackedCameraScript.GetViveCameraTexture();
        if (webcamTexture == null)
        {
            return;
        }

        if (webcamTexture != null)
        {
            if (data == null || (data.Length != webcamTexture.width * webcamTexture.height))
                data = new Color32[webcamTexture.width * webcamTexture.height];

            data = webcamTexture.GetPixels32();
            //data = webcamTexture.GetPixels32(0);

            if (bytes == null || bytes.Length != data.Length * 3)
                bytes = new byte[data.Length * 3];
            if (grayBytes == null || grayBytes.Length != data.Length * 1)
                grayBytes = new byte[data.Length * 1];


            // OPENCV PROCESSING
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            GCHandle resultHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            GCHandle grayHandle = GCHandle.Alloc(grayBytes, GCHandleType.Pinned);

            Mat currentWebcamMat = new Mat(new Size(webcamTexture.width, webcamTexture.height), DepthType.Cv8U, 4, handle.AddrOfPinnedObject(), webcamTexture.width * 4);
            Mat resultMat = new Mat(webcamTexture.height, webcamTexture.width, DepthType.Cv8U, 3, resultHandle.AddrOfPinnedObject(), webcamTexture.width * 3);
            Mat grayMat = new Mat(webcamTexture.height, webcamTexture.width, DepthType.Cv8U, 1, grayHandle.AddrOfPinnedObject(), webcamTexture.width * 1);

            CvInvoke.CvtColor(currentWebcamMat, resultMat, ColorConversion.Bgra2Bgr);
            CvInvoke.CvtColor(resultMat, grayMat, ColorConversion.Bgra2Gray);

            cvImageCorners = new Matrix<float>(patternSize.Width * patternSize.Height, 1, 2);
            if (Controller != null && Controller.GetHairTrigger() == false)
            {
                bool detected = DetectCheckerboard(grayMat, resultMat);
                if (detected)
                    SetCameraTransformFromChessboard();
            }


            handle.Free();
            resultHandle.Free();
            grayHandle.Free();

            if (flip != FlipType.None)
                CvInvoke.Flip(resultMat, resultMat, flip);
            if (displayTexture == null || displayTexture.width != webcamTexture.width ||
                displayTexture.height != webcamTexture.height)
            {
                displayTexture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGB24, false);
            }
            displayTexture.LoadRawTextureData(bytes);
            displayTexture.Apply();
            TrackedCameraScript.Instance.screenRenderer.material.mainTexture = displayTexture;
        }

        if (displayTexture != null)
        {
            rawImageDisplay.texture = displayTexture;
        }
    }

    private bool DetectCheckerboard(Mat detectImage, Mat drawImage = null)
    {
        bool result = CvInvoke.FindChessboardCorners(detectImage, patternSize, cvImageCorners);

        if (result == false)
            return false;

        CvInvoke.CornerSubPix(detectImage, cvImageCorners, new Size(5, 5), new Size(-1, -1), criteria);

        if (drawImage != null)
            CvInvoke.DrawChessboardCorners(drawImage, patternSize, cvImageCorners, true);

        return true;
    }

    private void SetCameraTransformFromChessboard()
    {
        Matrix<float>[] split = cvImageCorners.Split();
        Matrix<double> doubleCvImageCorners = new Matrix<double>(patternSize.Height * patternSize.Width, 1, 2);
        for (int iy = 0; iy < patternSize.Height; iy++)
        {
            for (int ix = 0; ix < patternSize.Width; ix++)
            {
                doubleCvImageCorners.Data[iy * patternSize.Width + ix, 0] = split[0][iy * patternSize.Width + ix, 0];
                doubleCvImageCorners.Data[iy * patternSize.Width + ix, 1] = split[1][iy * patternSize.Width + ix, 0];
            }
        }

        // Compute the rotation / translation of the chessboard (the cameras extrinsic pramaters)
        Mat tempRotation = new Mat(3, 1, DepthType.Cv64F, 1);
        Mat translationMatrix = new Mat(3, 1, DepthType.Cv64F, 1);
        bool res = CvInvoke.SolvePnP(cvWorldCorners, cvImageCorners, cvIntrinsicParams, cvDistortionParams, tempRotation, translationMatrix);
        if (res == false)
            return;

        // Converte the rotation from 3 axis rotations into a rotation matrix.
        Mat rotationMatrix = new Mat(3, 3, DepthType.Cv64F, 1);
        CvInvoke.Rodrigues(tempRotation, rotationMatrix);

        double[] rotationData = new double[9];
        Marshal.Copy(rotationMatrix.DataPointer, rotationData, 0, rotationMatrix.Width * rotationMatrix.Height);
        double[] translationData = new double[3];
        Marshal.Copy(translationMatrix.DataPointer, translationData, 0, translationMatrix.Width * translationMatrix.Height);

        Matrix4x4 cvModelView = new Matrix4x4();
        cvModelView.m00 = (float)rotationData[0 * 3 + 0];
        cvModelView.m10 = (float)rotationData[1 * 3 + 0];
        cvModelView.m20 = (float)rotationData[2 * 3 + 0];
        cvModelView.m30 = 0;

        cvModelView.m01 = (float)rotationData[0 * 3 + 1];
        cvModelView.m11 = (float)rotationData[1 * 3 + 1];
        cvModelView.m21 = (float)rotationData[2 * 3 + 1];
        cvModelView.m31 = 0;

        cvModelView.m02 = (float)rotationData[0 * 3 + 2];
        cvModelView.m12 = (float)rotationData[1 * 3 + 2];
        cvModelView.m22 = (float)rotationData[2 * 3 + 2];
        cvModelView.m32 = 0;

        cvModelView.m03 = (float)translationData[0];
        cvModelView.m13 = (float)translationData[1];
        cvModelView.m23 = (float)translationData[2];
        cvModelView.m33 = 1;
        //cvModelView = cvModelView.inverse;

        Vector3 position = ExtractPosition(cvModelView) + localOffset;
        Quaternion rotation = ExtractRotation(cvModelView)
            * Quaternion.AngleAxis(zOffsetAngle, Vector3.forward) 
            * Quaternion.AngleAxis(180, Vector3.up);

        GameObject temp = new GameObject();
        temp.transform.position = position;
        temp.transform.rotation = rotation;
        temp.transform.RotateAround(Vector3.zero, Vector3.right, xOffsetAngle);
        temp.transform.RotateAround(position, Vector3.forward, zOffsetAngle);
        position = temp.transform.position;
        rotation = temp.transform.rotation;
        Destroy(temp);

        Matrix4x4 corrected = Vector3QuatToMatrix(position, rotation);
        corrected = mainCamera.transform.localToWorldMatrix * corrected;
        target.transform.position = ExtractPosition(corrected);
        target.transform.rotation = ExtractRotation(corrected) * Quaternion.AngleAxis(180, Vector3.up);
    }

    public Matrix4x4 ExtractMatrixCorrected(Matrix4x4 opencv)
    {
        Matrix4x4 res = new Matrix4x4();
        res.m00 = opencv.m00;
        res.m10 = -opencv.m10;
        res.m20 = opencv.m20;
        res.m30 = 0;

        res.m01 = opencv.m01;
        res.m11 = -opencv.m11;
        res.m21 = opencv.m21;
        res.m31 = 0;

        res.m02 = opencv.m02;
        res.m12 = -opencv.m12;
        res.m22 = opencv.m22;
        res.m32 = 0;

        res.m03 = opencv.m03;
        res.m13 = -opencv.m13;
        res.m23 = opencv.m23;
        res.m33 = 1;
        return res;
    }

    public Matrix4x4 Vector3QuatToMatrix(Vector3 pos, Quaternion rot)
    {
        Matrix4x4 rotM = Matrix44FromQuat(rot);

        Matrix4x4 res = new Matrix4x4();
        res.m00 = rotM.m00;
        res.m10 = -rotM.m10;
        res.m20 = rotM.m20;
        res.m30 = 0;

        res.m01 = rotM.m01;
        res.m11 = -rotM.m11;
        res.m21 = rotM.m21;
        res.m31 = 0;

        res.m02 = rotM.m02;
        res.m12 = -rotM.m12;
        res.m22 = rotM.m22;
        res.m32 = 0;

        res.m03 = pos.x;
        res.m13 = -pos.y;
        res.m23 = pos.z;
        res.m33 = 1;

        return res;
    }

    public Matrix4x4 Matrix44FromQuat(Quaternion q)
    {
        Matrix4x4 m = Matrix4x4.identity;
        float q00 = 2.0f * q[0] * q[0];
        float q11 = 2.0f * q[1] * q[1];
        float q22 = 2.0f * q[2] * q[2];
        float q01 = 2.0f * q[0] * q[1];
        float q02 = 2.0f * q[0] * q[2];
        float q03 = 2.0f * q[0] * q[3];

        float q12 = 2.0f * q[1] * q[2];
        float q13 = 2.0f * q[1] * q[3];

        float q23 = 2.0f * q[2] * q[3];

        m.m00 = 1.0f - q11 - q22;
        m.m10 = q01 - q23;
        m.m20 = q02 + q13;

        m.m01 = q01 + q23;
        m.m11 = 1.0f - q22 - q00;
        m.m21 = q12 - q03;

        m.m02 = q02 - q13;
        m.m12 = q12 + q03;
        m.m22 = 1.0f - q11 - q00;

        return m;
    }

    public Quaternion ExtractRotation(Matrix4x4 matrix)
    {
        Vector3 forward;
        forward.x = matrix.m02;
        forward.y = matrix.m12;
        forward.z = matrix.m22;

        Vector3 upwards;
        upwards.x = matrix.m01;
        upwards.y = matrix.m11;
        upwards.z = matrix.m21;

        return Quaternion.LookRotation(forward, upwards);
    }

    public Vector3 ExtractPosition(Matrix4x4 matrix)
    {
        Vector3 position;
        position.x = matrix.m03;
        position.y = matrix.m13;
        position.z = matrix.m23;
        return position;
    }
}