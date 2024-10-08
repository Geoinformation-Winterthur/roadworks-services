using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;

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

        // GET roadworkactivity/1321231/pdf/
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult GetPdf(string uuid)
        {
            uuid = uuid.Trim().ToLower();

            if (uuid != null && uuid != String.Empty)
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    NpgsqlCommand selectPdfCommand = pgConn.CreateCommand();
                    selectPdfCommand.CommandText = "SELECT pdf_document" +
                                " FROM \"wtb_ssp_roadworkactivities\"" +
                                " WHERE uuid=@uuid";
                    selectPdfCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

                    NpgsqlDataReader reader = selectPdfCommand.ExecuteReader();
                    if (reader.Read())
                    {
                        byte[] pdfBytes = reader.IsDBNull(0) ?
                                    new byte[0] : (byte[])reader[0];
                        return File(pdfBytes, "application/pdf");
                    }
                }
            }

            _logger.LogError("Could not provide PDF for roadworkneed");
            return BadRequest();
        }


        // POST roadworkactivity/1321231/pdf/
        [HttpPost]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult AddPdf(string uuid, IFormFile pdfFile)
        {
            uuid = uuid.Trim().ToLower();

            if (uuid != null && uuid != String.Empty)
            {
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
                        updatePdfCommand.CommandText = "UPDATE \"wtb_ssp_roadworkactivities\"" +
                                    " SET pdf_document=@pdf_document" +
                                    " WHERE uuid=@uuid";
                        updatePdfCommand.Parameters.AddWithValue("pdf_document", pdfBytes);
                        updatePdfCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

                        updatePdfCommand.ExecuteNonQuery();
                        trans.Commit();
                    }

                }

                return Ok();

            }

            _logger.LogError("Could not update PDF for roadworkneed");
            return BadRequest();

        }

        // DELETE roadworkactivity/1321231/pdf/
        [HttpDelete]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IActionResult DeletePdf(string uuid)
        {
            uuid = uuid.Trim().ToLower();

            if (uuid != null && uuid != String.Empty)
            {

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using (NpgsqlTransaction trans = pgConn.BeginTransaction())
                    {
                        NpgsqlCommand updatePdfCommand = pgConn.CreateCommand();
                        updatePdfCommand.CommandText = "UPDATE \"wtb_ssp_roadworkactivities\"" +
                                    " SET pdf_document=@pdf_document" +
                                    " WHERE uuid=@uuid";
                        updatePdfCommand.Parameters.AddWithValue("pdf_document", DBNull.Value);
                        updatePdfCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

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