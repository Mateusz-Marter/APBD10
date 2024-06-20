using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APBD_10.Context;
using APBD_10.DTO;
using APBD_10.Exceptions;
using APBD_10.Models;

namespace APBD_10.Services;

public interface IUserService
{
    public Task Register(RegisterRequestDto register);
    public Task<RefreshTokenResponseDto> Login(LoginRequestDto login);
    public Task<TokenResponseDto> RefreshToken(RefreshTokenRequestDto dto);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    
    public UserService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    
    private async Task<bool> emailExists(string email)
    {
        return await _context.Users.CountAsync(u => u.Email == email) > 0;
    }
    
    public async Task Register(RegisterRequestDto register)
    {
        if (await emailExists(register.Email))
        {
            throw new Exception("Email already exists");
        }
        
        var user = new User()
        {
            Email = register.Email,
            Login = register.Login,
        };
        
        user.HashedPassword = new PasswordHasher<User>().HashPassword(user, register.Password);
        
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task<RefreshTokenResponseDto> Login(LoginRequestDto login)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == login.Email);
        
        if (user == null) 
            throw new NotFoundException("User not found");
        
        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, login.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new InvalidPasswordException();
        
        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken.token;
        user.RefreshTokenExpiration = refreshToken.expiration;
        
        await _context.SaveChangesAsync();
        
        return new RefreshTokenResponseDto()
        {
            RefreshToken = refreshToken.token,
            RefreshTokenExpiration = refreshToken.expiration,
        };
    }

    public async Task<TokenResponseDto> RefreshToken(RefreshTokenRequestDto dto)
    {
        var principal = _tokenService.ValidateAndGetPrincipalFromJwt(dto.AccessToken, false);
        if (principal is null) 
            throw new InvalidTokenException();
        
        var claimIdUser = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claimIdUser is null || !int.TryParse(claimIdUser, out _))
            throw new InvalidTokenException();
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == int.Parse(claimIdUser));
        if (user is null || user.RefreshToken != dto.RefreshToken || user.RefreshTokenExpiration < DateTime.UtcNow)
            throw new InvalidTokenException();

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken.token;
        user.RefreshTokenExpiration = refreshToken.expiration;
        
        var response = new TokenResponseDto
        {
            AccessToken = _tokenService.GenerateAccessToken(user),
            RefreshToken = refreshToken.token,
        };

        return response;
    }
    
}