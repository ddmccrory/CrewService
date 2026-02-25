using CrewService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CrewService.Persistance.Encryption;

public sealed class EncryptedStringConverter(IFieldEncryptor encryptor) : ValueConverter<string, string>(
        v => encryptor.Encrypt(v),
        v => encryptor.Decrypt(v))
{
}
