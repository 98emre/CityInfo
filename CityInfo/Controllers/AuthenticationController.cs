using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CityInfo.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthenticationController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentException(nameof(configuration));
        }


        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate(AuthenticateRequestBody authenticateRequestBody)
        {

            var user = ValidateUserCredentials(authenticateRequestBody.UserName, authenticateRequestBody.Password);

            if(user == null)
            {
                return Unauthorized();
            }

            var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(_configuration["Authentication:SecretForKey"]));
            
            // Step 1 Header
            var signingCredentials = new SigningCredentials(securityKey,SecurityAlgorithms.HmacSha256);

            // Step 2 PayLoad
            var claimsToken = new List<Claim>();
            claimsToken.Add(new Claim("sub",user.UserId.ToString()));
            claimsToken.Add(new Claim("given_name",user.FirstName));
            claimsToken.Add(new Claim("family_name", user.LastName));
            claimsToken.Add(new Claim("city", user.City));


            // Step 3 Signature
            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsToken,
                DateTime.UtcNow,
                DateTime.UtcNow.AddHours(1),
                signingCredentials);

            var tokenReturn = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            return Ok(tokenReturn);
        }

        public class AuthenticateRequestBody
        {
            public string? UserName { get; set; }
            public string? Password { get; set; }
        }

        private CityInfoUser ValidateUserCredentials(string? userName, string? password)
        {
            return new CityInfoUser(1, userName ?? "", "James", "Bond", "Paris");
        }

        private class CityInfoUser
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string City { get; set; }

            public CityInfoUser(int userId, string userName, string firstName, string lastName, string city)
            {
                UserId = userId;
                UserName = userName;
                FirstName = firstName;
                LastName = lastName;
                City = city;
            }
        }
    }
}
