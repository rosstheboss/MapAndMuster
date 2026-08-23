using System.Globalization;
using MapAndMuster.Domain.Maps;

namespace MapAndMuster.Backend.UnitTests.Maps;

public sealed class PolygonGeometryTests
{
    [Fact]
    public void ClampToMapKeepsPointsOnTheImageRectangle()
    {
        var clamped = PolygonGeometry.ClampToMap([new MapPoint(-0.2, 1.4), new MapPoint(0.3, 0.4)]);

        Assert.Equal(new MapPoint(0, 1), clamped[0]);
        Assert.Equal(new MapPoint(0.3, 0.4), clamped[1]);
    }

    [Fact]
    public void FindSnapTargetHighlightsANearbyVertex()
    {
        var snap = PolygonGeometry.FindSnapTarget(new MapPoint(0.201, 0.199), [new MapPoint(0.2, 0.2), new MapPoint(0.8, 0.8)]);

        Assert.Equal(new MapPoint(0.2, 0.2), snap);
    }

    [Fact]
    public void FindSnapTargetIgnoresDistantVertices()
    {
        var snap = PolygonGeometry.FindSnapTarget(new MapPoint(0.5, 0.5), [new MapPoint(0.1, 0.1)]);

        Assert.Null(snap);
    }

    [Fact]
    public void SharedBorderIsAllowedAndSuppliesAMarkerMidpoint()
    {
        var left = Square(0.1, 0.1, 0.3);
        var right = Square(0.4, 0.1, 0.3);

        Assert.False(PolygonGeometry.InteriorsOverlap(left, right));
        Assert.True(PolygonGeometry.TrySharedBorder(left, right, out var midpoint));
        Assert.Equal(0.4, midpoint.X, 3);
        Assert.Equal(0.25, midpoint.Y, 3);
    }

    [Fact]
    public void OverlappingInteriorsAreRejected()
    {
        var left = Square(0.1, 0.1, 0.4);
        var right = Square(0.3, 0.1, 0.4);

        Assert.True(PolygonGeometry.InteriorsOverlap(left, right));
    }

    [Fact]
    public void ThinInteriorOverlapIsStillRejected()
    {
        var left = Square(0.1, 0.1, 0.3);
        var right = Square(0.39, 0.1, 0.3);

        Assert.True(PolygonGeometry.InteriorsOverlap(left, right));
    }

    [Fact]
    public void NestedTerritoryCountsAsOverlap()
    {
        var outer = Square(0.1, 0.1, 0.8);
        var inner = Square(0.3, 0.3, 0.2);

        Assert.True(PolygonGeometry.InteriorsOverlap(outer, inner));
    }

    [Fact]
    public void ExtraVerticesAlongASharedBorderAreAllowed()
    {
        var existing = Square(0.1, 0.1, 0.3);
        MapPoint[] neighbor =
        [
            new(0.4, 0.1),
            new(0.399, 0.2),
            new(0.4, 0.3),
            new(0.4, 0.4),
            new(0.7, 0.4),
            new(0.7, 0.1),
        ];

        Assert.False(PolygonGeometry.InteriorsOverlap(existing, neighbor));
    }

    [Fact]
    public void NearCollinearZigzagAlongASharedBorderIsNotOverlap()
    {
        var existing = Square(0.1, 0.1, 0.3);
        MapPoint[] neighbor =
        [
            new(0.4, 0.1),
            new(0.401, 0.18),
            new(0.399, 0.26),
            new(0.4, 0.4),
            new(0.7, 0.4),
            new(0.7, 0.1),
        ];

        Assert.False(PolygonGeometry.InteriorsOverlap(existing, neighbor));
    }

