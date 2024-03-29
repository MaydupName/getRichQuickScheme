﻿using System.Security.Cryptography;
using System.Text;
using Domain.Common;
using FluentValidation;

namespace Domain.ValueObj;

public class Password : ValueObject
{
    private Password()
    {
    }

    public string Salt { get; private set; } = default!;
    public string Hashed { get; private set; } = default!;

    public static Password Create(string password)
    {
        var salt = GenerateRandomText.Generate(32);
        var valueObj = new Password()
        {
            Salt = salt,
            Hashed = HashPassword(salt, password)
        };
        valueObj.Validate();
        return valueObj;
    }

    private static string HashPassword(string salt, string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + salt));
        return Encoding.UTF8.GetString(bytes);
    }
    
    public bool Compare(Password? pas1)
    {
        if (pas1 is null)
            return false;
        return Hashed == pas1.Hashed;
    }
    
    public bool Compare(string? pas1)
    {
        if (pas1 is null)
            return false;
        return Hashed == HashPassword(Salt, pas1);
    }

    protected override IEnumerable<object?> GetFields()
    {
        yield return Hashed;
        yield return Salt;
    }
}

public class PasswordValidator : AbstractValidator<Password>
{
    public PasswordValidator()
    {
        RuleFor(x => x.Hashed).MaximumLength(4096).NotEmpty();
        RuleFor(x => x.Salt).Length(32).NotEmpty();
    }
}