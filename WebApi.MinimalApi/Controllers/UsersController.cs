using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    
    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    [HttpGet("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] object user)
    {
        throw new NotImplementedException();
    }
    
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        
        userRepository.Delete(userId);
        return NoContent();
    }
    
    [HttpHead("{userId}")]
    public IActionResult HeadUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        
        if (user is null)
            return NotFound();
        
        Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
        return Ok();
    }
    
    [HttpGet]
    public IActionResult GetUsersWithPagination([FromQuery] int pageSize, [FromQuery] int pageNumber = 1)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= PageList<UserEntity>.MinPageSize) pageSize = PageList<UserEntity>.MinPageSize;
        if (pageSize > PageList<UserEntity>.MaxPageSize) pageSize = PageList<UserEntity>.MaxPageSize;
        
        var resultPage = userRepository.GetPage(pageNumber, pageSize);
        var users = resultPage.Select(user => user.ToDto());
        var pagination = new Pagination
        {
            PreviousPageLink = resultPage.HasPrevious ? $"api/users/{resultPage.CurrentPage - 1}" : null ,
            NextPageLink = resultPage.HasNext ? $"api/users/{resultPage.CurrentPage + 1}" : null,
            TotalCount = (int)resultPage.TotalCount,
            CurrentPage = resultPage.CurrentPage,
            PageSize = resultPage.PageSize,
            TotalPages = resultPage.TotalPages
        };
        
        Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(pagination));
        
        return Ok(users);
    }
    
    [HttpOptions]
    public IActionResult GetUserOptions()
    {
        Response.Headers.Append("Allow", "POST, GET, OPTIONS");
        return Ok();
    }
}