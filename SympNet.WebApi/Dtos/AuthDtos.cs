namespace SympNet.WebApi.Dtos;

public record RegisterDto(
    string Email,
    string Password,
    string Role,
    string? FullName = null,
    string? Speciality = null
);

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    string Email,
    string Role,
    Guid UserId,
    string? FullName = null
);

public record CreateUserDto(
    string Email,
    string Password,
    string Role,
    string? FullName = null
);

public record UpdateUserDto(
    string? Email,
    string? Password,
    string? Role,
    bool? IsActive
);