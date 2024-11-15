using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("RoadWorkNeed/{uuid}/Pdf/")]
    public class PdfOfNeedController : ControllerBase
    {
        private readonly ILogger<PdfOfNeedController> _logger;

        public PdfOfNeedController(ILogger<PdfOfNeedController> logger)
        {
            _logger = logger;
        }

        // GET roadworkneed/1321231/pdf/?docuuid=676fb676s...
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult GetPdf(string? docUuid)
        {
            if (docUuid == null) docUuid = "";

            docUuid = docUuid.Trim().ToLower();

            if (docUuid != String.Empty)
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

            _logger.LogError("Could not provide PDF for roadwork need");
            return BadRequest();
        }


        // POST roadworkneed/1321231/pdf/
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
                                    "(uuid, roadworkneed, filename, document) " +
                                    "VALUES(@uuid, @roadworkneed, @filename, @document)";
                        updatePdfCommand.Parameters.AddWithValue("uuid", docUuid);
                        updatePdfCommand.Parameters.AddWithValue("roadworkneed", new Guid(uuid));
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
            errorObj.errorMessage = "Could not update PDF for roadwork need";
            _logger.LogError("Could not update PDF for roadwork need");
            return BadRequest(errorObj);
        }

        // DELETE roadworkneed/1321231/pdf/?docuuid=72623723...
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

            _logger.LogError("Could not delete PDF for a roadwork need");
            return BadRequest();

        }

    }
}