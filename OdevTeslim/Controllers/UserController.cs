using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using OdevTeslim.Models;
using OdevTeslim.DTOs;

namespace Uyg.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        ResultDto result = new ResultDto();
        public UserController(UserManager<AppUser> userManager, IMapper mapper, RoleManager<AppRole> roleManager, IConfiguration configuration, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _configuration = configuration;
            _signInManager = signInManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")] // Sadece Admin'ler erişebilir
        public List<UserDto> List()
        {
            var users = _userManager.Users.ToList();
            var userDtos = _mapper.Map<List<UserDto>>(users);
            return userDtos;
        }
     
        [HttpGet]
        public UserDto GetById(string id)
        {
            var user = _userManager.Users.Where(s => s.Id == id).SingleOrDefault();
            var userDto = _mapper.Map<UserDto>(user);
            return userDto;
        }
        
        [HttpPost]
        public async Task<ResultDto> Add(RegisterDto dto)
        {
            var identityResult = await _userManager.CreateAsync(new() { UserName = dto.UserName, Email = dto.Email, FirstName = dto.FullName, PhoneNumber = dto.PhoneNumber }, dto.Password);

            if (!identityResult.Succeeded)
            {
                result.Status = false;
                foreach (var item in identityResult.Errors)
                {
                    result.Message += "<p>" + item.Description + "<p>";
                }

                return result;
            }
            var user = await _userManager.FindByNameAsync(dto.UserName);
            var roleExist = await _roleManager.RoleExistsAsync("Uye");
            if (!roleExist)
            {
                var role = new AppRole { Name = "Uye" };
                await _roleManager.CreateAsync(role);
            }

            await _userManager.AddToRoleAsync(user, "Uye");
            result.Status = true;
            result.Message = "Üye Eklendi";
            return result;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ResultDto> SignIn(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user is null)
            {
                result.Status = false;
                result.Message = "Üye Bulunamadı!";
                return result;
            }
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordCorrect)
            {
                result.Status = false;
                result.Message = "Kullanıcı Adı veya Parola Geçersiz!";
                return result;
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("JWTID", Guid.NewGuid().ToString()),

            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GenerateJWT(authClaims);

            result.Status = true;
            result.Message = token;
            return result;

        }
        private string GenerateJWT(List<Claim> claims)
        {

            var accessTokenExpiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["AccessTokenExpiration"]));


            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var tokenObject = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    expires: accessTokenExpiration,
                    claims: claims,
                    signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
                );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);

            return token;
        }

        [HttpPut("{id}")] // PUT /api/User/{id}  (Route şemanız /api/[controller]/[action] olduğu için PUT /api/User/UpdateUser/{id} de olabilir)
        [Authorize(Roles = "Admin")] // Sadece Admin kullanıcı güncelleyebilsin
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                result.Status = false;
                result.Message = "Güncellenecek kullanıcı bulunamadı.";
                return NotFound(result);
            }

            // Gelen DTO'daki verilerle kullanıcıyı güncelle
            // AutoMapper kullanıyorsanız: _mapper.Map(dto, user);
            // Manuel güncelleme:
            user.UserName = dto.UserName;
            user.Email = dto.Email;
            user.FirstName = dto.FirstName; // AppUser modelinizdeki property adıyla eşleşmeli
            user.LastName = dto.LastName;   // AppUser modelinizdeki property adıyla eşleşmeli
            user.PhoneNumber = dto.PhoneNumber;
            // Güvenlik nedeniyle Email veya UserName değişiyorsa ek doğrulamalar gerekebilir (örn: EmailConfirmed, SecurityStamp güncelleme)
            // user.NormalizedUserName = _userManager.KeyNormalizer.NormalizeName(dto.UserName); // Gerekebilir
            // user.NormalizedEmail = _userManager.KeyNormalizer.NormalizeEmail(dto.Email); // Gerekebilir

            var identityResult = await _userManager.UpdateAsync(user);

            if (!identityResult.Succeeded)
            {
                result.Status = false;
                result.Message = "Kullanıcı güncellenirken hatalar oluştu:";
                foreach (var error in identityResult.Errors)
                {
                    result.Message += $" {error.Description}";
                }
                return BadRequest(result);
            }

            result.Status = true;
            result.Message = "Kullanıcı başarıyla güncellendi.";
            return Ok(result); // Veya NoContent() (204) dönebilirsiniz.
        }


    }
}

