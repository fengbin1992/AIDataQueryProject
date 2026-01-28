namespace AIDataQuery.API.Infrastructure.Encryption;

public interface IAesEncryptor
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
