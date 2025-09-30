using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private static readonly Regex AllowedLoginRegex = MyRegex();
    
    public UsersController(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    [HttpGet("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult GetUserById([FromRoute] Guid userId, [FromHeader(Name = "Accept")] string acceptHeader)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userDto = user.ToDto();

        if (acceptHeader.Contains("application/json"))
        {
            return new JsonResult(userDto)
            {
                StatusCode = 200,
                ContentType = "application/json; charset=utf-8"
            };
        }

        if (!acceptHeader.Contains("application/xml"))
            return StatusCode(406);

        var xmlSerializer = new XmlSerializer(typeof(UserDto));
        using var stringWriter = new StringWriter();
        xmlSerializer.Serialize(stringWriter, userDto);
        var xml = stringWriter.ToString();
        return Content(xml, "application/xml; charset=utf-8");

    }

    [HttpPost] 
    [Produces("application/json")]
    [HttpPost]
    public IActionResult CreateUser([FromBody] object? body)
    {
        var acceptHeader = Request.Headers.Accept.FirstOrDefault() ?? "*/*";
        if (acceptHeader.Contains("text/plain", StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status406NotAcceptable);

        if (Request.ContentLength == 0 || body == null)
            return StatusCode(StatusCodes.Status400BadRequest);

        JObject? jsonObj;
        switch (body)
        {
            case JObject jo:
                jsonObj = jo;
                break;
            case JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }:
                return StatusCode(StatusCodes.Status400BadRequest);
            case JsonElement je:
                jsonObj = JObject.Parse(je.GetRawText());
                break;
            default:
                jsonObj = JObject.FromObject(body);
                break;
        }

        var login = jsonObj.GetValue("login")?.ToString();
        var firstName = jsonObj.GetValue("firstName")?.ToString();
        var lastName = jsonObj.GetValue("lastName")?.ToString();
        
        var newUser = new UserEntity(Guid.Empty)
        {
            GamesPlayed = 0,
            CurrentGameId = null
        };

        if (string.IsNullOrWhiteSpace(login) || !AllowedLoginRegex.IsMatch(login))
        {
            newUser.Login = "Login is required";
            return UnprocessableEntity(newUser);
        }
        if (string.IsNullOrWhiteSpace(firstName))
            newUser.FirstName = "John";
        if (string.IsNullOrWhiteSpace(lastName))
            newUser.LastName = "Doe";

        var inserted = userRepository.Insert(newUser);
        var idString = inserted.Id.ToString();
        var location = $"{Request.Path.Value?.TrimEnd('/')}/{idString}";
        Response.Headers.Location = location;

        if (acceptHeader.Contains("application/xml"))
        {
            return new ContentResult
            {
                Content = $"<guid>{idString}</guid>",
                ContentType = "application/xml; charset=utf-8",
                StatusCode = StatusCodes.Status201Created
            };
        }

        return new ContentResult
        {
            Content = JsonConvert.SerializeObject(idString),
            ContentType = "application/json; charset=utf-8",
            StatusCode = StatusCodes.Status201Created
        };
    }

    [GeneratedRegex(@"^[A-Za-z0-9_\-]+$")]
    private static partial Regex MyRegex();
}