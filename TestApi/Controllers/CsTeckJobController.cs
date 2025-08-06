using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TestApi.Healper;
using TestApi.Parameters;

namespace TestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CsTeckJobController : ControllerBase
    {
        private readonly SqlHelper _sqlHelper;
        public CsTeckJobController(SqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper ?? throw new ArgumentNullException(nameof(sqlHelper));
        }
        [HttpGet]
        public async Task<IActionResult> GetCsTeckJob(int E)
        {
            if (E <= 0)
                return BadRequest("Invalid UserId");

            var parameters = new[]
            {
                new SqlParameter("@E", E)
            };

            var result = await _sqlHelper.ExecuteStoredProcedureAsync("CsTeckJob", parameters);

            return Ok(result); // ← هذا الآن سيرجع JSON سليم كمصفوفة من الكائنات
        }
        [HttpPost("InsertAA")]
        public async Task<IActionResult> InsertAA([FromBody] AAInsertModel model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var data = model.GetType()
                        .GetProperties()
                        .ToDictionary(p => p.Name, p => p.GetValue(model) ?? DBNull.Value);
            try
            {
                await _sqlHelper.ExecuteInsertAsync("AA", data);
                return Ok("Inserted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("updateAA")]
        public async Task<IActionResult> UpdateAA([FromBody] AAInsertModel model, [FromHeader] int Key)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var data = model.GetType()
                        .GetProperties()
                        .ToDictionary(p => p.Name, p => p.GetValue(model) ?? DBNull.Value);

            try
            {
                string keyColumn = "IID"; // ← غيرها حسب اسم العمود الأساسي في جدول AA
                await _sqlHelper.ExecuteUpdateAsync("AA", data, keyColumn, Key);

                return Ok("Updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("deleteAA")]
        public async Task<IActionResult> DeleteAA([FromHeader] int key)
        {
            if (key == 0)
                return BadRequest("Invalid key");

            try
            {
                string keyColumn = "IID"; // ← غيرها حسب اسم العمود الأساسي في جدول AA
                await _sqlHelper.ExecuteDeleteAsync("AA", keyColumn, key);
                return Ok("Deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


    }
}
