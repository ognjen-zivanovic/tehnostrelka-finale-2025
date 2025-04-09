using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class RuntimeImageLibraryManager : MonoBehaviour
{
    public ARTrackedImageManager imageManager;
    void Start()
    {
    }


    public IEnumerator AddImageAtRuntime(Texture2D runtimeTexture, string textureName)
    {
        // Wait for the AR Session to initialize
        yield return new WaitUntil(() => imageManager.subsystem != null && imageManager.subsystem.running);

        if (imageManager.subsystem is XRImageTrackingSubsystem imageTrackingSubsystem)
        {
            var library = imageTrackingSubsystem.imageLibrary as MutableRuntimeReferenceImageLibrary;

            if (library == null)
            {
                Debug.LogError("The image library is not mutable. Ensure AR Foundation supports runtime libraries on your platform.");
                yield break;
            }

            var jobHandle = library.ScheduleAddImageWithValidationJob(
                runtimeTexture,
                textureName,
                0.2f
            );

            while (!jobHandle.jobHandle.IsCompleted)
                yield return null;

            jobHandle.jobHandle.Complete();

            Debug.Log("Image added to runtime library!");

            imageManager.enabled = false;
            imageManager.referenceLibrary = library;
            imageManager.enabled = true;
        }
        else
        {
            Debug.LogError("Image tracking subsystem not available or not running.");
        }
    }
}
