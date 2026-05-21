using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using roadwork_portal_service.DAO;
using roadwork_portal_service.Model;
using System.Security.Claims;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JournalEntriesController : ControllerBase
    {
        private readonly ILogger<JournalEntriesController> _logger;
        private IConfiguration _configuration;

        public JournalEntriesController(ILogger<JournalEntriesController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get a collection of journal entries.
        /// </summary>
        /// <param name="roadWorkActivityUuid">Filter by road work activity (uuid).</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public ActionResult<IEnumerable<JournalEntryFeature>> GetJournalEntries(string roadWorkActivityUuid = "")
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            // Get the features from db
            JournalEntryDAO journalEntryDAO = new JournalEntryDAO();
            var journalEntries = journalEntryDAO.GetByActivityUuid(roadWorkActivityUuid);

            // Update edit permission property
            if (User.IsInRole("administrator"))
            {
                // administrator is allowed to edit all entries
                journalEntries.ForEach(elem => elem.properties.isEditingAllowed = true);
            }
            else if (User.IsInRole("territorymanager") && !string.IsNullOrEmpty(userUuid))
            {
                // territorymanager is allowed to edit own entries
                journalEntries.ForEach(elem => elem.properties.isEditingAllowed = elem.properties.createdBy == userUuid);
            }

            return Ok(journalEntries);
        }

        /// <summary>
        /// Insert a journal entry.
        /// </summary>
        /// <param name="journalEntryFeature">The feature to insert.</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public ActionResult<JournalEntryFeature> InsertJournalEntry([FromBody] JournalEntryFeature journalEntryFeature)
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            if (string.IsNullOrEmpty(userUuid))
                return Problem(title: "Unknown user.", statusCode: 500);

            // Check object to insert
            if (journalEntryFeature?.properties == null)
            {
                _logger.LogWarning("No journal entry feature received in insert feature method.");
                return Ok(new JournalEntryFeature { errorMessage = "SSP-3" });
            }

            if (string.IsNullOrEmpty(journalEntryFeature?.properties?.uuidRoadworkActivity))
            {
                _logger.LogWarning("No uuid roadwork activity received in insert feature method.");
                return Ok(new JournalEntryFeature { errorMessage = "SSP-3" });
            }


            // Set new uid, current user and date
            journalEntryFeature.properties.uuid = Guid.NewGuid().ToString();
            journalEntryFeature.properties.createdBy = userUuid;
            journalEntryFeature.properties.created = DateTime.Now;
            journalEntryFeature.properties.lastModified = DateTime.Now;

            try
            {
                // Insert into db
                JournalEntryDAO journalEntryDAO = new JournalEntryDAO();
                journalEntryDAO.Insert(journalEntryFeature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                journalEntryFeature.errorMessage = "SSP-3";

                return Ok(journalEntryFeature);
            }

            return Ok(journalEntryFeature);
        }

        /// <summary>
        /// Update a journal entry.
        /// </summary>
        /// <param name="journalEntryFeature">The feature to update.</param>
        /// <returns></returns>
        [HttpPut]
        [Authorize]
        public ActionResult<JournalEntryFeature> UpdateJournalEntry([FromBody] JournalEntryFeature journalEntryFeature)
        {
            var userUuid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check permission
            if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                return Forbid();

            if (string.IsNullOrEmpty(userUuid))
                return Problem(title: "Unknown user.", statusCode: 500);

            // Check object to insert
            if (string.IsNullOrEmpty(journalEntryFeature?.properties.uuid))
            {
                _logger.LogWarning("No journal entry feature received in update feature method.");
                return Ok(new JournalEntryFeature { errorMessage = "SSP-3" });
            }

            // Get the existing feature from db
            JournalEntryDAO journalEntryDAO = new JournalEntryDAO();
            var journalEntryFromDb = journalEntryDAO.GetByUuid(journalEntryFeature.properties.uuid);
            if (journalEntryFromDb == null)
            {
                _logger.LogWarning("Journal entry feature to update not existing.");
                return Ok(new JournalEntryFeature { errorMessage = "SSP-3" });
            }

            // Check if the user (territorymanager) is the owner
            if (User.IsInRole("territorymanager") && journalEntryFromDb?.properties?.createdBy != userUuid)
                return Forbid();

            // Update the feature
            journalEntryFromDb.properties.content = journalEntryFeature.properties.content;
            journalEntryFromDb.properties.lastModified = DateTime.Now;

            try
            {
                journalEntryDAO.Update(journalEntryFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                journalEntryFromDb.errorMessage = "SSP-3";

                return Ok(journalEntryFromDb);
            }

            return Ok(journalEntryFromDb);
        }
    }
}