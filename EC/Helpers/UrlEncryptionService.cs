using Microsoft.AspNetCore.DataProtection;

public class UrlEncryptionService
{
    private readonly IDataProtector _protector;

    public UrlEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ResetPasswordProtector");
    }

    public string Encrypt(string data)
    {
        return _protector.Protect(data);
    }

    public string Decrypt(string encrypted)
    {
        return _protector.Unprotect(encrypted);
    }
}
