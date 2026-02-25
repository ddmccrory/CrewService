using CrewService.Domain.Interfaces;

namespace CrewService.UnitTests.Fixtures;

internal sealed class TestFieldEncryptor : IFieldEncryptor
{
    public string Encrypt(string plainText) => plainText;
    public string Decrypt(string cipherText) => cipherText;
}
