using io.github.mapepire_ibmi;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace UnitTests;

[TestClass]
[TestCategory("Offline")]
public sealed class CertificateValidationTests
{
    /// <summary>
    /// Rejects a peer certificate that is untrusted by the platform when no roots were configured.
    /// </summary>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void UntrustedPeerCertificateIsRejectedWithoutConfiguredRoots(bool useNullRoots)
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate("CN=localhost");
        using X509Chain chain = new();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        Assert.IsFalse(chain.Build(certificate));
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(
            useNullRoots ? null : new X509Certificate2Collection());

        Assert.IsFalse(validate(this, certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    /// <summary>
    /// Accepts a certificate that the platform validated when no roots were configured.
    /// </summary>
    [TestMethod]
    public void PlatformValidationIsAcceptedWithoutConfiguredRoots()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate("CN=localhost");
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(new());

        Assert.IsTrue(validate(this, certificate, null, SslPolicyErrors.None));
    }

    /// <summary>
    /// Accepts an explicitly trusted certificate when the hostname matches.
    /// </summary>
    [TestMethod]
    public void ExplicitlyTrustedCertificateIsAccepted()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate("CN=localhost");
        using X509Chain chain = new();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(new(certificate));

        Assert.IsTrue(validate(this, certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    /// <summary>
    /// Rejects a certificate outside the explicitly configured trust roots.
    /// </summary>
    [TestMethod]
    public void CertificateOutsideCustomTrustIsRejected()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate("CN=localhost");
        using X509Certificate2 root = CreateSelfSignedCertificate("CN=Different root");
        using X509Chain chain = new();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(new(root));

        Assert.IsFalse(validate(this, certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors));
    }

    /// <summary>
    /// Rejects a hostname mismatch even when the peer certificate is explicitly trusted.
    /// </summary>
    [TestMethod]
    public void HostnameMismatchIsRejectedWithCustomTrust()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate("CN=localhost");
        using X509Chain chain = new();
        RemoteCertificateValidationCallback validate = SqlJob.CreateCustomRemoteCertificateValidationCallback(new(certificate));

        Assert.IsFalse(validate(this, certificate, chain,
            SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors));
    }

    /// <summary>
    /// Creates an ephemeral certificate independent of the machine's trust store.
    /// </summary>
    private static X509Certificate2 CreateSelfSignedCertificate(string subject)
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return request.CreateSelfSigned(now.AddHours(-1), now.AddHours(1));
    }
}
