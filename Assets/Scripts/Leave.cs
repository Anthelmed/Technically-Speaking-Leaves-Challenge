using System;
using System.Collections.Generic;
using System.Linq;
using JPBotelho;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Leave : MonoBehaviour
{
	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private MeshFilter meshFilter;
	
	private float _size;
	private AnimationCurve _shapeCurve;
	private int _resolution;

	private List<Vector3> _controlPoints = new();
	private CatmullRom _catmullRom;
	
	private static readonly int Index = Shader.PropertyToID("_Index");
	
	public void Initialize(float index, float size, AnimationCurve shapeCurve, int resolution)
	{
		_size = size;
		_shapeCurve = shapeCurve;
		_resolution = resolution;
		
		var material = meshRenderer.material;
		material.SetFloat(Index, index);

		meshRenderer.sharedMaterial = material;
	}
	
	public void SetSize(float size)
	{
		_size = size;
	}

	private void OnValidate()
	{
		meshRenderer = GetComponent<MeshRenderer>();
		meshFilter = GetComponent<MeshFilter>();
	}

	private void OnDisable()
	{
		ClearControlPoints();
		meshFilter.sharedMesh = null;
	}

	public void AddControlPoint(Vector3 position)
	{
		_controlPoints.Add(position);
	}

	public void UpdatePath()
	{
		var points = _controlPoints.ToArray();

		if (points.Length <= 2 || _resolution <= 2) return;

		_catmullRom ??= new CatmullRom(points, _resolution, false);
		_catmullRom.Update(points);
	}

	public void UpdateMesh()
	{
		var points = _controlPoints.ToArray();

		if (points.Length <= 2 || _resolution <= 2) return;
		
		meshFilter.sharedMesh = GenerateMesh();
	}

	public void ClearControlPoints()
	{
		_controlPoints.Clear();
	}
	
	public void DrawSpline(Color color)
	{
		_catmullRom?.DrawSpline(color);
	}

	public void DrawTangents(Color color)
	{
		_catmullRom?.DrawTangents(1, color);
	}

	public Mesh GenerateMesh()
	{
		var points = _catmullRom.GetPoints();
		
		var vertices = new Vector3[points.Length * 3];
		var uvs = new Vector2[points.Length * 3];
		var triangles = new int[4 * (points.Length - 1) * 3];

		var vertexIndex = 0;
		var triangleIndex = 0;

		for (var index = 0; index < points.Length; index++)
		{
			var pathPercent = index / (float)(points.Length - 1);
			
			var point = points[index];
			
			Vector3 right =  math.normalize(math.cross(new float3(point.tangent.x, point.tangent.y, 0), new float3(0,0,-1)));
			
			var shape = _shapeCurve.Evaluate(pathPercent);
			
			vertices[vertexIndex] = point.position + right * (shape * _size) * 0.25f;
			vertices[vertexIndex + 1] = point.position;
			vertices[vertexIndex + 2] = point.position - right * (shape * _size) * 0.25f;
			
			uvs[vertexIndex] = new Vector2(pathPercent, 0);
			uvs[vertexIndex + 1] = new Vector2(pathPercent, 0.5f);
			uvs[vertexIndex + 2] = new Vector2(pathPercent, 1);

			if (index < points.Length - 1)
			{
				triangles[triangleIndex] = vertexIndex;
				triangles[triangleIndex + 1] = vertexIndex + 3;
				triangles[triangleIndex + 2] = vertexIndex + 1;

				triangles[triangleIndex + 3] = vertexIndex + 1;
				triangles[triangleIndex + 4] = vertexIndex + 3;
				triangles[triangleIndex + 5] = vertexIndex + 4;
				
				triangles[triangleIndex + 6] = vertexIndex + 1;
				triangles[triangleIndex + 7] = vertexIndex + 4;
				triangles[triangleIndex + 8] = vertexIndex + 2;
				
				triangles[triangleIndex + 9] = vertexIndex + 2;
				triangles[triangleIndex + 10] = vertexIndex + 4;
				triangles[triangleIndex + 11] = vertexIndex + 5;
			}

			vertexIndex += 3;
			triangleIndex += 12;
		}

		var mesh = new Mesh
		{
			vertices = vertices,
			triangles = triangles,
			uv = uvs
		};

		return mesh;
	}
}