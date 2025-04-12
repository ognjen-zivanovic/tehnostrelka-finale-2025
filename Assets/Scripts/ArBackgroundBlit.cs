using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ArBackgroundBlit : MonoBehaviour
{
    public ARCameraBackground m_ARCameraBackground;

    public RenderTexture m_RenderTexture;

    public Image m_Image;

    private Texture2D destinationTexture;

    public void BlitARBackgroundToTexture()
    {
        var commandBuffer = new CommandBuffer();
        commandBuffer.name = "AR Camera Background Blit Pass";

        var texture = !m_ARCameraBackground.material.HasProperty("_MainTex") ?
            null : m_ARCameraBackground.material.GetTexture("_MainTex");

        var colorBuffer = Graphics.activeColorBuffer;
        var depthBuffer = Graphics.activeDepthBuffer;

        var width = Screen.width;
        var height = Screen.height;

        m_RenderTexture.Release();
        m_RenderTexture.width = width;
        m_RenderTexture.height = height;
        m_RenderTexture.Create();

        Graphics.SetRenderTarget(m_RenderTexture);

        commandBuffer.ClearRenderTarget(true, false, Color.clear);

        Debug.Log(texture);
        commandBuffer.Blit(
            texture,
            BuiltinRenderTextureType.CurrentActive,
            m_ARCameraBackground.material);

        Graphics.ExecuteCommandBuffer(commandBuffer);

        Graphics.SetRenderTarget(colorBuffer, depthBuffer);

        destinationTexture = new Texture2D(m_RenderTexture.width, m_RenderTexture.height, TextureFormat.RGBA32, false);
        Graphics.CopyTexture(m_RenderTexture, destinationTexture);

        m_Image.sprite = Sprite.Create(destinationTexture, new Rect(0, 0, destinationTexture.width, destinationTexture.height), new Vector2(0.5f, 0.5f));
        m_Image.preserveAspect = true;

        commandBuffer.Release();
    }


}
