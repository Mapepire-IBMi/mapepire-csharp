using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using io.github.mapepire_ibmi;

namespace UnitTests;

/// <summary>Exercises the custom trust callback with local certificate inputs.</summary>
[TestClass]
public sealed class CertificateValidationTests
{
    /// <summary>Checks that missing certificates or chains produce a rejection.</summary>
    [TestMethod]
    public void RejectsMissingCertificateInputs()
    {
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback([]);
        using var chain = new X509Chain();
        using X509Certificate2 certificate = CreateCertificate();

        Assert.IsFalse(validate(this, null, chain, SslPolicyErrors.None));
        Assert.IsFalse(validate(this, certificate, null, SslPolicyErrors.None));
    }

    /// <summary>Checks trust-root handling and hostname validation for complete inputs.</summary>
    /// <param name="supplyRoot">Whether to pass the certificate as an explicitly configured root.</param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ValidatesWithConfiguredOrPeerRoot(bool supplyRoot)
    {
        using X509Certificate2 certificate = CreateCertificate();
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        var roots = new X509Certificate2Collection();
        if (supplyRoot)
        {
            roots.Add(certificate);
        }

        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(roots);

        Assert.IsTrue(validate(this, certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors));
        Assert.IsFalse(validate(this, certificate, chain, SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    /// <summary>Creates a temporary certificate for exercising custom trust.</summary>
    /// <returns>A self-signed certificate that the caller must dispose.</returns>
    private static X509Certificate2 CreateCertificate()
    {
        using RSA key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Mapepire test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
