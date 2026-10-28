using KubePodCreator.Services;
using Microsoft.AspNetCore.Mvc;

namespace KubePodCreator.Controllers;

[ApiController]
[Route("[controller]")]
public class PodCreatorController(PodCreatorService creator) : ControllerBase
{
    [HttpPost("CreateGameServer")]
    public async Task<IpPortDto> CreateGameServer()
    {
        return await creator.CreatePodAndService();
    }
    [HttpGet("Pods")]
    public async Task<IActionResult> ListPods()
    {
        await creator.ListPodsAsync();
        return Ok("Pod names written to the API logs.");
    }
}
