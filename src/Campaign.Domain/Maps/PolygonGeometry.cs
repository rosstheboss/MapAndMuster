namespace Campaign.Domain.Maps;

/// <summary>
/// Geometry helpers for overlay polygons on the rectangular map image.
/// Coordinates are normalized to the unit square. Shared borders are allowed; overlapping interiors are not.
/// </summary>
public static class PolygonGeometry
{
    /// <summary>Distance treated as the same vertex or collinear point.</summary>
    public const double Epsilon = 1e-6;

    /// <summary>Minimum shared-border length, as a fraction of the map, required to suggest adjacency.</summary>
    public const double MinSharedBorderLength = 0.008;

    /// <summary>Squared distance used when snapping a drawing cursor to an existing vertex.</summary>
    public const double SnapDistance = 0.018;

    /// <summary>
    /// Clamps every vertex onto the map rectangle.
    /// </summary>
    /// <param name="points">The vertices.</param>
    /// <returns>The clamped vertices.</returns>
    public static IReadOnlyList<MapPoint> ClampToMap(IReadOnlyList<MapPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        return [.. points.Select(static point => point.ClampToMap())];
    }

    /// <summary>
    /// Returns the nearest existing vertex within the snap radius, if any.
    /// </summary>
    /// <param name="cursor">The drawing cursor.</param>
    /// <param name="vertices">Candidate vertices.</param>
    /// <returns>The snap target, or <see langword="null"/>.</returns>
    public static MapPoint? FindSnapTarget(MapPoint cursor, IEnumerable<MapPoint> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        MapPoint? best = null;
        var bestDistance = SnapDistance * SnapDistance;
        foreach (var vertex in vertices)
        {
            var distance = cursor.DistanceSquaredTo(vertex);
            if (distance <= bestDistance)
            {
                best = vertex;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Gets whether a closed polygon is usable as a territory: at least three distinct points, on the map,
    /// with positive area and no self-intersection.
    /// </summary>
    /// <param name="polygon">The vertices, without a duplicated closing point.</param>
    /// <returns><see langword="true"/> when the polygon can become a territory.</returns>
    public static bool IsValidTerritoryPolygon(IReadOnlyList<MapPoint> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        if (polygon.Count < 3)
        {
            return false;
        }

        for (var index = 0; index < polygon.Count; index++)
        {
            if (!polygon[index].IsOnMap)
            {
                return false;
            }
        }

        var unique = DistinctVertices(polygon);
        if (unique.Count < 3 || Area(unique) < Epsilon)
        {
            return false;
        }

        return !SelfIntersects(unique);
    }

    /// <summary>
    /// Gets whether two polygons' interiors overlap. Shared vertices and collinear shared borders are allowed.
    /// </summary>
    /// <param name="left">The first polygon.</param>
    /// <param name="right">The second polygon.</param>
    /// <returns><see langword="true"/> when interiors overlap.</returns>
    public static bool InteriorsOverlap(IReadOnlyList<MapPoint> left, IReadOnlyList<MapPoint> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Count < 3 || right.Count < 3)
        {
            return false;
        }

        if (HasProperEdgeCrossing(left, right))
        {
            return true;
        }

        if (HasVertexStrictlyInside(left, right) || HasVertexStrictlyInside(right, left))
        {
            return true;
        }

        return HasInteriorSampleInside(left, right) || HasInteriorSampleInside(right, left);
    }

    /// <summary>
    /// Finds the longest shared border between two polygons, when it is long enough to suggest adjacency.
    /// </summary>
    /// <param name="left">The first polygon.</param>
    /// <param name="right">The second polygon.</param>
    /// <param name="midpoint">The midpoint of the shared border.</param>
    /// <returns><see langword="true"/> when a usable shared border exists.</returns>
    public static bool TrySharedBorder(
        IReadOnlyList<MapPoint> left,
        IReadOnlyList<MapPoint> right,
        out MapPoint midpoint)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        midpoint = default;
        var bestLength = MinSharedBorderLength;
        var found = false;
        var leftCount = left.Count;
        var rightCount = right.Count;
        for (var i = 0; i < leftCount; i++)
        {
            var a1 = left[i];
            var a2 = left[(i + 1) % leftCount];
            for (var j = 0; j < rightCount; j++)
            {
                var b1 = right[j];
                var b2 = right[(j + 1) % rightCount];
                if (!TryCollinearOverlap(a1, a2, b1, b2, out var overlapStart, out var overlapEnd))
                {
                    continue;
                }

                var length = Math.Sqrt(overlapStart.DistanceSquaredTo(overlapEnd));
                if (length >= bestLength)
                {
                    bestLength = length;
                    midpoint = new MapPoint(
                        (overlapStart.X + overlapEnd.X) / 2,
                        (overlapStart.Y + overlapEnd.Y) / 2);
                    found = true;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Returns the average of the vertices. Used as a fallback adjacency marker.
    /// </summary>
    /// <param name="polygon">The vertices.</param>
    /// <returns>The centroid.</returns>
    public static MapPoint Centroid(IReadOnlyList<MapPoint> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        if (polygon.Count == 0)
        {
            return new MapPoint(0.5, 0.5);
        }

        var x = 0d;
        var y = 0d;
        foreach (var point in polygon)
        {
            x += point.X;
            y += point.Y;
        }

        return new MapPoint(x / polygon.Count, y / polygon.Count);
    }

    /// <summary>
    /// Gets whether the point is strictly inside the polygon (not on an edge).
    /// </summary>
    /// <param name="polygon">The vertices.</param>
    /// <param name="point">The test point.</param>
    /// <returns><see langword="true"/> when the point is in the interior.</returns>
    public static bool ContainsStrict(IReadOnlyList<MapPoint> polygon, MapPoint point)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        if (IsOnBoundary(polygon, point))
        {
            return false;
        }

        var inside = false;
        var count = polygon.Count;
        var j = count - 1;
        for (var i = 0; i < count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];
            var intersect = ((a.Y > point.Y) != (b.Y > point.Y))
                && (point.X < ((b.X - a.X) * (point.Y - a.Y) / ((b.Y - a.Y) + double.Epsilon)) + a.X);
            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Signed polygon area. Absolute value is the geometric area in normalized units.
    /// </summary>
    /// <param name="polygon">The vertices.</param>
    /// <returns>The signed area.</returns>
    public static double Area(IReadOnlyList<MapPoint> polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        var area = 0d;
        var count = polygon.Count;
        for (var i = 0; i < count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % count];
            area += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(area) / 2;
    }

    private static List<MapPoint> DistinctVertices(IReadOnlyList<MapPoint> polygon)
    {
        var points = new List<MapPoint>(polygon.Count);
        foreach (var point in polygon)
        {
            if (points.Count > 0 && points[^1].DistanceSquaredTo(point) <= Epsilon * Epsilon)
            {
                continue;
            }

            points.Add(point);
        }

        if (points.Count > 1 && points[0].DistanceSquaredTo(points[^1]) <= Epsilon * Epsilon)
        {
            points.RemoveAt(points.Count - 1);
        }

        return points;
    }

    private static bool SelfIntersects(List<MapPoint> polygon)
    {
        var count = polygon.Count;
        for (var i = 0; i < count; i++)
        {
            var a1 = polygon[i];
            var a2 = polygon[(i + 1) % count];
            for (var j = i + 1; j < count; j++)
            {
                if (Math.Abs(i - j) <= 1 || (i == 0 && j == count - 1))
                {
                    continue;
                }

                var b1 = polygon[j];
                var b2 = polygon[(j + 1) % count];
                if (SegmentsProperlyIntersect(a1, a2, b1, b2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasProperEdgeCrossing(IReadOnlyList<MapPoint> left, IReadOnlyList<MapPoint> right)
    {
        var leftCount = left.Count;
        var rightCount = right.Count;
        for (var i = 0; i < leftCount; i++)
        {
            var a1 = left[i];
            var a2 = left[(i + 1) % leftCount];
            for (var j = 0; j < rightCount; j++)
            {
                var b1 = right[j];
                var b2 = right[(j + 1) % rightCount];
                if (SegmentsProperlyIntersect(a1, a2, b1, b2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasVertexStrictlyInside(IReadOnlyList<MapPoint> vertices, IReadOnlyList<MapPoint> polygon)
    {
        foreach (var vertex in vertices)
        {
            if (ContainsStrict(polygon, vertex))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasInteriorSampleInside(IReadOnlyList<MapPoint> source, IReadOnlyList<MapPoint> other)
    {
        var center = Centroid(source);
        if (ContainsStrict(other, center))
        {
            return true;
        }

        foreach (var vertex in source)
        {
            var sample = new MapPoint((vertex.X + center.X) / 2, (vertex.Y + center.Y) / 2);
            if (ContainsStrict(other, sample))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOnBoundary(IReadOnlyList<MapPoint> polygon, MapPoint point)
    {
        var count = polygon.Count;
        for (var i = 0; i < count; i++)
        {
            if (PointOnSegment(polygon[i], polygon[(i + 1) % count], point))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SegmentsProperlyIntersect(MapPoint a1, MapPoint a2, MapPoint b1, MapPoint b2)
    {
        var o1 = Orientation(a1, a2, b1);
        var o2 = Orientation(a1, a2, b2);
        var o3 = Orientation(b1, b2, a1);
        var o4 = Orientation(b1, b2, a2);
        return o1 * o2 < 0 && o3 * o4 < 0;
    }

    private static double Orientation(MapPoint a, MapPoint b, MapPoint c)
    {
        return ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));
    }

    private static bool PointOnSegment(MapPoint a, MapPoint b, MapPoint p)
    {
        if (Math.Abs(Orientation(a, b, p)) > Epsilon)
        {
            return false;
        }

        var minX = Math.Min(a.X, b.X) - Epsilon;
        var maxX = Math.Max(a.X, b.X) + Epsilon;
        var minY = Math.Min(a.Y, b.Y) - Epsilon;
        var maxY = Math.Max(a.Y, b.Y) + Epsilon;
        return p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY;
    }

    private static bool TryCollinearOverlap(
        MapPoint a1,
        MapPoint a2,
        MapPoint b1,
        MapPoint b2,
        out MapPoint overlapStart,
        out MapPoint overlapEnd)
    {
        overlapStart = default;
        overlapEnd = default;
        if (Math.Abs(Orientation(a1, a2, b1)) > Epsilon * 10 || Math.Abs(Orientation(a1, a2, b2)) > Epsilon * 10)
        {
            return false;
        }

        var dx = a2.X - a1.X;
        var dy = a2.Y - a1.Y;
        var axisLength = Math.Sqrt((dx * dx) + (dy * dy));
        if (axisLength < Epsilon)
        {
            return false;
        }

        double Project(MapPoint point)
        {
            return (((point.X - a1.X) * dx) + ((point.Y - a1.Y) * dy)) / axisLength;
        }

        var aMin = 0d;
        var aMax = axisLength;
        var bMin = Project(b1);
        var bMax = Project(b2);
        if (bMin > bMax)
        {
            (bMin, bMax) = (bMax, bMin);
        }

        var start = Math.Max(aMin, bMin);
        var end = Math.Min(aMax, bMax);
        if (end - start < Epsilon)
        {
            return false;
        }

        var ux = dx / axisLength;
        var uy = dy / axisLength;
        overlapStart = new MapPoint(a1.X + (ux * start), a1.Y + (uy * start));
        overlapEnd = new MapPoint(a1.X + (ux * end), a1.Y + (uy * end));
        return true;
    }
}
