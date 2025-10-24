using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

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
                    UPDATE public.wtb_ssp_config_dates
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
                const string sql = @"
                    UPDATE public.wtb_ssp_config_dates
                    SET attachments  = COALESCE(@attachments,  attachments),
                        acceptance_1 = COALESCE(@acceptance_1, acceptance_1),
                        misc_items   = COALESCE(@misc_items,   misc_items)
                    WHERE sks_no = @sks_no;";

                await using var conn = new NpgsqlConnection(AppConfig.connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@attachments",   (object?)dto.attachments  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@acceptance_1",  (object?)dto.acceptance1  ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@misc_items",    (object?)dto.miscItems    ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sks_no",        sksNo);

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(Problem(title: "Session not found.", statusCode: 404));

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session {SksNo}.", sksNo);
                return Problem(title: "Failed to update session.", detail: ex.Message, statusCode: 500);
            }
        }




        private static async Task<List<SessionDto>> GetSessionsFromDbAsync()
        {
            const string sql = @"
                SELECT planneddate, sks_no, acceptance_1, attachments, misc_items,
                       present_user_ids, distribution_user_ids
                FROM   public.wtb_ssp_config_dates
                WHERE  date_type = 'SKS'
                ORDER  BY planneddate DESC;";

            var result = new List<SessionDto>();

            await using var conn = new NpgsqlConnection(AppConfig.connectionString);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                const int iPlannedDate = 0;
                const int iSksNo       = 1;
                const int iAcceptance1 = 2;
                const int iAttachments = 3;
                const int iMiscItems = 4;
                const int iPresentUserIds = 5;
                const int iDistributionUserIds = 6;


                if (reader.IsDBNull(iPlannedDate)) continue;

                var dto = new SessionDto
                {
                    plannedDate = reader.GetDateTime(iPlannedDate),
                    sksNo = reader.IsDBNull(iSksNo) ? 0L : reader.GetInt64(iSksNo),
                    acceptance1 = reader.IsDBNull(iAcceptance1) ? "-" : reader.GetString(iAcceptance1),
                    attachments = reader.IsDBNull(iAttachments) ? "-" : reader.GetString(iAttachments),
                    miscItems = reader.IsDBNull(iMiscItems) ? "-" : reader.GetString(iMiscItems),
                    presentUserIds = reader.IsDBNull(iPresentUserIds) ? null : reader.GetString(iPresentUserIds),
                    distributionUserIds = reader.IsDBNull(iDistributionUserIds) ? null : reader.GetString(iDistributionUserIds)
                };

                result.Add(dto);
            }

            return result;
        }
    }
}
