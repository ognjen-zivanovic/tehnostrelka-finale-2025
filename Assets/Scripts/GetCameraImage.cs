using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using OpenAI;
using System.Collections.Generic;
using OpenAI.Chat;
using OpenAI.Models;
using System.Threading.Tasks;
using System.IO;
using TMPro;
using System;

public class GetCameraImage : MonoBehaviour
{
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] RawImage rawImage;
    [SerializeField] CropBoxController cropBoxController;

    Texture2D lastTexture;
    [SerializeField] TMP_Text infoText;

    RuntimeImageLibraryManager imageLibraryManager;

    void Update()
    {
        imageLibraryManager = gameObject.GetComponent<RuntimeImageLibraryManager>();
    }

    void Start()
    {

    }

    public async void TakeImage()
    {
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            // Example: Convert to Text ure2D
            StartCoroutine(ProcessImage(image));
        }

        //await AskGPT();
    }

    System.Collections.IEnumerator ProcessImage(XRCpuImage image)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
        };

        // Create buffer
        var rawTextureData = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
        image.Convert(conversionParams, rawTextureData);
        image.Dispose();

        // Create texture
        Texture2D texture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);
        texture.LoadRawTextureData(rawTextureData);
        texture.Apply();
        rawTextureData.Dispose();



        int width = texture.width;
        int height = texture.height;


        Vector4 cropAmount = cropBoxController.GetCropAmounts();
        int cropY = (int)(cropAmount.x * width);
        int cropX = (int)(cropAmount.z * height);

        int newWidth = width - 2 * cropX;
        int newHeight = height - 2 * cropY;


        // Get the pixels from the region
        Color[] pixels = texture.GetPixels(cropX, cropY, newWidth, newHeight);

        // Create the new texture
        Texture2D croppedTexture = new Texture2D(newWidth, newHeight, texture.format, false);
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();



        // byte[] pngData = croppedTexture.EncodeToPNG();
        // if (pngData != null)
        // {
        //     File.WriteAllBytes("uki.png", pngData);
        //     Debug.Log("Saved cropped texture to: " + "uki.png");
        // }
        // else
        // {
        //     Debug.LogError("Failed to encode texture to PNG.");
        // }


        imageLibraryManager.StartCoroutine(imageLibraryManager.AddImageAtRuntime(croppedTexture, "uki"));
        rawImage.texture = croppedTexture;
        lastTexture = croppedTexture;

        yield return null;
    }

    public async Task AskGPT()
    {
        // asking gpt
        var api = new OpenAIClient();
        var messages = new List<Message>
        {
            new Message(Role.System, "Vaš zadatak je da iz slike korice knjige izvučete specifične informacije i pružite ih u strukturiranom formatu."),
            new Message(Role.User, new List<Content>
            {
                @"Bićete prikazani sa slikom korice knjige.
Vaš zadatak je da iz slike izdvojite sledeće informacije ako su dostupne:

Naziv: [Puni naziv knjige]
Autor: [Puno ime autora]
Kratak sadržaj: [Kratak sadržaj knjige u 2–4 rečenice, zasnovano na slici ili prepoznatom sadržaju]
Žanr: [Opcionalno – uključite samo ako se može razumno naslutiti sa slike]
Ako bilo koje polje nije jasno dostupno ili se ne može razumno naslutiti, napišite Nepoznato za to polje.

Važno: Vaš odgovor mora biti u ovom tačnom formatu. Ne uključujte nikakva objašnjenja ili dodatni tekst.",
                //new ImageUrl("", ImageDetail.Low)
                new ImageUrl($"data:image/png;base64,{Convert.ToBase64String(lastTexture.EncodeToPNG())}", ImageDetail.Low)
            })
        };
        var chatRequest = new ChatRequest(messages, model: Model.GPT4oMini);
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

        string s = $"{response.FirstChoice.Message.Role}: {response.FirstChoice.Message.Content} | Finish Reason: {response.FirstChoice.FinishDetails}";
        Debug.Log(s);
        infoText.text = s;
    }
}
