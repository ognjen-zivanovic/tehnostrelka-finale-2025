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

[System.Serializable]
public class PromptResponse
{
    public string Title;
    public string Author;
    public string BriefSummary;
    public string Genre;
}

public class GetCameraImage : MonoBehaviour
{
    [SerializeField] ARCameraManager cameraManager;
    [SerializeField] CropBoxController cropBoxController;

    Texture2D lastTexture;
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

        LogTextureInfo(lastTexture);

        await AskGPT();
    }

    void LogTextureInfo(Texture2D texture)
    {
        Debug.Log("Texture Name: " + texture.name);
        Debug.Log("Texture Width: " + texture.width);
        Debug.Log("Texture Height: " + texture.height);
        Debug.Log("Texture Format: " + texture.format);
        Debug.Log("Texture is Readable: " + texture.isReadable);
        Debug.Log("Texture Mipmap Enabled: " + (texture.mipmapCount > 1));
        Debug.Log("Texture Wrap Mode: " + texture.wrapMode);
        Debug.Log("Texture Filter Mode: " + texture.filterMode);
        Debug.Log("Texture Aniso Level: " + texture.anisoLevel);
        Debug.Log("Texture Texel Size: " + texture.texelSize);
    }
    System.Collections.IEnumerator ProcessImage(XRCpuImage image)
    {
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
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

        Debug.Log("TEXTURE WIDTH: " + width);
        Debug.Log("TEXTURE HEIGHT: " + height);

        Vector4 cropAmount = cropBoxController.GetCropAmounts();
        int cropX = (int)(cropAmount.z * width);
        int cropY = (int)(cropAmount.x * height);

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


        lastTexture = croppedTexture;



        yield return null;
    }

    public async Task AskGPT()
    {
        // asking gpt
        var api = new OpenAIClient();
        string promptStari = @"You will be presented with an image of a book cover.  
Your task is to extract the following information from the image if available and display it in **JSON format**.

If the title and/or author can be recognized from the image, feel free to use your knowledge to fill in the remaining information (such as a brief summary and genre).  
Do not omit any fields if there is a basis for making an assumption – even if the information is not directly on the image.

The output must be strictly the following JSON object, with no additional markings, text, or formatting:

{
  ""Title"": ""[Full title of the book]"",
  ""Author"": ""[Full name of the author]"",
  ""BriefSummary"": ""[Brief summary of the book in 2–4 sentences, based on the image, text, or known information about the book]"",
  ""Genre"": ""[Include if it can be reasonably inferred from the title, author, or general knowledge]""
}

If any field cannot be filled (neither from the image nor from known data), enter ""Unknown"".

IMPORTANT: Do not add markers like ```json or any introductory or accompanying text. The output must be **strictly a JSON object**.
";

        string prompt = @"Given the image of a book cover, extract or infer the following information and present it in this format:
Title: [Title of the book]
Author: [Name of the author]
Genre: [Primary genre(s) of the book]
Publication Year: [Year the book was first published]
Description: [A very short description of the book, based on knowledge or inference]
Similar Books: [List of similar books, either by the same author or others]
Notable Awards (if any): [List any major awards the book has won]

If the title and/or author can be recognized from the image, feel free to use your knowledge to fill in the remaining information. Use reasonable assumptions where necessary. Do not omit any fields if there is a basis for making an assumption – even if the information is not directly visible on the cover. Do not include any explanation or commentary – only provide the structured response as listed above.";
        var messages = new List<Message>
        {
            new Message(Role.System, "Vaš zadatak je da iz slike korice knjige izvučete specifične informacije i pružite ih u strukturiranom formatu."),
            new Message(Role.User, new List<Content>
            {
                prompt,
                //new ImageUrl("", ImageDetail.Low)
                lastTexture
            })
        };
        var chatRequest = new ChatRequest(messages, model: Model.GPT4o);
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

        // string fakeResponseString = @"
        //         {
        //   ""Title"": ""Dune"",
        //   ""Author"": ""Frank Herbert"",
        //   ""BriefSummary"": ""Dune is a science fiction novel set in the distant future amidst a huge interstellar empire. It revolves around the story of Paul Atreides and his noble family's control of the desert planet Arrakis, which is the universe's only source of 'spice', a powerful substance essential for space travel and longevity."",
        //   ""Genre"": ""Science Fiction""
        //   }
        // ";

        SetBookINfo.info = $"{response.FirstChoice.Message.Content}";
        Debug.Log(SetBookINfo.info);

        imageLibraryManager.StartCoroutine(imageLibraryManager.AddImageAtRuntime(lastTexture, "IMG_20394"));
    }
}
