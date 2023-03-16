// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace roadwork_portal_service.Model;

public class RoadworkPolygon
{
    public RoadworkCoordinate[] coordinates { get; set; }
    public RoadworkPolygon() {
        coordinates = new RoadworkCoordinate[0];
    }

    public RoadworkPolygon(Polygon polygon) {
        List<RoadworkCoordinate> resultCoords = new List<RoadworkCoordinate>();
        foreach(Coordinate coord in polygon.ExteriorRing.Coordinates){
            resultCoords.Add(new RoadworkCoordinate(coord.X, coord.Y));
        }
        this.coordinates = resultCoords.ToArray();
    }

    public Polygon getNtsPolygon() {
        GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 2056);
        List<Coordinate> ntsCoords = new List<Coordinate>();
        foreach(RoadworkCoordinate coord in this.coordinates){
            ntsCoords.Add(new Coordinate(coord.X, coord.Y));
        }
        Polygon ntsPoly = geomFactory.CreatePolygon(ntsCoords.ToArray());
        return ntsPoly;
    }



}

