namespace CrewService.Domain.Interfaces;

public interface IFieldEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
