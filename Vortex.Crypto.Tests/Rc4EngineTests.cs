using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Vortex.Crypto.Tests;

/// <summary>
/// TEST-01 safety net for <see cref="Rc4Engine"/>: round-trip correctness, one well-known RC4
/// test vector, the constructor's key-length guards, and the CODE-02 argument validation added to
/// <see cref="Rc4Engine.ProcessBytes"/>.
/// </summary>
public sealed class Rc4EngineTests
{
    private static byte[] Key(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void RoundTrip_EncryptThenDecrypt_ReturnsOriginalPlaintext()
    {
        byte[] key = Key("a reasonably long RC4 session key, not just a couple of bytes");
        byte[] plaintext = new byte[4096];
        new Random(1234).NextBytes(plaintext);

        Rc4Engine encryptor = new(key);
        byte[] ciphertext = encryptor.Process(plaintext);

        ciphertext.Should().NotEqual(plaintext, "RC4 must actually transform the data");

        // A fresh engine re-keyed identically must reproduce the same keystream so it can decrypt.
        Rc4Engine decryptor = new(key);
        byte[] decrypted = decryptor.Process(ciphertext);

        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public void KnownVector_KeyPlaintext_MatchesStandardRc4Ciphertext()
    {
        // Classical RC4 test vector (Key="Key", Plaintext="Plaintext"), widely reproduced across
        // reference implementations. dropN=0 / no offset matches the plain, undropped RC4 stream.
        Rc4Engine engine = new(Key("Key"));
        byte[] ciphertext = engine.Process(Key("Plaintext"));

        Convert.ToHexString(ciphertext).Should().Be("BBF316E8D940AF0AD3");
    }

    [Fact]
    public void Constructor_NullKey_ThrowsArgumentNullException()
    {
        Action act = () => _ = new Rc4Engine(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_EmptyKey_ThrowsArgumentException()
    {
        Action act = () => _ = new Rc4Engine(Array.Empty<byte>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_KeyLongerThan256Bytes_ThrowsArgumentException()
    {
        Action act = () => _ = new Rc4Engine(new byte[257]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_KeyExactly256Bytes_Succeeds()
    {
        Action act = () => _ = new Rc4Engine(new byte[256]);

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NegativeDropN_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => _ = new Rc4Engine(Key("some-key"), dropN: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void ProcessBytes_NegativeOffset_ThrowsArgumentOutOfRangeException(
        int inputOffset,
        int outputOffset
    )
    {
        Rc4Engine engine = new(Key("k"));
        byte[] buffer = new byte[8];

        Action act = () =>
            engine.ProcessBytes(buffer, inputOffset, 4, buffer, outputOffset);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProcessBytes_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        Rc4Engine engine = new(Key("k"));
        byte[] buffer = new byte[8];

        Action act = () => engine.ProcessBytes(buffer, 0, -1, buffer, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ProcessBytes_NullInputData_ThrowsArgumentNullException()
    {
        Rc4Engine engine = new(Key("k"));
        byte[] buffer = new byte[8];

        Action act = () => engine.ProcessBytes(null!, 0, 4, buffer, 0);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProcessBytes_NullOutputData_ThrowsArgumentNullException()
    {
        Rc4Engine engine = new(Key("k"));
        byte[] buffer = new byte[8];

        Action act = () => engine.ProcessBytes(buffer, 0, 4, null!, 0);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProcessBytes_InputBufferShorterThanRequestedLength_ThrowsArgumentException()
    {
        Rc4Engine engine = new(Key("k"));
        byte[] input = new byte[4];
        byte[] output = new byte[8];

        Action act = () => engine.ProcessBytes(input, 0, 8, output, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessBytes_OutputBufferShorterThanRequestedLength_ThrowsArgumentException()
    {
        Rc4Engine engine = new(Key("k"));
        byte[] input = new byte[8];
        byte[] output = new byte[4];

        Action act = () => engine.ProcessBytes(input, 0, 8, output, 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Peek_DoesNotAdvanceKeystream_AndMatchesSubsequentProcessBytes()
    {
        byte[] key = Key("peek-does-not-consume");
        byte[] plaintext = Key("some plaintext that is peeked at before being really processed");

        Rc4Engine engine = new(key);

        byte[] peeked = engine.Peek(plaintext);
        byte[] peekedAgain = engine.Peek(plaintext);

        // Peeking twice must return identical output -- proof it never mutates (_i, _j).
        peekedAgain.Should().Equal(peeked);

        byte[] processed = engine.ProcessBytes(
            plaintext,
            0,
            plaintext.Length,
            new byte[plaintext.Length],
            0
        );

        // The real (state-advancing) call must produce the exact same bytes Peek predicted.
        processed.Should().Equal(peeked);
    }

    [Fact]
    public void DropN_ProducesDifferentKeystreamThanNoDrop()
    {
        byte[] key = Key("drop-n-changes-the-stream");
        byte[] plaintext = new byte[64];
        new Random(99).NextBytes(plaintext);

        Rc4Engine noDrop = new(key);
        Rc4Engine withDrop = new(key, dropN: 256);

        byte[] noDropCipher = noDrop.Process(plaintext);
        byte[] withDropCipher = withDrop.Process(plaintext);

        withDropCipher.Should().NotEqual(noDropCipher);
    }

    [Fact]
    public void SameKey_TwoIndependentEngines_ProduceIdenticalCiphertext()
    {
        byte[] key = Key("deterministic-keystream");
        byte[] plaintext = Key("identical input must yield identical output for the same key");

        Rc4Engine first = new(key);
        Rc4Engine second = new(key);

        first.Process(plaintext).Should().Equal(second.Process(plaintext));
    }
}
