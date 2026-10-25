using System.Buffers.Binary;
using System.Buffers.Text;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wishlist.API.Tests;

/// <summary>
/// A platform authenticator in code: one ES256 (P-256) passkey, "none"
/// attestation, user present and verified, backed up like a synced passkey.
/// Produces the JSON a browser sends for navigator.credentials.create()/get().
/// </summary>
public sealed class SoftwareAuthenticator : IDisposable
{
    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private byte[]? _userHandle;

    public byte[] CredentialId { get; } = RandomNumberGenerator.GetBytes(16);
    public string Id => Base64Url.EncodeToString(CredentialId);

    /// <summary>The signature counter reported by the next assertion (incremented after each).</summary>
    public uint Counter { get; set; } = 1;

    /// <summary>Answers creation options (the "options" object from the API).</summary>
    public object Create(JsonElement options, string origin)
    {
        var rpId = options.GetProperty("rp").GetProperty("id").GetString()!;
        _userHandle = Base64Url.DecodeFromChars(options.GetProperty("user").GetProperty("id").GetString());
        var clientData = ClientData("webauthn.create", options.GetProperty("challenge").GetString()!, origin);

        var authData = AuthenticatorData(rpId, attested: true, counter: 0);
        var attestation = new CborWriter(CborConformanceMode.Ctap2Canonical);
        attestation.WriteStartMap(3);
        attestation.WriteTextString("fmt");
        attestation.WriteTextString("none");
        attestation.WriteTextString("attStmt");
        attestation.WriteStartMap(0);
        attestation.WriteEndMap();
        attestation.WriteTextString("authData");
        attestation.WriteByteString(authData);
        attestation.WriteEndMap();

        return new
        {
            id = Id,
            rawId = Id,
            type = "public-key",
            authenticatorAttachment = "platform",
            clientExtensionResults = new { },
            response = new
            {
                clientDataJSON = Base64Url.EncodeToString(clientData),
                attestationObject = Base64Url.EncodeToString(attestation.Encode()),
                transports = new[] { "internal", "hybrid" },
            },
        };
    }

    /// <summary>Answers request options (the "options" object from the API).</summary>
    public object Get(JsonElement options, string origin)
    {
        var rpId = options.GetProperty("rpId").GetString()!;
        var clientData = ClientData("webauthn.get", options.GetProperty("challenge").GetString()!, origin);
        var authData = AuthenticatorData(rpId, attested: false, counter: Counter++);

        var signed = authData.Concat(SHA256.HashData(clientData)).ToArray();
        var signature = _key.SignData(signed, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);

        return new
        {
            id = Id,
            rawId = Id,
            type = "public-key",
            authenticatorAttachment = "platform",
            clientExtensionResults = new { },
            response = new
            {
                clientDataJSON = Base64Url.EncodeToString(clientData),
                authenticatorData = Base64Url.EncodeToString(authData),
                signature = Base64Url.EncodeToString(signature),
                userHandle = Base64Url.EncodeToString(_userHandle ?? throw new InvalidOperationException("Create first.")),
            },
        };
    }

    private static byte[] ClientData(string type, string challenge, string origin) =>
        JsonSerializer.SerializeToUtf8Bytes(new { type, challenge, origin, crossOrigin = false });

    private byte[] AuthenticatorData(string rpId, bool attested, uint counter)
    {
        // Flags: user present (0x01), user verified (0x04), backup eligible (0x08),
        // backed up (0x10), attested credential data (0x40).
        byte flags = 0x01 | 0x04 | 0x08 | 0x10;
        if (attested) flags |= 0x40;

        var data = new List<byte>();
        data.AddRange(SHA256.HashData(Encoding.UTF8.GetBytes(rpId)));
        data.Add(flags);
        var counterBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(counterBytes, counter);
        data.AddRange(counterBytes);

        if (attested)
        {
            data.AddRange(new byte[16]); // AAGUID
            var length = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(length, (ushort)CredentialId.Length);
            data.AddRange(length);
            data.AddRange(CredentialId);
            data.AddRange(CosePublicKey());
        }

        return data.ToArray();
    }

    private byte[] CosePublicKey()
    {
        var p = _key.ExportParameters(false).Q;
        var cose = new CborWriter(CborConformanceMode.Ctap2Canonical);
        cose.WriteStartMap(5);
        cose.WriteInt32(1); cose.WriteInt32(2);   // kty: EC2
        cose.WriteInt32(3); cose.WriteInt32(-7);  // alg: ES256
        cose.WriteInt32(-1); cose.WriteInt32(1);  // crv: P-256
        cose.WriteInt32(-2); cose.WriteByteString(p.X!);
        cose.WriteInt32(-3); cose.WriteByteString(p.Y!);
        cose.WriteEndMap();
        return cose.Encode();
    }

    public void Dispose() => _key.Dispose();
}
