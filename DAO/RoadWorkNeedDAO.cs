// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc.Routing;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.DAO;
public class RoadWorkNeedDAO
{
    public bool isDryRun;

    public  RoadWorkNeedDAO(bool isDryRun)
    {
        this.isDryRun = isDryRun;
    }

    public RoadWorkNeedFeature Insert(RoadWorkNeedFeature roadWorkNeedFeature,
                    ConfigurationData configData)
    {

        if (roadWorkNeedFeature == null)
        {
            RoadWorkNeedFeature errorObj = new RoadWorkNeedFeature();
            errorObj.errorMessage = "SSP-22";
            return errorObj;
        }

        if (roadWorkNeedFeature.properties.description == null)
        {
            roadWorkNeedFeature.properties.description = "";
        }
        else
        {
            roadWorkNeedFeature.properties.description = roadWorkNeedFeature.properties.description.Trim();
        }

        if (roadWorkNeedFeature.properties.description == "")
        {
            roadWorkNeedFeature.errorMessage = "SSP-23";
            return roadWorkNeedFeature;
        }

        if(roadWorkNeedFeature.properties.url == null)
            roadWorkNeedFeature.properties.url = "";

        roadWorkNeedFeature.properties.url = roadWorkNeedFeature.properties.url.Trim();

        Uri uri;
        bool isUri = Uri.TryCreate(roadWorkNeedFeature.properties.url, UriKind.Absolute, out uri);

        if (roadWorkNeedFeature.properties.url != "" && !isUri)
        {
            roadWorkNeedFeature.errorMessage = "SSP-26";
            return roadWorkNeedFeature;
        }

        Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();
        Coordinate[] coordinates = roadWorkNeedPoly.Coordinates;

        if (coordinates.Length < 3)
        {

            roadWorkNeedFeature.errorMessage = "SSP-7";
            return roadWorkNeedFeature;
        }

        // only if project area is greater than min area size:
        if (roadWorkNeedPoly.Area <= configData.minAreaSize)
        {
            roadWorkNeedFeature = new RoadWorkNeedFeature();
            roadWorkNeedFeature.errorMessage = "SSP-8";
            return roadWorkNeedFeature;
        }

        // only if project area is smaller than max area size:
        if (roadWorkNeedPoly.Area > configData.maxAreaSize)
        {
            roadWorkNeedFeature = new RoadWorkNeedFeature();
            roadWorkNeedFeature.errorMessage = "SSP-16";
            return roadWorkNeedFeature;
        }

        if (roadWorkNeedFeature.properties.name != null)
        {
            roadWorkNeedFeature.properties.name = roadWorkNeedFeature.properties.name.Trim();
        }

        Guid resultUuid = Guid.NewGuid();
        roadWorkNeedFeature.properties.uuid = resultUuid.ToString();

        if (isDryRun)
        {
            roadWorkNeedFeature.errorMessage = "SSP-25";
            return roadWorkNeedFeature;
        }

        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand insertComm = _CreatePreparedStatementForInsert(roadWorkNeedFeature,
                            roadWorkNeedPoly, pgConn);
            insertComm.ExecuteNonQuery();
        }
        return roadWorkNeedFeature;
    }

    private NpgsqlCommand _CreatePreparedStatementForInsert(RoadWorkNeedFeature roadWorkNeedFeature,
                        Polygon roadWorkNeedPoly, NpgsqlConnection pgConn)
    {
        NpgsqlCommand insertComm = pgConn.CreateCommand();
        insertComm.CommandText = @"INSERT INTO ""wtb_ssp_roadworkneeds""
                                    (uuid, name, orderer, created, last_modified, finish_early_to,
                                    finish_optimum_to, finish_late_to, priority, status, description, relevance,
                                    costs, private, section, comment, url, geom)
                                    VALUES (@uuid, @name, @orderer, @created, @last_modified,
                                    @finish_early_to, @finish_optimum_to,
                                    @finish_late_to, @priority, @status, @description, @relevance,
                                    @costs, @private, @section, @comment, @url, @geom)";
        insertComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));
        insertComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
        if (roadWorkNeedFeature.properties.orderer.uuid != "")
        {
            insertComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.orderer.uuid));
        }
        else
        {
            insertComm.Parameters.AddWithValue("orderer", DBNull.Value);
        }
        roadWorkNeedFeature.properties.created = DateTime.Now;
        insertComm.Parameters.AddWithValue("created", roadWorkNeedFeature.properties.created);
        roadWorkNeedFeature.properties.lastModified = DateTime.Now;
        insertComm.Parameters.AddWithValue("last_modified", roadWorkNeedFeature.properties.lastModified);
        insertComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
        insertComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
        insertComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
        insertComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priority.code);
        insertComm.Parameters.AddWithValue("status", roadWorkNeedFeature.properties.status.code);
        insertComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
        insertComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
        insertComm.Parameters.AddWithValue("costs", roadWorkNeedFeature.properties.costs != 0 ? roadWorkNeedFeature.properties.costs : DBNull.Value);
        insertComm.Parameters.AddWithValue("private", roadWorkNeedFeature.properties.isPrivate);
        insertComm.Parameters.AddWithValue("section", roadWorkNeedFeature.properties.section);
        insertComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
        insertComm.Parameters.AddWithValue("url", roadWorkNeedFeature.properties.url);
        insertComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

        return insertComm;
    }

}