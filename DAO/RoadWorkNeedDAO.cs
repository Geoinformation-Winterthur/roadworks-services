// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>

using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.DAO;
public class RoadWorkNeedDAO
{
    public bool isDryRun;

    public RoadWorkNeedDAO(bool isDryRun)
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

        if (roadWorkNeedFeature.properties.overarchingMeasure &&
        ((roadWorkNeedFeature.properties.desiredYearFrom == null
            || roadWorkNeedFeature.properties.desiredYearFrom < DateTime.Now.Year) ||
            (roadWorkNeedFeature.properties.desiredYearTo == null
            || roadWorkNeedFeature.properties.desiredYearTo < DateTime.Now.Year)))
        {
            roadWorkNeedFeature.errorMessage = "SSP-27";
            return roadWorkNeedFeature;
        }

        if (roadWorkNeedFeature.properties.url == null)
            roadWorkNeedFeature.properties.url = "";

        roadWorkNeedFeature.properties.url = roadWorkNeedFeature.properties.url.Trim();

        Uri uri;
        bool isUri = Uri.TryCreate(roadWorkNeedFeature.properties.url, UriKind.Absolute, out uri);

        if (roadWorkNeedFeature.properties.url != "" && !isUri)
        {
            roadWorkNeedFeature.errorMessage = "SSP-26";
            return roadWorkNeedFeature;
        }

