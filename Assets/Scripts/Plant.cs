using System;
using System.Collections.Generic;
using System.Linq;
using JPBotelho;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = Unity.Mathematics.Random;

public class Plant : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Stem stemPrefab;
    [SerializeField] private Leave leavePrefab;

    [Header("Path")]
    [SerializeField] private float maxDistanceBetweenPoint = 0.5f;
    [SerializeField] private float globalNoiseMultiplier = 0.5f;
    [SerializeField] private int steamPathResolution = 6;
    [SerializeField] private int leavePathResolution = 8;
    
    [Header("Main Stem")]
    [SerializeField] private Vector2 minMaxMainStemThickness;
    [SerializeField] private AnimationCurve mainStemThicknessCurve;
    [SerializeField] private Vector2 minMaxSecondaryStemSpacing;
    [SerializeField] private AnimationCurve secondaryStemSpacingCurve;
    
    [Header("Secondary Stem")]
    [SerializeField] private Vector2 minMaxSecondaryStemSize;
    [SerializeField] private AnimationCurve secondaryStemSizeCurve;
    [SerializeField] private Vector2 minMaxSecondaryStemThickness;
    [SerializeField] private AnimationCurve secondaryStemThicknessCurve;
    [SerializeField] private Vector2 minMaxSecondaryStemLength;
    [SerializeField] private AnimationCurve secondaryStemLengthCurve;
    [SerializeField] private Vector2 minMaxSecondaryStemBend;
    [SerializeField] private AnimationCurve secondaryStemBendCurve;
    [SerializeField] private Vector2 minMaxSecondaryStemRotation;
    [SerializeField] private AnimationCurve secondaryStemRotationCurve;
    
    [Header("Leave")]
    [SerializeField] private Vector2 minMaxLeaveSize;
    [SerializeField] private AnimationCurve leaveSizeCurve;
    [SerializeField] private Vector2 minMaxLeaveLength;
    [SerializeField] private AnimationCurve leaveLengthCurve;
    [SerializeField] private Vector2 minMaxLeaveBend;
    [SerializeField] private AnimationCurve leaveBendCurve;
    [SerializeField] private AnimationCurve leaveShapeCurve;
    [SerializeField] private Vector2 minMaxLeaveRotation;
    [SerializeField] private AnimationCurve leaveRotationCurve;
    
    ComponentPool<Stem> _stemsPool;
    ComponentPool<Leave> _leavesPool;

    private int _secondaryStemStartIndex = 9;
    
    private Stem _mainStem;
    private List<Stem> _secondaryStems = new ();
    private List<Leave> _leaves = new ();
    
    private bool _isDragging = false;

    private Camera _camera;

    private Random _random;
    
    private float RandomMultiplier =>  1 + (_random.NextFloat(-1, 1) * globalNoiseMultiplier);
    
    private bool IsPointerOverGameWindow => !(0 > Pointer.current.position.ReadValue().x || 
                                            0 > Pointer.current.position.ReadValue().y || 
                                            Screen.width < Pointer.current.position.ReadValue().x || 
                                            Screen.height < Pointer.current.position.ReadValue().y);

    private void Start()
    {
        _camera = Camera.main;
        
        _random = new Random((uint)UnityEngine.Random.Range(1, 100000));
        
        _stemsPool = new ComponentPool<Stem>(transform, stemPrefab, true, 32, 256);
        _leavesPool = new ComponentPool<Leave>(transform, leavePrefab, true, 32, 256);
        
        InstantiateMainStem();
    }

    private void Update()
    {
        if (!IsPointerOverGameWindow)
        {
            _isDragging = false;
            return;
        }
        
        var pointer = Pointer.current;
        
        if (pointer.press.wasPressedThisFrame)
            StartDragging();
        
        if (pointer.press.wasReleasedThisFrame)
            EndDragging();
    }

    private void FixedUpdate()
    {
        DrawMainStem();
        
        if (_mainStem.ControlPoints.Count <= 2) return;
        
        UpdateSecondaryStems();
        UpdateLeaves();
    }

    private void InstantiateMainStem()
    {
        _mainStem = Instantiate(stemPrefab, new Vector3(0, 0, -0.03f), quaternion.identity);
        _mainStem.Initialize(0, 1, minMaxMainStemThickness, mainStemThicknessCurve, steamPathResolution);
    }
    
    private void StartDragging()
    {
        _isDragging = true;
        
        _mainStem.ClearControlPoints();
        _mainStem.ClearMesh();
        
        ReleaseSecondaryStems();
        ReleaseLeaves();

        var startPosition = GetPointAtPointerPosition();
        
        _mainStem.AddControlPoint(startPosition);
    }

    private void EndDragging()
    {
        _isDragging = false;
    }

    private void DrawMainStem()
    {
        if (!_isDragging) return;

        var lastControlPoint = _mainStem.LastControlPoint;
        var currentPosition = GetPointAtPointerPosition();
        
        if (Vector3.Distance(lastControlPoint, currentPosition) < maxDistanceBetweenPoint) return;
        
        _mainStem.AddControlPoint(currentPosition);
        _mainStem.UpdatePath();
        _mainStem.UpdateMesh();
    }

    private void UpdateSecondaryStems()
    {
        if (!_isDragging) return;
        
        var catmullRomPoints = _mainStem.GetCatmullRomPoints(3);
        var catmullRomPointIndex = _secondaryStemStartIndex;

        var stemIndex = 0;
        
        while (catmullRomPointIndex < catmullRomPoints.Length - 1)
        {
            if (_secondaryStems.Count <= stemIndex)
                InstantiateSecondaryStem(stemIndex);
            
            var catmullRomPoint = catmullRomPoints[catmullRomPointIndex];
            
            UpdateSecondaryStemPath(stemIndex, catmullRomPoint);
            
            catmullRomPointIndex += (int)math.lerp(minMaxSecondaryStemSpacing.x, minMaxSecondaryStemSpacing.y,
                secondaryStemSpacingCurve.Evaluate(stemIndex / (float)_secondaryStems.Count));

            stemIndex++;
        }
    }
    
    private void UpdateLeaves()
    {
        if (!_isDragging) return;

        var catmullRomPoints = _mainStem.GetCatmullRomPoints(3);
        var catmullRomPointIndex = _secondaryStemStartIndex;

        var leafIndex = 0;
        
        while (catmullRomPointIndex < catmullRomPoints.Length - 1)
        {
            if (_leaves.Count <= leafIndex)
                InstantiateLeaf(leafIndex);
            
            var catmullRomPoint = catmullRomPoints[catmullRomPointIndex];
            
            UpdateLeafPath(leafIndex, catmullRomPoint);
            
            catmullRomPointIndex += (int)math.lerp(minMaxSecondaryStemSpacing.x, minMaxSecondaryStemSpacing.y,
                secondaryStemSpacingCurve.Evaluate(leafIndex / (float)_leaves.Count));

            leafIndex++;
        }
    }

    private void InstantiateSecondaryStem(int index)
    {
        var stem = _stemsPool.Value.Get();
        stem.transform.position = new Vector3(0, 0, -(0.04f + index / 100f));
        
        stem.Initialize(index, 0, minMaxSecondaryStemThickness, secondaryStemThicknessCurve, steamPathResolution, RandomMultiplier, RandomMultiplier, RandomMultiplier, RandomMultiplier);
        
        _secondaryStems.Add(stem);
    }

    private void UpdateSecondaryStemPath(int index, CatmullRom.CatmullRomPoint catmullRomPoint)
    {
        var stem = _secondaryStems.ElementAt(index);
        
        var direction = (index % 2) == 0 ? -1 : 1;
        
        var pathPercent = index / (float)_secondaryStems.Count;
        
        var bend = math.lerp(minMaxSecondaryStemBend.x, minMaxSecondaryStemBend.y,
            1 - secondaryStemBendCurve.Evaluate(pathPercent)) * stem.BendMultiplier;
        
        var rotation = math.lerp(minMaxSecondaryStemRotation.x, minMaxSecondaryStemRotation.y,
            secondaryStemRotationCurve.Evaluate(pathPercent)) * stem.RotationMultiplier;
        
        Vector3 right =  math.normalize(math.cross(new float3(catmullRomPoint.tangent.x, catmullRomPoint.tangent.y, 0), new float3(0,0,-1)));
        right = Quaternion.AngleAxis(rotation * direction, -Vector3.forward) * right;
        var up = catmullRomPoint.tangent * bend;

        var size = math.lerp(minMaxSecondaryStemSize.x, minMaxSecondaryStemSize.y,
            secondaryStemSizeCurve.Evaluate(pathPercent)) * stem.SizeMultiplier;
        
        var length = math.lerp(minMaxSecondaryStemLength.x, minMaxSecondaryStemLength.y,
            1 - secondaryStemLengthCurve.Evaluate(pathPercent)) * stem.LengthMultiplier;
            
        var pointA = catmullRomPoint.position;
        var pointB = catmullRomPoint.position + right * direction * 0.5f * length / 2f + up;
        var pointC = catmullRomPoint.position + right * direction * 0.5f * length + up;
        
        stem.ClearControlPoints();
        
        stem.SetSize(size);
        
        stem.AddControlPoint(pointA);
        stem.AddControlPoint(pointB);
        stem.AddControlPoint(pointC);
        
        stem.UpdatePath();
        stem.UpdateMesh();
    }
    
    private void InstantiateLeaf(int index)
    {
        var leave = _leavesPool.Value.Get();
        leave.transform.position = new Vector3(0, 0, -(0.05f + index / 100f));
        
        leave.Initialize(index, 0, leaveShapeCurve, leavePathResolution);
        
        _leaves.Add(leave);
    }
    
    private void UpdateLeafPath(int index, CatmullRom.CatmullRomPoint catmullRomPoint)
    {
        var stem = _secondaryStems.ElementAt(index);
        var stemLastPoint = stem.LastCatmullRomPoint;

        var direction = (index % 2) == 0 ? -1 : 1;
        
        var pathPercent = index / (float)_leaves.Count;
        
        var bend = math.lerp(minMaxLeaveBend.x, minMaxLeaveBend.y,
            1 - leaveBendCurve.Evaluate(pathPercent)) * stem.BendMultiplier;
        
        var rotation = math.lerp(minMaxLeaveRotation.x, minMaxLeaveRotation.y,
            leaveRotationCurve.Evaluate(pathPercent)) * stem.RotationMultiplier;
        
        Vector3 right =  math.normalize(math.cross(new float3(catmullRomPoint.tangent.x, catmullRomPoint.tangent.y, 0), new float3(0,0,-1)));
        right = Quaternion.AngleAxis(rotation * stem.RotationMultiplier * direction, -Vector3.forward) * right;
        var up = catmullRomPoint.tangent * bend;

        var size = math.lerp(minMaxLeaveSize.x, minMaxLeaveSize.y,
            leaveSizeCurve.Evaluate(pathPercent)) * stem.SizeMultiplier;
        
        var length = math.lerp(minMaxLeaveLength.x, minMaxLeaveLength.y,
            1 - leaveLengthCurve.Evaluate(pathPercent)) * stem.LengthMultiplier;

        var startPosition = stemLastPoint.position - right * direction * 0.03f;
        
        var pointA = startPosition;
        var pointB = startPosition + right * direction * 0.5f * length / 2f + up;
        var pointC = startPosition + right * direction * 0.5f * length + up;
        
        var leave = _leaves.ElementAt(index);
        
        leave.ClearControlPoints();
        
        leave.SetSize(size);
        
        leave.AddControlPoint(pointA);
        leave.AddControlPoint(pointB);
        leave.AddControlPoint(pointC);
        
        leave.UpdatePath();
        leave.UpdateMesh();
    }
    
    private void ReleaseSecondaryStems()
    {
        foreach (var stem in _secondaryStems)
        {
            _stemsPool.Value.Release(stem);
        }
        
        _secondaryStems.Clear();
    }
    
    private void ReleaseLeaves()
    {
        foreach (var leaf in _leaves)
        {
            _leavesPool.Value.Release(leaf);
        }
        
        _leaves.Clear();
    }
    
    private Vector3 GetPointAtPointerPosition()
    {
        var ray = _camera.ScreenPointToRay(Pointer.current.position.ReadValue());

        return Physics.Raycast(ray, out var hit) ? hit.point : Vector3.negativeInfinity;
    }
}
