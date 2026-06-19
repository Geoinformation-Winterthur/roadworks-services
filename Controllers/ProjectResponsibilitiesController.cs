using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using roadwork_portal_service.DAO;
using roadwork_portal_service.Model;
using System.Security.Claims;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ActivityResponsibilitiesController : ControllerBase
    {
        private readonly ILogger<ActivityResponsibilitiesController> _logger;
        private IConfiguration _configuration;

        public ActivityResponsibilitiesController(ILogger<ActivityResponsibilitiesController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get the project lead resonsibility of an activity.
        /// </summary>
        /// <param name="roadWorkActivityUuid">Filter by road work activity (uuid).</param>
        /// <returns></returns>
        [HttpGet("project-lead")]
        [Authorize]
        public ActionResult<ActivityResponsibilityFeature> GetActivityProjectResponsibility(string roadWorkActivityUuid)
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            // Check activity uuid
            if (string.IsNullOrEmpty(roadWorkActivityUuid))
            {
                _logger.LogWarning("No activity uuid provided to get project responsibility.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            // Get the features from db
            ActivityResponsibilityDAO activityResponsibilityDAO = new ActivityResponsibilityDAO();
            return Ok(activityResponsibilityDAO.GetProjectLeadByUuidActivity(roadWorkActivityUuid));
        }

        /// <summary>
        /// Get a collection of the phase leads of an activity.
        /// </summary>
        /// <param name="roadWorkActivityUuid">Filter by road work activity (uuid).</param>
        /// <returns></returns>
        [HttpGet("phase-leads")]
        [Authorize]
        public ActionResult<IEnumerable<ActivityResponsibilityFeature>> GetActivityPhaseResponsibilities(string roadWorkActivityUuid)
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            // Check activity uuid
            if (string.IsNullOrEmpty(roadWorkActivityUuid))
            {
                _logger.LogWarning("No activity uuid provided to get phase responsibilities.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            // Get the features from db
            ActivityResponsibilityDAO activityResponsibilityDAO = new ActivityResponsibilityDAO();
            return Ok(activityResponsibilityDAO.GetPhaseLeadsByUuidActivity(roadWorkActivityUuid));
        }

        /// <summary>
        /// Insert a journal entry.
        /// </summary>
        /// <param name="activityResponsibilityFeature">The feature to insert.</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult<ActivityResponsibilityFeature> InsertActivityResponsibility([FromBody] ActivityResponsibilityFeature activityResponsibilityFeature)
        {
            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            // Check object to insert
            if (activityResponsibilityFeature?.properties == null)
            {
                _logger.LogWarning("No journal entry feature received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidRoadworkActivity))
            {
                _logger.LogWarning("No uuid roadwork activity received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidOrganisationalUnit))
            {
                _logger.LogWarning("No uuid organisational unit received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidUser))
            {
                _logger.LogWarning("No uuid user received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (activityResponsibilityFeature.properties.responsibilityType == null)
            {
                _logger.LogWarning("Invalid responsibility type in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (activityResponsibilityFeature.properties.responsibilityType == ResponsibilityType.PhaseLead
                && string.IsNullOrEmpty(activityResponsibilityFeature.properties.phase))
            {
                _logger.LogWarning("Invalid phase for responsibility type \"PhaseLead\" type in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            // Set new uid
            activityResponsibilityFeature.properties.uuid = Guid.NewGuid().ToString();

            // Phase must be empty for type ProjectLead
            if (activityResponsibilityFeature.properties.responsibilityType == ResponsibilityType.ProjectLead)
            {
                activityResponsibilityFeature.properties.phase = string.Empty;
            }

            try
            {
                // Insert into db
                ActivityResponsibilityDAO activityResponsibilityDAO = new ActivityResponsibilityDAO();
                activityResponsibilityDAO.Insert(activityResponsibilityFeature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                activityResponsibilityFeature.errorMessage = "SSP-3";

                return StatusCode(500, activityResponsibilityFeature);
            }

            return Ok(activityResponsibilityFeature);
        }

        /// <summary>
        /// Update a journal entry.
        /// </summary>
        /// <param name="activityResponsibilityFeature">The feature to update.</param>
        /// <returns></returns>
        [HttpPut]
        [Authorize]
        public ActionResult<ActivityResponsibilityFeature> UpdateActivityResponsibility([FromBody] ActivityResponsibilityFeature activityResponsibilityFeature)
        {
            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            // Check object to insert
            if (string.IsNullOrEmpty(activityResponsibilityFeature?.properties?.uuid))
            {
                _logger.LogWarning("No roadwork activity entry feature received in update feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidRoadworkActivity))
            {
                _logger.LogWarning("No uuid roadwork activity received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidOrganisationalUnit))
            {
                _logger.LogWarning("No uuid organisational unit received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(activityResponsibilityFeature.properties.uuidUser))
            {
                _logger.LogWarning("No uuid user received in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (activityResponsibilityFeature.properties.responsibilityType == null)
            {
                _logger.LogWarning("Invalid responsibility type in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            if (activityResponsibilityFeature.properties.responsibilityType == ResponsibilityType.PhaseLead
                && string.IsNullOrEmpty(activityResponsibilityFeature.properties.phase))
            {
                _logger.LogWarning("Invalid phase for responsibility type \"PhaseLead\" type in insert feature method.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            // Get the existing feature from db
            ActivityResponsibilityDAO activityResponsibilityDAO = new ActivityResponsibilityDAO();
            var activityResponsibilityFromDb = activityResponsibilityDAO.GetByUuid(activityResponsibilityFeature.properties.uuid);
            if (activityResponsibilityFromDb == null)
            {
                _logger.LogWarning("Activity responsibility entry feature to update not existing.");
                return BadRequest(new ActivityResponsibilityFeature { errorMessage = "SSP-3" });
            }

            // Update the feature
            activityResponsibilityFromDb.properties.uuidOrganisationalUnit = activityResponsibilityFeature.properties.uuidOrganisationalUnit;
            activityResponsibilityFromDb.properties.uuidUser = activityResponsibilityFeature.properties.uuidUser;

            // responsibilityType may not be changed!
            if (activityResponsibilityFromDb.properties.responsibilityType == activityResponsibilityFeature.properties.responsibilityType)
            {
                if (activityResponsibilityFromDb.properties.responsibilityType == ResponsibilityType.PhaseLead)
                {
                    activityResponsibilityFromDb.properties.phase = activityResponsibilityFeature.properties.phase;
                    activityResponsibilityFromDb.properties.sortOrder = activityResponsibilityFeature.properties.sortOrder;
                }
                else
                {
                    activityResponsibilityFromDb.properties.phase = string.Empty;
                }
            }

            try
            {
                activityResponsibilityDAO.Update(activityResponsibilityFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                activityResponsibilityFromDb.errorMessage = "SSP-3";

                return StatusCode(500, activityResponsibilityFromDb);
            }

            return Ok(activityResponsibilityFromDb);
        }
    }
}