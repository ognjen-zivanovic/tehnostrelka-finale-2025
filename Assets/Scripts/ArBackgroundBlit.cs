using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering;

public class ArBackgroundBlit : MonoBehaviour
{
    public ARCameraBackground m_ARCameraBackground;

    public RenderTexture m_RenderTexture;


    public void BlitARBackgroundToTexture()
    {
        var commandBuffer = new CommandBuffer();
        commandBuffer.name = "AR Camera Background Blit Pass";

        var texture = !m_ARCameraBackground.material.HasProperty("_MainTex") ?
            null : m_ARCameraBackground.material.GetTexture("_MainTex");

        var colorBuffer = Graphics.activeColorBuffer;
        var depthBuffer = Graphics.activeDepthBuffer;

        Graphics.SetRenderTarget(m_RenderTexture);

        commandBuffer.ClearRenderTarget(true, false, Color.clear);

        commandBuffer.Blit(
            texture,
            BuiltinRenderTextureType.CurrentActive,
            m_ARCameraBackground.material);

        Graphics.ExecuteCommandBuffer(commandBuffer);

        Graphics.SetRenderTarget(colorBuffer, depthBuffer);

        commandBuffer.Release();
    }


}
