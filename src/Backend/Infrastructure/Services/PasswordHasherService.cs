using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IAMS.Infrastructure.Services;

public sealed class PasswordHasherService : IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    public string Hash(string password)
        => Hasher.HashPassword(null!, password);

    public bool Verify(string password, string hash)
        => Hasher.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}