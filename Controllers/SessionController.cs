using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;
using System.Globalization;
using NpgsqlTypes;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(Roles = "administrator, territorymanager")]
    public class SessionController : ControllerBase
    {
        private readonly ILogger<SessionController> _logger;

        public SessionController(ILogger<SessionController> logger)
        {
            _logger = logger;
        }

        /// <summary>Retrieves all SKS sessions of Winterthur.</summary>
        /// <response code="200">Array of sessions ordered by planned date (desc).</response>
        /// <response code="500">Problem details when an error occurs.</response>
        [HttpGet]
        [Authorize(Roles = "administrator, territorymanager")]
        [ProducesResponseType(typeof(List<SessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<SessionDto>>> GetSessions()
        {
            try
            {
                var sessions = await GetSessionsFromDbAsync();
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load sessions.");
                return Problem(title: "Failed to load sessions.", detail: ex.Message, statusCode: 500);
            }
        }

        [HttpPatch("{sksNo:long}/users")]
        [Authorize(Roles = "administrator, territorymanager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSessionUsersAsync(
            [FromRoute] long sksNo,
            [FromBody] UpdateSessionUsersDto dto)
        {
            try
            {
                const string sql = @"
                    UPDATE wtb_ssp_config_dates
                    SET present_user_ids      = COALESCE(@present, present_user_ids),
                        distribution_user_ids = COALESCE(@distribution, distribution_user_ids)
                    WHERE sks_no = @sks_no;";

                await using var conn = new NpgsqlConnection(AppConfig.connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@present", (object?)dto.presentUserIds ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@distribution", (object?)dto.distributionUserIds ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sks_no", sksNo);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(Problem(title: "Session not found.", statusCode: 404));

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user lists for session {SksNo}.", sksNo);
                return Problem(title: "Failed to update user lists.", detail: ex.Message, statusCode: 500);
            }
        }

        [HttpPatch("{sksNo:long}")]
        [Authorize(Roles = "administrator, territorymanager")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateSessionDetailsAsync(
            [FromRoute] long sksNo,
            [FromBody] UpdateSessionDetailsDto dto)
        {
            try
            {
                DateTime? plannedDate = null;
                if (!string.IsNullOrWhiteSpace(dto.plannedDateForBackend))
                {
                    plannedDate = DateTime.ParseExact(
                        dto.plannedDateForBackend,
                        "dd.MM.yyyy",
                        CultureInfo.GetCultureInfo("de-CH"));

                    plannedDate = DateTime.SpecifyKind(plannedDate.Value, DateTimeKind.Unspecified);
                }

                var reportType = string.IsNullOrWhiteSpace(dto.reportType) ? null : dto.reportType;
                if (reportType is not null && reportType is not ("PRE_PROTOCOL" or "PROTOCOL"))
                    return Problem(title: "Invalid report_type.", detail: "Use PRE_PROTOCOL or PROTOCOL.", statusCode: 400);

                const string sql = @"
                    UPDATE wtb_ssp_config_dates
                    SET attachments    = COALESCE(@attachments, attachments),
                        acceptance_1   = COALESCE(@acceptance_1, acceptance_1),
                        misc_items     = COALESCE(@misc_items, misc_items),
                        planneddate    = COALESCE(@planneddate, planneddate),
                        report_type    = COALESCE(CAST(@report_type AS report_type_enum), report_type),
                        location       = COALESCE(@location, location),
                        time_window    = COALESCE(@time_window, time_window),
                        chairperson    = COALESCE(@chairperson, chairperson),
                        minute_taker   = COALESCE(@minute_taker, minute_taker)
                    WHERE sks_no = @sks_no;";

                await using var conn = new NpgsqlConnection(AppConfig.connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("@attachments", (object?)dto.attachments ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@acceptance_1", (object?)dto.acceptance1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@misc_items", (object?)dto.miscItems ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object?)dto.location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@time_window", (object?)dto.timeWindow ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chairperson", (object?)dto.chairperson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@minute_taker", (object?)dto.minuteTaker ?? DBNull.Value);

                var pDate = cmd.Parameters.Add("@planneddate", NpgsqlDbType.Date);
                pDate.Value = (object?)plannedDate ?? DBNull.Value;

                cmd.Parameters.AddWithValue("@report_type", (object?)dto.reportType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sks_no", sksNo);

                var rows = await cmd.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    return NotFound(Problem(
                        title: "Session not found.",
                        detail: $"No record found for SKS number {sksNo}.",
                        statusCode: 404));
                }

                _logger.LogInformation("Session {SksNo} updated successfully.", sksNo);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session {SksNo}.", sksNo);
                return Problem(title: "Failed to update session.", detail: ex.Message, statusCode: 500);
            }
        }

        [HttpPost]
        [Authorize(Roles = "administrator, territorymanager")]
        [ProducesResponseType(typeof(SessionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SessionDto>> CreateSessionAsync([FromBody] CreateSessionDto dto)
        {
            try
            {
                const string sql = @"
                    INSERT INTO wtb_ssp_config_dates
                        (
                            sks_no,
                            date_type,
                            planneddate,
                            acceptance_1,
                            attachments,
                            misc_items,
                            present_user_ids,
                            distribution_user_ids,
                            location,
                            time_window,
                            chairperson,
                            minute_taker
                        )
                    VALUES
                        (
                            @sks_no,
                            'SKS',
                            @planneddate,
                            COALESCE(@acceptance_1, 'Das Protokoll wird ohne Anmerkungen verdankt.'),
                            COALESCE(@attachments, 'Keine'),
                            COALESCE(@misc_items, 'Keine'),
                            COALESCE(@present, ''),
                            COALESCE(@distribution, ''),
                            COALESCE(@location, ''),
                            COALESCE(@time_window, ''),
                            COALESCE(@chairperson, ''),
                            COALESCE(@minute_taker, '')
                        )
                    RETURNING
                        planneddate,
                        sks_no,
                        acceptance_1,
                        attachments,
                        misc_items,
                        present_user_ids,
                        distribution_user_ids,
                        location,
                        time_window,
                        chairperson,
                        minute_taker,
                        report_type,
                        date_type;";

                await using var conn = new NpgsqlConnection(AppConfig.connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                DateTime? plannedDate = null;
                if (!string.IsNullOrWhiteSpace(dto.plannedDateForBackend))
                {
                    plannedDate = DateTime.ParseExact(
                        dto.plannedDateForBackend,
                        "dd.MM.yyyy",
                        CultureInfo.GetCultureInfo("de-CH"));

                    plannedDate = DateTime.SpecifyKind(plannedDate.Value, DateTimeKind.Unspecified);
                }

                cmd.Parameters.AddWithValue("@sks_no", dto.sksNo);

                var pDate = cmd.Parameters.Add("@planneddate", NpgsqlDbType.Date);
                pDate.Value = (object?)plannedDate ?? DBNull.Value;

                cmd.Parameters.AddWithValue("@acceptance_1", (object?)dto.acceptance1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@attachments", (object?)dto.attachments ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@misc_items", (object?)dto.miscItems ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@present", (object?)dto.presentUserIds ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@distribution", (object?)dto.distributionUserIds ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object?)dto.location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@time_window", (object?)dto.timeWindow ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chairperson", (object?)dto.chairperson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@minute_taker", (object?)dto.minuteTaker ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return Problem(title: "Insert failed.", statusCode: 500);

                var result = new SessionDto
                {
                    sksNo = reader.GetInt64(reader.GetOrdinal("sks_no")),
                    plannedDate = reader.GetDateTime(reader.GetOrdinal("planneddate")),
                    acceptance1 = reader.IsDBNull(reader.GetOrdinal("acceptance_1")) ? "" : reader.GetString(reader.GetOrdinal("acceptance_1")),
                    attachments = reader.IsDBNull(reader.GetOrdinal("attachments")) ? "" : reader.GetString(reader.GetOrdinal("attachments")),
                    miscItems = reader.IsDBNull(reader.GetOrdinal("misc_items")) ? "" : reader.GetString(reader.GetOrdinal("misc_items")),
                    presentUserIds = reader.IsDBNull(reader.GetOrdinal("present_user_ids")) ? "" : reader.GetString(reader.GetOrdinal("present_user_ids")),
                    distributionUserIds = reader.IsDBNull(reader.GetOrdinal("distribution_user_ids")) ? "" : reader.GetString(reader.GetOrdinal("distribution_user_ids")),
                    location = reader.IsDBNull(reader.GetOrdinal("location")) ? "" : reader.GetString(reader.GetOrdinal("location")),
                    timeWindow = reader.IsDBNull(reader.GetOrdinal("time_window")) ? "" : reader.GetString(reader.GetOrdinal("time_window")),
                    chairperson = reader.IsDBNull(reader.GetOrdinal("chairperson")) ? "" : reader.GetString(reader.GetOrdinal("chairperson")),
                    minuteTaker = reader.IsDBNull(reader.GetOrdinal("minute_taker")) ? "" : reader.GetString(reader.GetOrdinal("minute_taker")),
                    reportType = reader.IsDBNull(reader.GetOrdinal("report_type")) ? "" : reader.GetString(reader.GetOrdinal("report_type")),
                    dateType = reader.IsDBNull(reader.GetOrdinal("date_type")) ? "" : reader.GetString(reader.GetOrdinal("date_type")),
                };

                return Created($"/Session/{result.sksNo}", result);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "23505")
            {
                return Problem(
                    title: "SKS-Nr ist bereits vergeben.",
                    detail: "SqlState == 23505",
                    statusCode: StatusCodes.Status409Conflict
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create session.");
                return Problem(title: "Failed to create session.", detail: ex.Message, statusCode: 500);
            }
        }

        private static async Task<List<SessionDto>> GetSessionsFromDbAsync()
        {
            const string sql = @"
                SELECT planneddate,
                       sks_no,
                       acceptance_1,
                       attachments,
                       misc_items,
                       present_user_ids,
                       distribution_user_ids,
                       report_type,
                       date_type,
                       location,
                       time_window,
                       chairperson,
                       minute_taker
                FROM   wtb_ssp_config_dates
                ORDER  BY planneddate DESC;";

            var result = new List<SessionDto>();

            await using var conn = new NpgsqlConnection(AppConfig.connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                const int iPlannedDate = 0;
                const int iSksNo = 1;
                const int iAcceptance1 = 2;
                const int iAttachments = 3;
                const int iMiscItems = 4;
                const int iPresentUserIds = 5;
                const int iDistributionUserIds = 6;
                const int iReportType = 7;
                const int iDateType = 8;
                const int iLocation = 9;
                const int iTimeWindow = 10;
                const int iChairperson = 11;
                const int iMinuteTaker = 12;

                if (reader.IsDBNull(iPlannedDate)) continue;

                var dto = new SessionDto
                {
                    plannedDate = reader.GetDateTime(iPlannedDate),
                    sksNo = reader.IsDBNull(iSksNo) ? 0L : reader.GetInt64(iSksNo),
                    acceptance1 = reader.IsDBNull(iAcceptance1) ? "-" : reader.GetString(iAcceptance1),
                    attachments = reader.IsDBNull(iAttachments) ? "-" : reader.GetString(iAttachments),
                    miscItems = reader.IsDBNull(iMiscItems) ? "-" : reader.GetString(iMiscItems),
                    presentUserIds = reader.IsDBNull(iPresentUserIds) ? null : reader.GetString(iPresentUserIds),
                    distributionUserIds = reader.IsDBNull(iDistributionUserIds) ? null : reader.GetString(iDistributionUserIds),
                    reportType = reader.IsDBNull(iReportType) ? "-" : reader.GetString(iReportType),
                    dateType = reader.IsDBNull(iDateType) ? "-" : reader.GetString(iDateType),
                    location = reader.IsDBNull(iLocation) ? "" : reader.GetString(iLocation),
                    timeWindow = reader.IsDBNull(iTimeWindow) ? "" : reader.GetString(iTimeWindow),
                    chairperson = reader.IsDBNull(iChairperson) ? "" : reader.GetString(iChairperson),
                    minuteTaker = reader.IsDBNull(iMinuteTaker) ? "" : reader.GetString(iMinuteTaker),
                };

                result.Add(dto);
            }

            return result;
        }
    }
}