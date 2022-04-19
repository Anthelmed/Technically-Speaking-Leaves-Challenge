using System.Collections.Generic;
using JPBotelho;
using UnityEngine;

public static class CatmullRomExtensions
{
        public static List<Vector2> GetSimplifiedPoints(this CatmullRom catmullRom)
        {
                var points = new List<Vector2>();
                var catmullRomPoints = catmullRom.GetPoints();

                foreach (var catmullRomPoint in catmullRomPoints)
                        points.Add(catmullRomPoint.position);
                
                var simplifiedPoints = new List<Vector2>();
                LineUtility.Simplify(points, 0.001f, simplifiedPoints);

                return simplifiedPoints;
        }
}