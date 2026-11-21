using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using roadwork_portal_service.Constants;
using roadwork_portal_service.DAO;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers.RoadWorkNeed
{
    /// <summary>
    /// Controller to clone needs.
    /// </summary>
    [ApiController]
    [Route(ApiRoutes.RoadWorkNeed)]
    public class RoadWorkNeedCloneController : ControllerBase
    {
        private readonly ILogger<RoadWorkNeedCloneController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkNeedCloneController(ILogger<RoadWorkNeedCloneController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        /// <summary>
        /// Create a new need (isMinorWork = false) from an existing need of subtype minor work (isMinorWork = true).
        /// </summary>
        /// <param name="uuid"></param>
        /// <returns></returns>
        [HttpPost("{uuid}/create-need-from-minor-work")]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<CreatedResourceResult> CreateNeedFromMinorWork(Guid uuid)
        {
            try
            {
                RoadWorkNeedDAO roadWorkNeedDAO = new RoadWorkNeedDAO(false);
                var newNeedUuid = roadWorkNeedDAO.CreateNeedFromMinorWork(uuid);
                if (newNeedUuid == null)
                {
                    var errorResult = new CreatedResourceResult()
                    {
                        errorMessage = "SSP-101"
                    };

                    return Ok(errorResult); // Ok required because of frontend behaviour
                }

                var createdResourceResult = new CreatedResourceResult()
                {
                    uuid = newNeedUuid.Value
                };

                return Ok(createdResourceResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                var errorResult = new CreatedResourceResult()
                {
                    errorMessage = "SSP-101"
                };

                return Ok(errorResult); // Ok required because of frontend behaviour
            }
        }
    }
}
