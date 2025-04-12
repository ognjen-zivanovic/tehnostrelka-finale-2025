using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections;
using Unity.Collections;

public class CpuImageDisplay : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public Shader blitShader; // Assign a passthrough + matrix shader
    public RawImage rawImage; // UI element to display the image

    public void RequestCameraTexture(Action<Texture2D> onComplete)
    {
        Debug.Log("CONVERSION: Attempting to acquire latest CPU image.");

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            Debug.LogWarning("CONVERSION: Failed to acquire CPU image.");
            return;
        }

        Debug.Log("CONVERSION: Successfully acquired CPU image.");
        StartCoroutine(ProcessImage(cpuImage, onComplete));
    }


    public void TestTestText()
    {
        Debug.Log("CONVERSION: Running TestTestText.");

        Texture2D testTexture = new Texture2D(1, 1);

        // Start a coroutine to delay the execution of RequestCameraTexture
        StartCoroutine(DelayedRequest(testTexture));
    }

    private IEnumerator DelayedRequest(Texture2D testTexture)
    {
        // Wait for the next frame
        yield return null;

        // Now request the camera texture
        RequestCameraTexture((texture) =>
        {
            testTexture = texture;
            Debug.Log("CONVERSION: Test texture name: " + testTexture.name);
            rawImage.texture = testTexture;
        });
    }

    IEnumerator ProcessImage(XRCpuImage cpuImage, Action<Texture2D> onComplete)
    {
        Debug.Log("CONVERSION: Starting image processing coroutine.");

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
            outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        Debug.Log("CONVERSION: Conversion parameters set.");

        var rawTextureData = new NativeArray<byte>(cpuImage.GetConvertedDataSize(conversionParams), Allocator.Temp);
        Debug.Log("CONVERSION: Allocated raw texture buffer.");

        cpuImage.Convert(conversionParams, rawTextureData);
        Debug.Log("CONVERSION: CPU image converted to RGBA32.");

        cpuImage.Dispose();
        Debug.Log("CONVERSION: CPU image disposed.");

        var tempTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
        tempTexture.LoadRawTextureData(rawTextureData);
        tempTexture.Apply();
        Debug.Log("CONVERSION: Temporary Texture2D created and applied.");

        rawTextureData.Dispose();
        Debug.Log("CONVERSION: Raw texture data disposed.");

        Matrix4x4 displayMatrix = Matrix4x4.identity;
        if (cameraManager.subsystem == null)
        {
            Debug.LogError("CONVERSION: AR Camera subsystem is not available.");
        }

        if (cameraManager.subsystem != null && cameraManager.subsystem.TryGetLatestFrame(new XRCameraParams(), out XRCameraFrame frame))
        {
            if (frame.hasDisplayMatrix)
            {
                displayMatrix = frame.displayMatrix;
                Debug.Log("CONVERSION: Display matrix acquired from camera frame.");
            }
            else
            {
                Debug.Log("CONVERSION: Camera frame does not contain a display matrix.");
            }
        }
        else
        {
            Debug.Log("CONVERSION: Failed to get latest camera frame.");
        }

        Material mat = new Material(blitShader);
        mat.SetMatrix("_DisplayMatrix", displayMatrix);
        Debug.Log("CONVERSION: Material created and display matrix set.");

        RenderTexture rt = new RenderTexture(tempTexture.width, tempTexture.height, 0);
        Graphics.Blit(tempTexture, rt, mat);
        Debug.Log("CONVERSION: Blitted temporary texture to render texture using display matrix.");

        Texture2D output = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        RenderTexture.active = rt;
        output.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        output.Apply();
        Debug.Log("CONVERSION: Output texture created from render texture.");

        RenderTexture.active = null;
        Destroy(rt);
        Destroy(tempTexture);
        Destroy(mat);
        Debug.Log("CONVERSION: Cleaned up temporary resources.");

        onComplete?.Invoke(output);
        Debug.Log("CONVERSION: Callback invoked with final Texture2D.");

        yield return null;
    }
}
