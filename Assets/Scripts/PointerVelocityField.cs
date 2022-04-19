using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointerVelocityField : MonoBehaviour
{
    [SerializeField] private CustomRenderTexture customRenderTexture;
    [SerializeField] private float pointerRadius = 0.33f;

    private static readonly int GlobalPointerVelocityField = Shader.PropertyToID("_GlobalPointerVelocityField");
    private static readonly int PointerLocalTexturePosition = Shader.PropertyToID("_PointerLocalTexturePosition");
    private static readonly int PointerVelocity = Shader.PropertyToID("_PointerVelocity");
    private static readonly int PointerRadius = Shader.PropertyToID("_PointerRadius");

    private void Start()
    {
        Shader.SetGlobalTexture(GlobalPointerVelocityField, customRenderTexture);
    }

    private void Update()
    {
        var pointer = Pointer.current;

        var material = customRenderTexture.material;
        
        var pointerVelocity = pointer.delta.ReadValue().normalized;
        var pointerScreenPosition = pointer.position.ReadValue();
        Vector2 pointerLocalTexturePosition = math.remap(Vector2.zero, new Vector2(Screen.width, Screen.height),
            Vector2.zero, Vector2.one, pointerScreenPosition);
        
        material.SetVector(PointerLocalTexturePosition, pointerLocalTexturePosition);
        material.SetVector(PointerVelocity, pointerVelocity);
        material.SetFloat(PointerRadius, pointerRadius);
            
        customRenderTexture.Update();
    }
}