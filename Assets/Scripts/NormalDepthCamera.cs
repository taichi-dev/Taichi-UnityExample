using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalDepthCamera : MonoBehaviour {
    private Camera _MainCamera;

    public Material BlitMaterial;
    public Material DownsampleMaterial;
    public Material UpsampleMaterial;
    public Material LiquidMask2NormalMaterial;
    public Material NormalUpsampleMaterial;

    public RenderTexture SceneNormalDepth;

    private RenderTexture LiquidMaskDownsampled1;
    private RenderTexture LiquidMaskDownsampled2;
    private RenderTexture LiquidMaskUpsampled1;
    private RenderTexture LiquidMaskUpsampled2;

    private RenderTexture SceneNormalDownsampled2;
    private RenderTexture SceneNormalUpsampled2;

    public Color LiquidColor;
    public Color LiquidSpecularColor;

    private void Start() {
        _MainCamera = GetComponent<Camera>();
        if (Camera.main != _MainCamera) {
            throw new ArgumentException("This component should be added to the main camera");
        }

        LiquidMaskDownsampled1 = new RenderTexture(
            SceneNormalDepth.width / 4,
            SceneNormalDepth.height / 4,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
        LiquidMaskDownsampled2 = new RenderTexture(
            SceneNormalDepth.width / 8,
            SceneNormalDepth.height / 8,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
        LiquidMaskUpsampled1 = new RenderTexture(
            SceneNormalDepth.width / 4,
            SceneNormalDepth.height / 4,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);
        LiquidMaskUpsampled2 = new RenderTexture(
            SceneNormalDepth.width,
            SceneNormalDepth.height,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat);

        SceneNormalDownsampled2 = new RenderTexture(
            _MainCamera.pixelWidth / 2,
            _MainCamera.pixelHeight / 2,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
        SceneNormalUpsampled2 = new RenderTexture(
            _MainCamera.pixelWidth,
            _MainCamera.pixelHeight,
            1,
            UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (Camera.current == Camera.main) {
            DownsampleMaterial.SetVector("_MainTexInvSize", new Vector2(1.0f / SceneNormalDepth.width, 1.0f / SceneNormalDepth.height));
            Graphics.Blit(SceneNormalDepth, LiquidMaskDownsampled1, DownsampleMaterial);

            DownsampleMaterial.SetVector("_MainTexInvSize", new Vector2(1.0f / LiquidMaskDownsampled1.width, 1.0f / LiquidMaskDownsampled1.height));
            Graphics.Blit(LiquidMaskDownsampled1, LiquidMaskDownsampled2, DownsampleMaterial);

            UpsampleMaterial.SetTexture("_SameSizeTex", LiquidMaskDownsampled1);
            UpsampleMaterial.SetVector("_MainTexInvSize", new Vector2(1.0f / LiquidMaskDownsampled1.width, 1.0f / LiquidMaskDownsampled1.height));
            Graphics.Blit(LiquidMaskDownsampled2, LiquidMaskUpsampled1, UpsampleMaterial);

            UpsampleMaterial.SetTexture("_SameSizeTex", SceneNormalDepth);
            UpsampleMaterial.SetVector("_MainTexInvSize", new Vector2(1.0f / SceneNormalDepth.width, 1.0f / SceneNormalDepth.height));
            Graphics.Blit(LiquidMaskDownsampled1, LiquidMaskUpsampled2, UpsampleMaterial);

            // Liquid mask to normal.
            LiquidMask2NormalMaterial.SetTexture("_LiquidMask", LiquidMaskUpsampled2);
            LiquidMask2NormalMaterial.SetVector("_LiquidMaskInvSize", new Vector2(1.0f / LiquidMaskUpsampled2.width, 1.0f / LiquidMaskUpsampled2.height));
            Graphics.Blit(LiquidMaskUpsampled2, SceneNormalDownsampled2, LiquidMask2NormalMaterial);

            // Smooth out the screen space normal map.
            NormalUpsampleMaterial.SetVector("_MainTexInvSize", new Vector2(1.0f / SceneNormalDownsampled2.width, 1.0f / SceneNormalDownsampled2.height));
            NormalUpsampleMaterial.SetTexture("_SameSizeTex", LiquidMaskDownsampled1);
            Graphics.Blit(SceneNormalDownsampled2, SceneNormalUpsampled2, NormalUpsampleMaterial);

            BlitMaterial.SetTexture("_SceneColor", source);
            BlitMaterial.SetVector("_SceneColorInvSize", new Vector2(1.0f / source.width, 1.0f / source.height));
            BlitMaterial.SetTexture("_SceneNormal", SceneNormalUpsampled2);
            BlitMaterial.SetVector("_SceneNormalInvSize", new Vector2(1.0f / SceneNormalUpsampled2.width, 1.0f / SceneNormalUpsampled2.height));
            BlitMaterial.SetVector("_LiquidColor", LiquidColor);
            BlitMaterial.SetVector("_LiquidSpecularColor", LiquidSpecularColor);
            Graphics.Blit(source, destination, BlitMaterial);
        } else {
            Graphics.Blit(source, destination);
        }
    }
}
