using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace TestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CsTeckJobController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetCsTeckJob(int E)
        {
            if (E <= 0)
            {
                return BadRequest("Invalid UserId");
            }
            var parameters = new[]
            {
                new SqlParameter("@E", E)
            };

            var executor = new Healper.StoredProcedureExecutor(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build());
            var result = executor.ExecuteStoredProcedure("CsTeckJob", parameters);
            return Ok(result);
        }
    }
}
