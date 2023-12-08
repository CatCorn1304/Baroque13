using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class OilPaintingEffectPass : ScriptableRenderPass
{
    private RenderTargetIdentifier source;
    private RenderTargetIdentifier destination;

    private RenderTexture structureTensorTex;
    private RenderTexture kuwaharaFilterTex;
    private RenderTexture edgeFlowTex;

    private readonly Material structureTensorMaterial;
    private readonly Material kuwaharaFilterMaterial;
    private readonly Material lineIntegralConvolutionMaterial;

    private int kuwaharaFilterIterations = 1;

    public OilPaintingEffectPass(Material structureTensorMaterial,
                             Material kuwaharaFilterMaterial,
                             Material lineIntegralConvolutionMaterial)
{
    this.structureTensorMaterial = structureTensorMaterial;
    this.kuwaharaFilterMaterial = kuwaharaFilterMaterial;
    this.lineIntegralConvolutionMaterial = lineIntegralConvolutionMaterial;
}

public void Setup(OilPaintingEffect.Settings settings)
    {
        SetupKuwaharaFilter(settings.anisotropicKuwaharaFilterSettings);
    }
    private void SetupKuwaharaFilter(OilPaintingEffect.AnisotropicKuwaharaFilterSettings kuwaharaFilterSettings)
{
    kuwaharaFilterMaterial.SetInt("_FilterKernelSectors", kuwaharaFilterSettings.filterKernelSectors);
    kuwaharaFilterMaterial.SetTexture("_FilterKernelTex", kuwaharaFilterSettings.filterKernelTexture);
    kuwaharaFilterMaterial.SetFloat("_FilterRadius", kuwaharaFilterSettings.filterRadius);
    kuwaharaFilterMaterial.SetFloat("_FilterSharpness", kuwaharaFilterSettings.filterSharpness);
    kuwaharaFilterMaterial.SetFloat("_Eccentricity", kuwaharaFilterSettings.eccentricity);
    kuwaharaFilterIterations = kuwaharaFilterSettings.iterations;
    }

    private void SetupLineIntegralConvolution(OilPaintingEffect.EdgeFlowSettings edgeFlowSettings)
{
    lineIntegralConvolutionMaterial.SetTexture("_NoiseTex", edgeFlowSettings.noiseTexture);
    lineIntegralConvolutionMaterial.SetInt("_StreamLineLength", edgeFlowSettings.streamLineLength);
    lineIntegralConvolutionMaterial.SetFloat("_StreamKernelStrength", edgeFlowSettings.streamKernelStrength);
}
public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        blitTargetDescriptor.depthBufferBits = 0;

        var renderer = renderingData.cameraData.renderer;

        source = renderer.cameraColorTarget;
        destination = renderer.cameraColorTarget;

        structureTensorTex = RenderTexture.GetTemporary(blitTargetDescriptor.width, blitTargetDescriptor.height, 0, RenderTextureFormat.ARGBFloat);
        kuwaharaFilterTex = RenderTexture.GetTemporary(blitTargetDescriptor);
        edgeFlowTex = RenderTexture.GetTemporary(blitTargetDescriptor.width, blitTargetDescriptor.height, 0, RenderTextureFormat.RFloat);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("Oil Painting Effect");

        Blit(cmd, source, structureTensorTex, structureTensorMaterial, -1);

        kuwaharaFilterMaterial.SetTexture("_StructureTensorTex", structureTensorTex);

        Blit(cmd, source, kuwaharaFilterTex, kuwaharaFilterMaterial, -1);
        for (int i = 0; i < kuwaharaFilterIterations - 1; i++)
        {
            Blit(cmd, kuwaharaFilterTex, kuwaharaFilterTex, kuwaharaFilterMaterial, -1);
        }

        Blit(cmd, structureTensorTex, edgeFlowTex, lineIntegralConvolutionMaterial, -1);
                
        Blit(cmd, edgeFlowTex, destination);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    public override void FrameCleanup(CommandBuffer cmd)
    {
        RenderTexture.ReleaseTemporary(structureTensorTex);
        RenderTexture.ReleaseTemporary(kuwaharaFilterTex);
        RenderTexture.ReleaseTemporary(edgeFlowTex);
    }
}
