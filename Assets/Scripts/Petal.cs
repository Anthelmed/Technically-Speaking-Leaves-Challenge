using System;
using System.Collections.Generic;
using UnityEngine;
using CatmullRom = JPBotelho.CatmullRom;

public class Petal : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color color;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private PolygonCollider2D polygonCollider2D;

    [Header("Path")]
    [SerializeField] private Transform[] controlPoints;
    [SerializeField] private int resolution = 24;
    
    private CatmullRom _catmullRom;
    
    private static readonly int Color = Shader.PropertyToID("_Color");
    
    private void Start()
    {
        var points = ControlPoints().ToArray();
        
        _catmullRom ??= new CatmullRom(points, resolution, true);
        _catmullRom.Update(points);
        
        UpdateMesh();
    }
    
    [ContextMenu("UpdateMesh")]
    private void UpdateMesh()
    {
        var points = PathPoints();

        polygonCollider2D.points = points.ToArray();
        
        var mesh = polygonCollider2D.CreateMesh(true, true);

        meshRenderer.sharedMaterial.SetColor(Color, color);
        meshFilter.sharedMesh = mesh;
    }

    private List<Vector3> ControlPoints()
    {
        var points = new List<Vector3>();

        foreach (var controlPoint in controlPoints)
        {
            points.Add(controlPoint.position);
        }

        return points;
    }
    
    private List<Vector2> PathPoints()
    {
        return _catmullRom.GetSimplifiedPoints();
    }
    
    private void OnDrawGizmosSelected()
    {
        if (_catmullRom == null) return;
        
        var points = PathPoints();

        Gizmos.color = color;
        
        foreach (var point in points)
        {
            Gizmos.DrawSphere(point, 0.05f);
        }
        
        _catmullRom.DrawSpline(color);
    }
}
