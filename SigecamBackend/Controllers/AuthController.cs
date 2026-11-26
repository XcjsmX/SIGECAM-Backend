using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SigecamBackend.DTOs;
using SigecamBackend.Models;

namespace SigecamBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FirestoreDb _firestore;
    private readonly IConfiguration _config;

    public AuthController(FirestoreDb firestore, IConfiguration config)
    {
        _firestore = firestore;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var usersRef = _firestore.Collection("Users");
        var existingQuery = await usersRef.WhereEqualTo("email", request.Email.ToLower()).GetSnapshotAsync();

        if (existingQuery.Count > 0)
        {
            return BadRequest(new { mensaje = "El correo electrónico ya está registrado." });
        }

        var newUser = new User
        {
            Nombre = request.Nombre,
            Email = request.Email.ToLower(),
            PasswordHash = HashPassword(request.Password),
            Rol = "Paciente", // Rol por defecto según requerimiento
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var docRef = await usersRef.AddAsync(newUser);
        newUser.Id = docRef.Id;

        var token = GenerateJwtToken(newUser);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = newUser.Id,
            Nombre = newUser.Nombre,
            Email = newUser.Email,
            Rol = newUser.Rol
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var usersRef = _firestore.Collection("Users");
        var snapshot = await usersRef.WhereEqualTo("email", request.Email.ToLower()).GetSnapshotAsync();

        if (snapshot.Count == 0)
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        var userDoc = snapshot.Documents[0];
        var user = userDoc.ConvertTo<User>();
        user.Id = userDoc.Id;

        if (!user.Activo)
        {
            return Unauthorized(new { mensaje = "Esta cuenta ha sido desactivada por un administrador." });
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Nombre = user.Nombre,
            Email = user.Email,
            Rol = user.Rol,
            CentroId = user.CentroId
        });
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Nombre),
            new Claim(ClaimTypes.Role, user.Rol)
        };

        if (!string.IsNullOrEmpty(user.CentroId))
        {
            claims.Add(new Claim("CentroId", user.CentroId));
        }

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpireMinutes"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}