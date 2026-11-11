using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Contracts;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(ProfileResponse profile) : ControllerBase
{
    // O perfil é um registro só, então não tem tabela: vem do profile.json, lido na subida.
    [HttpGet]
    public ProfileResponse Get() => profile;
}
