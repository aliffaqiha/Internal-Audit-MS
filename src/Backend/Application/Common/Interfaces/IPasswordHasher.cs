namespace IAMS.Application.Common.Interfaces;

/// <summary>Password hashing/verification abstraction (implemented with ASP.NET Core Identity hasher).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}