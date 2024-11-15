using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("RoadWorkActivity/{uuid}/Pdf/")]
    public class PdfOfActivityController : ControllerBase
    {
        private readonly ILogger<PdfOfActivityController> _logger;

        public PdfOfActivityController(ILogger<PdfOfActivityController> logger)
        {
            _logger = logger;
        }

        // GET roadworkactivity/132fa1231/pdf/?docuuid=9737fe34
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult GetPdf(string docUuid)
        {
            docUuid = docUuid.Trim().ToLower();

            if (docUuid != null && docUuid != String.Empty)
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    NpgsqlCommand selectPdfCommand = pgConn.CreateCommand();
                    selectPdfCommand.CommandText = "SELECT document" +
                                " FROM \"wtb_ssp_documents\"" +
                                " WHERE uuid=@doc_uuid";
                    selectPdfCommand.Parameters.AddWithValue("doc_uuid", new Guid(docUuid));

                    NpgsqlDataReader reader = selectPdfCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        byte[] pdfBytes = reader.IsDBNull(0) ?
                                    new byte[0] : (byte[])reader[0];
                        return File(pdfBytes, "application/pdf");
                    }
                }
            }

            _logger.LogError("Could not provide PDF for roadwork activity");
            return BadRequest();
        }

        // POST roadworkactivity/1321231/pdf/
        [HttpPost]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public ActionResult<DocumentAttributes> AddPdf(string uuid, IFormFile pdfFile)
        {
            uuid = uuid.Trim().ToLower();

            if (uuid != null && uuid != String.Empty)
            {
                Guid docUuid = Guid.NewGuid();
                byte[] pdfBytes = new byte[0];

                Stream pdfStream = pdfFile.OpenReadStream();

                using (BinaryReader br = new BinaryReader(pdfStream))
                {
                    pdfBytes = br.ReadBytes((int)pdfStream.Length);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using (NpgsqlTransaction trans = pgConn.BeginTransaction())
                    {
                        NpgsqlCommand updatePdfCommand = pgConn.CreateCommand();
                        updatePdfCommand.CommandText = "INSERT INTO \"wtb_ssp_documents\"" +
                                    "(uuid, roadworkactivity, filename, document) " +
                                    "VALUES (@uuid, @roadworkactivity, @filename, @document)";
                        updatePdfCommand.Parameters.AddWithValue("uuid", docUuid);
                        updatePdfCommand.Parameters.AddWithValue("roadworkactivity", new Guid(uuid));
                        updatePdfCommand.Parameters.AddWithValue("filename", pdfFile.FileName);
                        updatePdfCommand.Parameters.AddWithValue("document", pdfBytes);

                        updatePdfCommand.ExecuteNonQuery();
                        trans.Commit();
                    }

                }

                DocumentAttributes documentAtts = new DocumentAttributes();
                documentAtts.uuid = docUuid.ToString();
                documentAtts.filename = pdfFile.FileName;
                return Ok(documentAtts);
            }

            DocumentAttributes errorObj = new DocumentAttributes();
            errorObj.errorMessage = "Could not update PDF for roadwork activity";
            _logger.LogError("Could not update PDF for roadwork activity");
            return BadRequest(errorObj);
        }

        // DELETE roadworkactivity/1321231/pdf/
        [HttpDelete]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult DeletePdf(string docUuid)
        {
            docUuid = docUuid.Trim().ToLower();

            if (docUuid != null && docUuid != String.Empty)
            {

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using (NpgsqlTransaction trans = pgConn.BeginTransaction())
                    {
                        NpgsqlCommand updatePdfCommand = pgConn.CreateCommand();
                        updatePdfCommand.CommandText = "DELETE FROM \"wtb_ssp_documents\"" +
                                    " WHERE uuid=@doc_uuid";
                        updatePdfCommand.Parameters.AddWithValue("doc_uuid", new Guid(docUuid));

                        updatePdfCommand.ExecuteNonQuery();
                        trans.Commit();
                    }

                }

                return Ok();

            }

            _logger.LogError("Could not delete PDF for a roadwork activity");
            return BadRequest();

        }

    }
}