using System;
using System.Collections.Generic;
using System.Linq;
using JPBotelho;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class Stem : MonoBehaviour
{
	[SerializeField] private MeshRenderer meshRenderer;
	[SerializeField] private MeshFilter meshFilter;
	
	private float _size;
	private Vector2 _minMaxThickness;
	private AnimationCurve _thicknessCurve;
	private int _resolution;

	private List<Vector3> _controlPoints = new();
	private CatmullRom _catmullRom;

	private float _bendMultiplier;
	private float _rotationMultiplier;
	private float _sizeMultiplier;
	private float _lengthMultiplier;
	
	public float BendMultiplier => _bendMultiplier;
	public float RotationMultiplier => _rotationMultiplier;
	public float SizeMultiplier => _sizeMultiplier;
	public float LengthMultiplier => _lengthMultiplier;

	public Vector3 LastControlPoint => _controlPoints.Last();
	public List<Vector3> ControlPoints => _controlPoints;
	public CatmullRom.CatmullRomPoint LastCatmullRomPoint => GetCatmullRomPoints().Last();
	
	private static readonly int Index = Shader.PropertyToID("_Index");
	private static readonly int Segments = Shader.PropertyToID("_Segments");

	public void Initialize(float index, float size, Vector2 minMaxThickness, AnimationCurve thicknessCurve, int resolution, float bendMultiplier = 1f, float rotationMultiplier = 1f, float sizeMultiplier = 1f, float lengthMultiplier = 1f)
	{
		_size = size;
		_minMaxThickness = minMaxThickness;
		_thicknessCurve = thicknessCurve;
		_resolution = resolution;
		
		_bendMultiplier = bendMultiplier;
		_rotationMultiplier = rotationMultiplier;
		_sizeMultiplier = sizeMultiplier;
		_lengthMultiplier = lengthMultiplier;
		
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
		ClearMesh();
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
		
		var catmullRomPoints = _catmullRom.GetPoints();
		
		meshRenderer.sharedMaterial.SetFloat(Segments, catmullRomPoints.Length);
		meshFilter.sharedMesh = GenerateMesh();
	}

	public void ClearControlPoints()
	{
		_controlPoints.Clear();
	}

	public void ClearMesh()
	{
		meshFilter.sharedMesh = null;
	}

	public CatmullRom.CatmullRomPoint[] GetCatmullRomPoints(int resolution = 2)
	{
		_catmullRom.Update(resolution, false);
		var points = _catmullRom.GetPoints();
		_catmullRom.Update(_resolution, false);

		return points;
	}

	public void DrawSpline(Color color)
	{
		_catmullRom?.DrawSpline(color);
	}

	public void DrawTangents(Color color)
	{
		_catmullRom?.DrawTangents(1, color);
	}
	
	public void DrawNormals(Color color)
	{
		_catmullRom?.DrawNormals(1, color);
	}

	public Mesh GenerateMesh()
	{
		var points = _catmullRom.GetPoints();

		var vertices = new Vector3[points.Length * 2];
		var uvs = new Vector2[points.Length * 2];
		var triangles = new int[2 * (points.Length - 1) * 3];

		var vertexIndex = 0;
		var triangleIndex = 0;

		for (var index = 0; index < points.Length; index++)
		{
			var pathPercent = index / (float)(points.Length - 1);
			
			var point = points[index];
			var thickness = math.lerp(_minMaxThickness.x, _minMaxThickness.y, _thicknessCurve.Evaluate(pathPercent));
			
			Vector3 right =  math.normalize(math.cross(new float3(point.tangent.x, point.tangent.y, 0), new float3(0,0,-1)));

			vertices[vertexIndex] = point.position + right * (thickness * _size) * 0.25f;
			vertices[vertexIndex + 1] = point.position - right * (thickness * _size) * 0.25f;
			
			uvs[vertexIndex] = new Vector2(pathPercent, 0);
			uvs[vertexIndex + 1] = new Vector2(pathPercent, 1);

			if (index < points.Length - 1)
			{
				triangles[triangleIndex] = vertexIndex;
				triangles[triangleIndex + 1] = vertexIndex + 2;
				triangles[triangleIndex + 2] = vertexIndex + 1;

				triangles[triangleIndex + 3] = vertexIndex + 1;
				triangles[triangleIndex + 4] = vertexIndex + 2;
				triangles[triangleIndex + 5] = vertexIndex + 3;
			}

			vertexIndex += 2;
			triangleIndex += 6;
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