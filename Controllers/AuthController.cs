using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApplication22.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private string _adminName = "admin";
        private string _password = "12345";
        private readonly IConfiguration _config;
        private List<User> _users = new List<User>();

        public AuthController(IConfiguration config)
        {
            _config = config;
            _users = LoadUsers();
            if (!_users.Any(x => x.Username == _adminName && x.Password == _password))
            {
                _users.Add(new User() { Username = _adminName, Password = _password, IsAdmin = true });
                SaveUsers();
            }
        }

        
        [HttpPost("register")]
        public IActionResult Reg([FromBody] UserRequest model)
        {
            var fitUser = _users.FirstOrDefault(x => x.Username == model.Username && x.Password == model.Password);
            if (fitUser == null)
            {
                var token = GenerateJwtToken(model.Username);
                _users.Add(new User() { Username = model.Username, Password = model.Password, Token = token });
                var isAdmin = false;
                SaveUsers();
                return Ok(new { token, isAdmin });
            }
            return Ok();
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] UserRequest model)
        {
            var fitUser = _users.FirstOrDefault(x => x.Username == model.Username && x.Password == model.Password);
            if (fitUser != null)
            {
                var token = GenerateJwtToken(model.Username);
                fitUser.Token = token;
                foreach(var c in fitUser.Comments)
                {
                    c.Token = token;
                }
                var isAdmin = fitUser.IsAdmin;
                SaveUsers();
                return Ok(new { token, isAdmin });
            }
            return Unauthorized();
        }

        [HttpPost("comment")]
        public IActionResult Comment([FromBody] CommentRequest request)
        {
            var fitUser = _users.FirstOrDefault(x => x.Token == request.Token);
            if (fitUser != null)
            {
                var id = _users.SelectMany(x => x.Comments).Any()
                     ? _users.SelectMany(x => x.Comments).Max(x => x.Id) + 1
                     : 1;
                var newComment = new CommentText()
                {
                    Id = id,
                    Comment = request.Comment,
                    CreatedAt = request.CreatedAt,
                    UserName = fitUser.Username,
                    Token = fitUser.Token,
                };

                
                fitUser.Comments.Add(newComment);

               
                var userIndex = _users.FindIndex(x => x.Token == fitUser.Token);
                if (userIndex != -1)
                {
                    _users[userIndex] = fitUser; 
                }
                SaveUsers();

                var responseComment = new CommentResponse()
                {
                    Id = id,
                    Comment = request.Comment,
                    CreatedAt = request.CreatedAt,
                    UserName = fitUser.Username,
                    Token = fitUser.Token,
                };
                return Ok(responseComment);
            }
            return BadRequest();
        }

        [HttpGet("comments")]
        public IActionResult GetComments()
        {
            var comments = _users.SelectMany(x => x.Comments)
         .Select(c => new CommentResponse
         {
             Id = c.Id,
             Comment = c.Comment,
             CreatedAt = c.CreatedAt,
             UserName = c.UserName,
             Token = c.Token,
         })
         .OrderByDescending(c => c.CreatedAt)  
         .ToList();

            return Ok(comments);
        }


        [HttpDelete("comments/deleteComment")]
        public IActionResult DeleteCommentById([FromBody] DeleteCommentRequest request)
        {
       
            var comment = _users.SelectMany(x => x.Comments)
                   .FirstOrDefault(c => c.Id == request.Id);
            

            if (comment == null)
            {
                return NotFound("Комментарий не найден.");
            }
            else
            {
                var user = _users.FirstOrDefault(x => x.Comments.Any(c => c.Id == request.Id));
                if (user == null) 
                {
                    return NotFound("User не найден.");
                }
                else
                {
                    user.Comments.Remove(comment);
                    SaveUsers();
                }

                var comments = _users.SelectMany(x => x.Comments)
                        .Select(c => new CommentResponse
                        {
                            Id = c.Id,
                            Comment = c.Comment,
                            CreatedAt = c.CreatedAt,
                            UserName = c.UserName,
                            Token = c.Token,
                        })
                        .ToList();

                return Ok(comments);
            }
           
            
        
        }







        private string GenerateJwtToken(string username)
        {
            var key = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("Ошибка: Jwt:Key не найден в конфигурации!");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.Name, username)
    };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            Console.WriteLine("Сгенерированный токен: " + jwt); 

            return jwt;
        }

        private List<User> LoadUsers()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "orders.json");

            var json = System.IO.File.ReadAllText("users.json");
            return JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();
        }


        public void SaveUsers()
        {
            var json = JsonConvert.SerializeObject(_users, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText("users.json", json);
        }

    }




    public class DeleteCommentRequest
    {
        public int Id { get; set; }
    }

    public class UserRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class CommentRequest
    {
        public string Token { get; set; }
        public string Comment { get; set; }

        public string CreatedAt { get; set; }   

    }

    public class CommentResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Comment { get; set; }

        public string CreatedAt { get; set; }
        public string Token { get; set; }
    }

    public class User
    {
        public Boolean IsAdmin { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }

        public List<CommentText> Comments { get; set; } = new List<CommentText>();
    }

    public class CommentText
    {
        public int Id { get; set; }
        public String Comment { get; set; }
         public string CreatedAt { get; set; }

        public string UserName { get; set; }
        public string Token { get; set; }
    }
}
