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
using System.Text.RegularExpressions;

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

    UIListManager uIListManager;

    void Update()
    {
        imageLibraryManager = gameObject.GetComponent<RuntimeImageLibraryManager>();
    }

    void Start()
    {
        uIListManager = GameObject.FindWithTag("UIManager").GetComponent<UIListManager>();
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

        string prompt = @"Given the image of a book cover, extract or infer the following information and present it in this format:

        You must make the some of the text bold using the html bold tags. Add a newline between every category.

<b>Title</b>: [Title of the book]
<b>Author</b>: [Name of the author]
<b>Genre</b>: [Primary genre(s) of the book]
<b>Publication Year</b>: [Year the book was first published]
<b>Description</b>: [A very short description of the book, based on knowledge or inference]
<b>Similar Books</b>: [List of similar books, either by the same author or others]
<b>Notable Awards (if any)</b>: [List any major awards the book has won]

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

        string responseString = $"{response.FirstChoice.Message.Content}";
        SetBookINfo.info = responseString;
        Debug.Log(SetBookINfo.info);

        imageLibraryManager.StartCoroutine(imageLibraryManager.AddImageAtRuntime(lastTexture, "IMG_20394"));

        string patternAuthor = @"<b>Author</b>:\s*(.*?)\n";
        Match matchAuthor = Regex.Match(responseString, patternAuthor);
        if (matchAuthor.Success)
        {
            string author = matchAuthor.Groups[1].Value;
            Console.WriteLine("Author found: " + author);
        }
        else
        {
            Console.WriteLine("Author not found.");
        }

        string patternTitle = @"<b>Title</b>:\s*(.*?)\n";
        Match matchTitle = Regex.Match(responseString, patternTitle);
        if (matchTitle.Success)
        {
            string title = matchTitle.Groups[1].Value;
            Console.WriteLine("Title found: " + title);
        }
        else
        {
            Console.WriteLine("Title not found.");
        }

        uIListManager.AddItemToList(matchTitle.Groups[1].Value, matchAuthor.Groups[1].Value, Sprite.Create(lastTexture, new Rect(0, 0, lastTexture.width, lastTexture.height), new Vector2(0.5f, 0.5f)));
    }
}
