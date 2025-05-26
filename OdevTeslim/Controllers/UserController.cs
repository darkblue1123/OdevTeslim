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
        [AllowAnonymous]
        public async Task<ResultDto> Add(RegisterDto dto)
        {
            var result = new ResultDto();
            if (!ModelState.IsValid) // SelectedRole artık DTO'da olduğu için ModelState bunu da kontrol eder
            {
                result.Status = false;
                result.Message = ""; // Hata mesajlarını birleştirmek için boş başlat
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    result.Message += $"<p>{error.ErrorMessage}</p>";
                }
                return result; // BadRequest(ModelState) de kullanılabilir, o zaman JavaScript tarafında farklı parse etmek gerekir.
            }

            // Seçilen rolün geçerli olup olmadığını kontrol et (isteğe bağlı ama önerilir)
            if (dto.SelectedRole != "Student" && dto.SelectedRole != "Teacher")
            {
                result.Status = false;
                result.Message = "<p>Geçersiz kayıt türü seçildi. Lütfen 'Student' veya 'Teacher' seçin.</p>";
                return result;
            }

            var identityResult = await _userManager.CreateAsync(new AppUser() { UserName = dto.UserName, Email = dto.Email, FirstName = dto.FullName, PhoneNumber = dto.PhoneNumber }, dto.Password);

            if (!identityResult.Succeeded)
            {
                result.Status = false;
                result.Message = ""; // Hata mesajlarını birleştirmek için boş başlat
                foreach (var item in identityResult.Errors)
                {
                    result.Message += "<p>" + item.Description + "</p>";
                }
                return result;
            }

            var user = await _userManager.FindByNameAsync(dto.UserName);
            if (user == null)
            {
                result.Status = false;
                result.Message = "<p>Kullanıcı oluşturuldu ancak sistemsel bir hata nedeniyle bulunamadı.</p>";
                // Belki burada oluşturulan kullanıcıyı silmek gerekebilir (rollback logic)
                // await _userManager.DeleteAsync(await _userManager.FindByNameAsync(dto.UserName)); // Çok dikkatli kullanılmalı
                return result;
            }

            // Seçilen rol var mı kontrol et, yoksa oluştur (Student ve Teacher rolleri için)
            if (!await _roleManager.RoleExistsAsync(dto.SelectedRole))
            {
                var appRole = new AppRole { Name = dto.SelectedRole };
                var roleCreationResult = await _roleManager.CreateAsync(appRole);
                if (!roleCreationResult.Succeeded)
                {
                    result.Status = false;
                    result.Message = $"<p>'{dto.SelectedRole}' rolü oluşturulurken bir hata meydana geldi.</p>";
                    // Oluşturulan kullanıcıyı silmek iyi bir pratik olabilir
                    await _userManager.DeleteAsync(user);
                    return result;
                }
            }

            // Kullanıcıya seçilen rolü ata
            var addToRoleResult = await _userManager.AddToRoleAsync(user, dto.SelectedRole);
            if (!addToRoleResult.Succeeded)
            {
                result.Status = false;
                result.Message = $"<p>Kullanıcıya '{dto.SelectedRole}' rolü atanırken bir hata meydana geldi.</p>";
                // Oluşturulan kullanıcıyı silmek iyi bir pratik olabilir
                await _userManager.DeleteAsync(user);
                return result;
            }

            result.Status = true;
            result.Message = "Hesabınız başarıyla oluşturuldu. Lütfen giriş yapınız.";
            return result;
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<ResultDto> SignIn(LoginDto dto)
        {
            var localResult = new ResultDto(); // <-- Her zaman lokal bir ResultDto örneği kullanın
            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user == null)
            {
                localResult.Status = false;
                // Kullanıcı adı veya şifre yanlış olduğunda hangisinin yanlış olduğunu belirtmemek daha güvenlidir.
                localResult.Message = "Kullanıcı Adı veya Parola Geçersiz!";
                return localResult;
            }

            // Sadece şifreyi kontrol etmek yerine, SignInManager'ın lockout gibi özelliklerini de
            // kullanabilen CheckPasswordSignInAsync metodunu kullanalım.
            // Bu metot, şifre doğruysa cookie OLUŞTURMAZ, sadece sonucu döner.
            var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            // lockoutOnFailure: true -> Belirli sayıda başarısız denemeden sonra hesabı kilitleme özelliği (önerilir)

            if (!checkPasswordResult.Succeeded)
            {
                localResult.Status = false;
                localResult.Message = "Kullanıcı Adı veya Parola Geçersiz!";
                if (checkPasswordResult.IsLockedOut)
                {
                    localResult.Message = "Hesabınız çok sayıda başarısız giriş denemesi nedeniyle kilitlendi. Lütfen daha sonra tekrar deneyin.";
                }
                else if (checkPasswordResult.IsNotAllowed)
                {
                    // Örneğin, email doğrulaması gerekiyorsa ve yapılmamışsa bu durum oluşabilir.
                    localResult.Message = "Giriş yapmanıza izin verilmiyor (örn: email doğrulanmamış).";
                }
                return localResult;
            }

            // ---- ŞİFRE DOĞRU, ŞİMDİ MVC TARAFINDA KİMLİK DOĞRULAMA İÇİN COOKIE OLUŞTURMA ----
            // Bu satır, HttpContext.User'ı bir sonraki MVC isteği için doldurur
            // ve _ViewStart.cshtml veya [Authorize] attributeları doğru çalışır.
            await _signInManager.SignInAsync(user, isPersistent: false); // isPersistent: false -> tarayıcı kapanınca cookie silinsin
                                                                         // --------------------------------------------------------------------------

            // ---- JWT Token Oluşturma (Mevcut kodunuzla aynı) ----
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim("JWTID", Guid.NewGuid().ToString()),
        // Diğer istediğiniz claim'ler buraya eklenebilir
    };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole)); // Rolleri token'a ekle
            }

            var token = GenerateJWT(authClaims); // Bu metodunuz zaten vardı

            localResult.Status = true;
            localResult.Message = token; // API istemcileri için JWT token
            return localResult;
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

