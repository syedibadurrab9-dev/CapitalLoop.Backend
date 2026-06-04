using Application.Portfolios.Commands.UpdateKycPayload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    public class KycController : BaseApiController
    {
        [HttpPost("submit-documents")]
        public async Task<IActionResult> SubmitKycDocuments([FromBody] UpdateKycPayloadCommand command, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(command, cancellationToken);
            if (!result) return NotFound("User identity baseline profile was not discovered.");
            return Ok(new { Status = "PendingReview", Message = "Compliance metrics filed successfully." });
        }
    }
}
