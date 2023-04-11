using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace PMTS.Authentication
{
    public class PmtsJwt
    {
        private string key = "6Yet9W%Hq^%g$uMT";
        //private string key = Environment.GetEnvironmentVariable("JWT_KEY");
        public string Create(int id)
        {
            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            SigningCredentials credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);
            JwtHeader header = new JwtHeader(credentials);
            JwtPayload payload = new JwtPayload(id.ToString(), null, null, DateTime.Now, DateTime.Today.AddDays(7));
            JwtSecurityToken securityToken = new JwtSecurityToken(header, payload);
            
            return new JwtSecurityTokenHandler().WriteToken(securityToken);
        }
        
        public JwtSecurityToken Validate(string token)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            handler.ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false
            }, out SecurityToken validatedToken);
            return (JwtSecurityToken)validatedToken;
        }
    }
}
