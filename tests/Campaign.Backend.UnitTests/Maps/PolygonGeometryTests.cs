using Campaign.Domain.Maps;

namespace Campaign.Backend.UnitTests.Maps;

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