        if (roadWorkNeedFeature.properties.hasSpongeCityMeasures)
        {
            if (roadWorkNeedFeature.properties.spongeCityMeasures == null ||
                roadWorkNeedFeature.properties.spongeCityMeasures.Length == 0)
            {
                roadWorkNeedFeature.errorMessage = "SSP-38";
                return roadWorkNeedFeature;
            }

            bool hasNonEmptyEntries = false;
            for (int i = 0; i < roadWorkNeedFeature.properties.spongeCityMeasures.Length; i++)
            {
                if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != null)
                    roadWorkNeedFeature.properties.spongeCityMeasures[i] =
                        roadWorkNeedFeature.properties.spongeCityMeasures[i].Trim();

                if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != String.Empty)
                    hasNonEmptyEntries = true;
            }

            if (!hasNonEmptyEntries)
            {
                roadWorkNeedFeature.errorMessage = "SSP-38";
                return roadWorkNeedFeature;
            }
        }

        if (roadWorkNeedFeature.properties.orderer.organisationalUnit.isCivilEngineering)
        {
            if (roadWorkNeedFeature.properties.costs == null ||
                    roadWorkNeedFeature.properties.costs.Length == 0)
            {
                roadWorkNeedFeature.errorMessage = "SSP-40";
                return roadWorkNeedFeature;
            }

            foreach (Costs cost in roadWorkNeedFeature.properties.costs)
            {
                if (!IsCostsValid(cost))
                {
                    roadWorkNeedFeature.errorMessage = "SSP-40";
                    return roadWorkNeedFeature;
                }
            }
        }
        else
        {
            roadWorkNeedFeature.properties.costs = null;
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

            using (NpgsqlTransaction trans = pgConn.BeginTransaction())
            {
                NpgsqlCommand insertComm = _CreatePreparedStatementForInsert(roadWorkNeedFeature,
                            roadWorkNeedPoly, pgConn);
                insertComm.ExecuteNonQuery();

                if (roadWorkNeedFeature.properties.costs != null)
                {
                    foreach (Costs costs in roadWorkNeedFeature.properties.costs)
                    {
                        if (costs != null)
                        {
                            if (costs.workTitle != null) costs.workTitle = costs.workTitle.Trim().ToLower();
                            if (costs.projectType != null) costs.projectType = costs.projectType.Trim().ToLower();
                            if (costs.costsComment != null) costs.costsComment = costs.costsComment.Trim();

                            NpgsqlCommand insertCostsComm =
                                _CreatePreparedStatementForInsertCosts(costs,
                                            roadWorkNeedFeature.properties.uuid, pgConn);
                            insertCostsComm.ExecuteNonQuery();
                        }
                    }
                }
                trans.Commit();
            }
        }

        return roadWorkNeedFeature;
    }

    private NpgsqlCommand _CreatePreparedStatementForInsert(RoadWorkNeedFeature roadWorkNeedFeature,
                        Polygon roadWorkNeedPoly, NpgsqlConnection pgConn)
    {
        NpgsqlCommand insertComm = pgConn.CreateCommand();
        insertComm.CommandText = @"INSERT INTO ""wtb_ssp_roadworkneeds""
                                    (uuid, name, orderer, created, last_modified, finish_early_to,
                                    finish_optimum_to, finish_late_to, priority, status, description,
                                    private, section, comment, url, overarching_measure,
                                    desired_year_from, desired_year_to, has_sponge_city_meas,
                                    is_sponge_1_1, is_sponge_1_2, is_sponge_1_3, is_sponge_1_4,
                                    is_sponge_1_5, is_sponge_1_6, is_sponge_1_7, is_sponge_1_8,
                                    is_sponge_2_1, is_sponge_2_2, is_sponge_2_3, is_sponge_2_4,
                                    is_sponge_2_5, is_sponge_2_6, is_sponge_2_7, is_sponge_3_1,
                                    is_sponge_3_2, is_sponge_3_3, is_sponge_4_1, is_sponge_4_2,
                                    is_sponge_5_1, still_relevant, decline, feedback_given, geom)
                                    VALUES (@uuid, @name, @orderer, @created, @last_modified,
                                    @finish_early_to, @finish_optimum_to,
                                    @finish_late_to, @priority, @status, @description,
                                    @private, @section, @comment, @url,
                                    @overarching_measure, @desired_year_from, @desired_year_to, 
                                    @has_sponge_city_meas, @is_sponge_1_1, @is_sponge_1_2,
                                    @is_sponge_1_3, @is_sponge_1_4, @is_sponge_1_5,
                                    @is_sponge_1_6, @is_sponge_1_7, @is_sponge_1_8,
                                    @is_sponge_2_1, @is_sponge_2_2, @is_sponge_2_3, @is_sponge_2_4,
                                    @is_sponge_2_5, @is_sponge_2_6, @is_sponge_2_7, @is_sponge_3_1,
                                    @is_sponge_3_2, @is_sponge_3_3, @is_sponge_4_1, @is_sponge_4_2,
                                    @is_sponge_5_1, @still_relevant, @decline, @feedback_given, @geom)";
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
        insertComm.Parameters.AddWithValue("status", roadWorkNeedFeature.properties.status);
        insertComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
        insertComm.Parameters.AddWithValue("private", roadWorkNeedFeature.properties.isPrivate);
        insertComm.Parameters.AddWithValue("still_relevant", roadWorkNeedFeature.properties.stillRelevant != null ? roadWorkNeedFeature.properties.stillRelevant : DBNull.Value);
        insertComm.Parameters.AddWithValue("feedback_given", roadWorkNeedFeature.properties.feedbackGiven != null ? roadWorkNeedFeature.properties.feedbackGiven : DBNull.Value);
        insertComm.Parameters.AddWithValue("decline", roadWorkNeedFeature.properties.decline != null ? roadWorkNeedFeature.properties.decline : DBNull.Value);
        insertComm.Parameters.AddWithValue("section", roadWorkNeedFeature.properties.section);
        insertComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
        insertComm.Parameters.AddWithValue("url", roadWorkNeedFeature.properties.url);
        insertComm.Parameters.AddWithValue("overarching_measure", roadWorkNeedFeature.properties.overarchingMeasure);
        if (roadWorkNeedFeature.properties.desiredYearFrom != null)
            insertComm.Parameters.AddWithValue("desired_year_from", roadWorkNeedFeature.properties.desiredYearFrom);
        else
            insertComm.Parameters.AddWithValue("desired_year_from", DBNull.Value);
        if (roadWorkNeedFeature.properties.desiredYearTo != null)
            insertComm.Parameters.AddWithValue("desired_year_to", roadWorkNeedFeature.properties.desiredYearTo);
        else
            insertComm.Parameters.AddWithValue("desired_year_to", DBNull.Value);
        insertComm.Parameters.AddWithValue("has_sponge_city_meas", roadWorkNeedFeature.properties.hasSpongeCityMeasures);

        insertComm.Parameters.AddWithValue("is_sponge_1_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.1"));
        insertComm.Parameters.AddWithValue("is_sponge_1_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.2"));
        insertComm.Parameters.AddWithValue("is_sponge_1_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.3"));
        insertComm.Parameters.AddWithValue("is_sponge_1_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.4"));
        insertComm.Parameters.AddWithValue("is_sponge_1_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.5"));
        insertComm.Parameters.AddWithValue("is_sponge_1_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.6"));
        insertComm.Parameters.AddWithValue("is_sponge_1_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.7"));
        insertComm.Parameters.AddWithValue("is_sponge_1_8", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.8"));
        insertComm.Parameters.AddWithValue("is_sponge_2_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.1"));
        insertComm.Parameters.AddWithValue("is_sponge_2_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.2"));
        insertComm.Parameters.AddWithValue("is_sponge_2_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.3"));
        insertComm.Parameters.AddWithValue("is_sponge_2_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.4"));
        insertComm.Parameters.AddWithValue("is_sponge_2_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.5"));
        insertComm.Parameters.AddWithValue("is_sponge_2_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.6"));
        insertComm.Parameters.AddWithValue("is_sponge_2_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.7"));
        insertComm.Parameters.AddWithValue("is_sponge_3_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.1"));
        insertComm.Parameters.AddWithValue("is_sponge_3_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.2"));
        insertComm.Parameters.AddWithValue("is_sponge_3_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.3"));
        insertComm.Parameters.AddWithValue("is_sponge_4_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.1"));
        insertComm.Parameters.AddWithValue("is_sponge_4_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.2"));
        insertComm.Parameters.AddWithValue("is_sponge_5_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("5.1"));

        insertComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

        return insertComm;
    }

    private NpgsqlCommand _CreatePreparedStatementForInsertCosts(Costs costs, string roadWorkNeedUuid,
                    NpgsqlConnection pgConn)
    {
        NpgsqlCommand insertCostsComm = pgConn.CreateCommand();
        insertCostsComm.CommandText = @"INSERT INTO ""wtb_ssp_costs""
                                    (uuid, roadworkneed, costs, work_title,
                                    project_type, costs_comment)
                                    VALUES (@uuid, @roadworkneed, @costs, @work_title,
                                    @project_type, @costs_comment)";
        insertCostsComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
        insertCostsComm.Parameters.AddWithValue("roadworkneed", new Guid(roadWorkNeedUuid));
        insertCostsComm.Parameters.AddWithValue("costs", costs.costs != null ? costs.costs : DBNull.Value);
        insertCostsComm.Parameters.AddWithValue("work_title", costs.workTitle != null ? costs.workTitle : DBNull.Value);
        insertCostsComm.Parameters.AddWithValue("project_type", costs.projectType != null ? costs.projectType : DBNull.Value);
        insertCostsComm.Parameters.AddWithValue("costs_comment", costs.costsComment != null ? costs.costsComment : DBNull.Value);

        return insertCostsComm;
    }

    public static bool IsCostsValid(Costs costs)
    {
        bool isNotValid = false;
        if (costs == null) return false;
        if (costs.workTitle == null || costs.workTitle == "")
            isNotValid = true;
        if (costs.projectType == null || costs.projectType == "")
            isNotValid = true;
        if (costs.costs == null || costs.costs == 0)
            isNotValid = true;
        return !isNotValid;
    }

}