using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatisticsController : ControllerBase
    {
        private readonly ILogger<StatisticsController> _logger;
        private IConfiguration Configuration { get; }

        public StatisticsController(ILogger<StatisticsController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET statistics/?statisticsname=...
        [HttpGet]
        [Authorize(Roles = "administrator")]
        public async Task<IEnumerable<ChartEntry>> GetStatistics(string statisticsName = "")
        {

            List<ChartEntry> chartEntries = new List<ChartEntry>();
            try
            {
                statisticsName = statisticsName.Trim().ToLower();

                if (String.Empty == statisticsName)
                {
                    _logger.LogWarning("No statics name was given in get statistics method.");
                    ChartEntry chartEntry = new ChartEntry();
                    chartEntry.errorMessage = "SSP-3";
                    chartEntries.Add(chartEntry);
                    return chartEntries;
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    NpgsqlCommand selectStatisticsComm = pgConn.CreateCommand();

                    if (statisticsName == "new_activities_last_month" || statisticsName == "new_needs_last_month")
                    {

                        selectStatisticsComm.CommandText = "SELECT extract(month from created), count(created) FROM ";
                        if (statisticsName == "new_activities_last_month")
                        {
                            selectStatisticsComm.CommandText += "\"wtb_ssp_roadworkactivities\"";
                        }
                        else if (statisticsName == "new_needs_last_month")
                        {
                            selectStatisticsComm.CommandText += "\"wtb_ssp_roadworkneeds\"";
                        }

                        selectStatisticsComm.CommandText += @" WHERE created > now() - interval '5 month'
                                GROUP BY extract(year from created), extract(month from created)
                                ORDER BY extract(year from created) ASC, extract(month from created) ASC";

                    }
                    else if (statisticsName == "activities_of_area_man")
                    {
                        selectStatisticsComm.CommandText = @"SELECT u.first_name || ' ' || u.last_name man_name, count(*)
                                        FROM ""wtb_ssp_users"" u
                                        LEFT JOIN ""wtb_ssp_managementareas"" ma ON u.uuid = ma.manager
                                        LEFT JOIN (SELECT r.uuid need_uuid, m.uuid man_area_uuid, max(ST_Area(ST_Intersection(r.geom, m.geom)))
                                        FROM ""wtb_ssp_roadworkactivities"" r, ""wtb_ssp_managementareas"" m
                                        WHERE ST_Area(ST_Intersection(r.geom, m.geom)) > 0
                                        GROUP BY r.uuid, m.uuid) r ON r.man_area_uuid=ma.uuid
                                        WHERE u.role_territorymanager=true AND u.active=true AND ma.name IS NOT NULL
                                        GROUP BY u.uuid, u.first_name, u.last_name;";
                    }

                    using (NpgsqlDataReader statisticsReader = await selectStatisticsComm.ExecuteReaderAsync())
                    {
                        while (statisticsReader.Read())
                        {
                            ChartEntry chartEntry = new ChartEntry();
                            if (statisticsName == "new_activities_last_month" ||
                                    statisticsName == "new_needs_last_month")
                            {
                                chartEntry.label = statisticsReader.IsDBNull(0) ? "" : translateMonth((int)statisticsReader.GetDouble(0));
                            } else {
                                chartEntry.label = statisticsReader.IsDBNull(0) ? "" : statisticsReader.GetString(0);
                            }

                            if (!statisticsReader.IsDBNull(1)) chartEntry.value = statisticsReader.GetInt32(1);

                            chartEntries.Add(chartEntry);
                        }
                    }
                    pgConn.Close();
                }

                return chartEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ChartEntry chartEntry = new ChartEntry();
                chartEntry.errorMessage = "SSP-3";
                chartEntries.Add(chartEntry);
                return chartEntries;
            }

        }

        private static string translateMonth(int monthNumber)
        {
            string monthName = "";
            if (monthNumber == 1) monthName = "Jan";
            else if (monthNumber == 2) monthName = "Feb";
            else if (monthNumber == 3) monthName = "Mar";
            else if (monthNumber == 4) monthName = "Apr";
            else if (monthNumber == 5) monthName = "Mai";
            else if (monthNumber == 6) monthName = "Jun";
            else if (monthNumber == 7) monthName = "Jul";
            else if (monthNumber == 8) monthName = "Aug";
            else if (monthNumber == 9) monthName = "Sep";
            else if (monthNumber == 10) monthName = "Okt";
            else if (monthNumber == 11) monthName = "Nov";
            else if (monthNumber == 12) monthName = "Dez";
            return monthName;
        }

    }
}