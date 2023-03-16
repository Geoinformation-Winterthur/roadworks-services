// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

using NetTopologySuite.Geometries;

public class RoadworkCoordinate
{
    public double X;
    public double Y;
    public RoadworkCoordinate() {}

    public RoadworkCoordinate(double X, double Y){
        this.X = X;
        this.Y = Y;
    }

    public Coordinate convertToNtsCoordinate() {
        return new Coordinate(this.X, this.Y);
    }

}