    [Fact]
    public void WrappingCoastlineIsNotOverlapWhenOnlyTheBorderIsShared()
    {
        var plains = new MapPoint[]
        {
            new(0.4, 0.4),
            new(0.6, 0.4),
            new(0.6, 0.6),
            new(0.4, 0.6),
        };
        MapPoint[] coastline =
        [
            new(0.35, 0.35),
            new(0.65, 0.35),
            new(0.65, 0.65),
            new(0.35, 0.65),
            new(0.35, 0.60),
            new(0.60, 0.60),
            new(0.60, 0.40),
            new(0.35, 0.40),
        ];

        Assert.False(PolygonGeometry.InteriorsOverlap(plains, coastline));
        Assert.True(PolygonGeometry.ContainsStrict(plains, PolygonGeometry.Centroid(coastline)));
    }

    [Fact]
    public void SelfIntersectingPolygonIsInvalid()
    {
        var bowtie = new MapPoint[]
        {
            new(0.2, 0.2),
            new(0.6, 0.6),
            new(0.6, 0.2),
            new(0.2, 0.6),
        };

        Assert.False(PolygonGeometry.IsValidTerritoryPolygon(bowtie));
    }

    [Fact]
    public void PointsOutsideTheMapAreInvalid()
    {
        var polygon = new MapPoint[]
        {
            new(0.1, 0.1),
            new(1.2, 0.1),
            new(0.1, 0.4),
        };

        Assert.False(PolygonGeometry.IsValidTerritoryPolygon(polygon));
    }

    [Fact]
    public void ExtraVertexOnASharedBorderIsAJunctionNotOverlap()
    {
        var existing = Square(0.1, 0.1, 0.3);
        MapPoint[] neighbor =
        [
            new(0.4, 0.1),
            new(0.4, 0.25),
            new(0.4, 0.4),
            new(0.55, 0.4),
            new(0.52, 0.25),
            new(0.55, 0.1),
        ];

        Assert.False(PolygonGeometry.InteriorsOverlap(existing, neighbor));
    }

    [Fact]
    public void SvgSharedBorderWithExtraVerticesIsNotOverlap()
    {
        var plains = ParseSvgPoints(
            "0.189575,0.129264 0.20514,0.146279 0.233826,0.15436 0.250611,0.156718 0.269149,0.16001 0.269149,0.16001 0.268053,0.156169 0.26753,0.154337 0.265729,0.152668 0.259623,0.151211 0.255699,0.145353 0.252706,0.140885 0.248637,0.128537 0.246399,0.121129 0.246399,0.121129 0.234192,0.118435 0.234192,0.118435 0.224833,0.123374 0.201233,0.118884");
        var sahigun = ParseSvgPoints(
            "0.259588,0.158312 0.25505,0.157506 0.250611,0.156718 0.243611,0.155735 0.233826,0.15436 0.233826,0.15436 0.22595,0.152141 0.219055,0.150199 0.219055,0.150199 0.218769,0.186923 0.231307,0.187346 0.251815,0.186807");
        var river = ParseSvgPoints(
            "0.259588,0.158312 0.269149,0.16001 0.269149,0.16001 0.284611,0.16435 0.284611,0.16435 0.295326,0.160459 0.322571,0.169836 0.314433,0.185101 0.286357,0.194979 0.251815,0.186807");

        Assert.False(PolygonGeometry.InteriorsOverlap(plains, sahigun));
        Assert.False(PolygonGeometry.InteriorsOverlap(plains, river));
    }

    private static MapPoint[] ParseSvgPoints(string points)
    {
        var numbers = points.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => double.Parse(part, CultureInfo.InvariantCulture))
            .ToArray();
        var parsed = new List<MapPoint>(numbers.Length / 2);
        for (var index = 0; index + 1 < numbers.Length; index += 2)
        {
            parsed.Add(new MapPoint(numbers[index], numbers[index + 1]));
        }

        return [.. parsed];
    }

    private static MapPoint[] Square(double x, double y, double size)
    {
        return
        [
            new MapPoint(x, y),
            new MapPoint(x + size, y),
            new MapPoint(x + size, y + size),
            new MapPoint(x, y + size),
        ];
    }
}
