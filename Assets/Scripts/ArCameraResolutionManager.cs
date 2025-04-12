using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ArCameraResolutionManager : MonoBehaviour
{
    NativeArray<XRCameraConfiguration> configurations = new NativeArray<XRCameraConfiguration>();
    // Start is called before the first frame update

    private ARCameraManager arCameraManager;
    void Start()
    {
        ARCameraManager am = GetComponent<ARCameraManager>();
        configurations = am.GetConfigurations(Allocator.Temp);
        Debug.Log($"Camera Configuration number: {configurations.Length}");
        foreach (var config in configurations)
        {
            Debug.Log($"Camera Configuration: {config.resolution.x}x{config.resolution.y} @ {config.framerate} fps");
        }
        //am.subsystem.currentConfiguration = configurations[configurations.Length - 1];

        arCameraManager = GetComponent<ARCameraManager>();

        // Check if ARCameraManager exists
        if (arCameraManager != null)
        {
            SetCameraResolution();
        }
    }

    void SetCameraResolution()
    {
        var cameraSubsystem = arCameraManager.subsystem;
        var cameraConfigurations = arCameraManager.GetConfigurations(Allocator.Temp);

        XRCameraConfiguration? highestResolution = null;
        foreach (var config in cameraConfigurations)
        {
            if (highestResolution == null ||
                (config.resolution.x * config.resolution.y) > (highestResolution?.resolution.x * highestResolution?.resolution.y))
            {
                highestResolution = config;
            }
        }

        if (highestResolution != null)
        {
            cameraSubsystem.currentConfiguration = highestResolution;
        }

        cameraConfigurations.Dispose();
    }

    void Update()
    {

    }
}
