using System;
using System.IO;
using System.Text;

namespace Backlot.Core.Security;
using System.Security.Cryptography;

public class EncryptionService : IEncryptionService
{
    private string _key;
    
    public EncryptionService(string key)
    {
        _key = key.Substring(0,16);
    }
    
    /// <summary>
    /// Returns an encrypted string with a random IV in the format of 'base64string.iv'
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public string Encrypt(string plainText)
    {
        // string with first 4 chars of a guid
        var iv = Guid.NewGuid().ToString("N").Substring(0, 16);
        using (var aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(_key); // an exception will be thrown if the key is not 16 chars long
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);
            var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        // Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                    return $"{Convert.ToBase64String(msEncrypt.ToArray())}.{iv}";
                }
            }
        }
    }
    
    /// <summary>
    /// Decrypts a string and is accepting an IV in the format of 'base64string.iv'
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public string Decrypt(string value)
    {
        var parts = value.Split('.');
        
        if(parts.Length != 2)
            throw new ArgumentException("The value to Decrypt is not in the correct format. It should be in the format of 'base64string.iv'");
        
        using (var aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(_key);
            aesAlg.IV = Encoding.UTF8.GetBytes(parts[1]);
            
            var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (var msDecrypt = new MemoryStream(Convert.FromBase64String(parts[0])))
            {
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream
                        // and place them in a string.
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Creates a one-way hash of the input string using SHA256.
    /// </summary>
    /// <param name="plainText">The string to hash.</param>
    /// <returns>A base64 encoded string of the hash.</returns>
    public string Hash(string plainText)
    {
        if (plainText != null)
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        throw new ArgumentNullException(nameof(plainText));
    }
}

public interface IEncryptionService
{
    /// <summary>
    /// Text which you can decrypt.
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    public string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypt text created by Encrypt
    /// </summary>
    /// <param name="cipherText"></param>
    /// <returns></returns>
    public string Decrypt(string cipherText);

    string Hash(string plainText);
}