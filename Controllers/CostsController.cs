using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using roadwork_portal_service.DAO;
using roadwork_portal_service.Model;
using System.Security.Claims;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CostsController : ControllerBase
    {
        private readonly ILogger<JournalEntriesController> _logger;
        private IConfiguration _configuration;

        public CostsController(ILogger<JournalEntriesController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Update costs.
        /// </summary>
        /// <param name="cost">The feature to update.</param>
        /// <returns></returns>
        [HttpPut]
        [Authorize]
        public ActionResult<Costs> UpdateCostsEntry([FromBody] Costs cost)
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            if (string.IsNullOrEmpty(userUuid))
                return Problem(title: "Unknown user.", statusCode: 500);

            // Check object to insert
            if (string.IsNullOrEmpty(cost?.uuid))
            {
                _logger.LogWarning("No Costs received in update feature method.");
                return BadRequest(new Costs { errorMessage = "SSP-3" });
            }

            // Get the existing feature from db
            CostsDAO costsDAO = new CostsDAO();
            var costsFromDb = costsDAO.GetByUuid(cost.uuid);
            if (costsFromDb == null)
            {
                _logger.LogWarning("Costs entry feature to update not existing.");
                cost.errorMessage = "SSP-3";
                return BadRequest(cost);
            }

            // Check if the user (territorymanager) is the owner
            if (costsFromDb.costs != cost.costs
                || costsFromDb.projectType != cost.projectType
                || costsFromDb.costsComment != cost.costsComment)
            {
                _logger.LogWarning("Only erp number may be changed Costs.");
                return Forbid();
            }

            // Update the feature
            costsFromDb.erpNumber = cost.erpNumber;

            try
            {
                costsDAO.Update(costsFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                costsFromDb.errorMessage = "SSP-3";

                return StatusCode(500, costsFromDb);
            }

            return Ok(costsFromDb);
        }
    }
}