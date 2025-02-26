using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace NLSeed.Tests.Fakes;

public static class CredentialsStub
{
    private static string? _fakeCertBase64 = null;
    
    // A fake macaroon in Base64 format.
    // "This is a fake macaroon" encoded in Base64 is "VGhpcyBpcyBhIGZha2UgbWFjYXJvb24=".
    public static string FakeMacaroon => "VGhpcyBpcyBhIGZha2UgbWFjYXJvb24=";

    // A fake self-signed X.509 certificate in Base64 (DER-encoded)
    // This certificate was generated for testing purposes.
    public static string FakeCertificate => _fakeCertBase64 ?? MakeCert();

    public static string MakeCert()
    {
        if (_fakeCertBase64 is not null) return _fakeCertBase64;
        
        var ecdsa = ECDsa.Create(); // generate asymmetric key pair
        var req = new CertificateRequest("cn=nlseed", ecdsa, HashAlgorithmName.SHA256);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

        // Create PFX (PKCS #12) with private key
        // File.WriteAllBytes("tmp/mycert.pfx", cert.Export(X509ContentType.Pfx, "dotNetFTW!"));

        // Create Base 64 encoded CER (public key only)
        _fakeCertBase64 = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

        return _fakeCertBase64;
    }
}