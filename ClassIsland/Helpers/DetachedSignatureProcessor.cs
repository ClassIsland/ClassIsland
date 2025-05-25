using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Bcpg;
using System.IO;
using System;
using PgpCore;
using System.Text;

namespace ClassIsland.Helpers;

// https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/openpgp/examples/DetachedSignatureProcessor.cs
public static class DetachedSignatureProcessor
{
    /// <summary>
    /// 验证分离的 PGP 签名。
    /// </summary>
    /// <param name="data">待验证的字符串数据。</param>
    /// <param name="signatureData">签名字节数组或 MemoryStream。</param>
    /// <param name="publicKeyData">公钥的字符串数据（ASCII Armored 格式）或字节数组。</param>
    /// <returns>如果签名有效，则返回 true；否则，返回 false。</returns>
    public static bool VerifyDetachedSignature(string data, byte[] signatureData, string publicKeyData)
    {
        using Stream keyIn = GenerateStreamFromString(publicKeyData);
        using Stream sigIn = new MemoryStream(signatureData);
        using Stream dataIn = new MemoryStream(Encoding.UTF8.GetBytes(data));
        return VerifyDetachedSignature(keyIn, dataIn, sigIn);
    }

    /// <summary>
    /// 内部方法：验证分离的 PGP 签名。
    /// </summary>
    /// <param name="publicKeyStream">公钥的 Stream（ASCII Armored 格式）。</param>
    /// <param name="dataStream">待验证的 Stream 数据。</param>
    /// <param name="signatureStream">签名的 Stream。</param>
    /// <returns>如果签名有效，则返回 true；否则，返回 false。</returns>
    private static bool VerifyDetachedSignature(Stream publicKeyStream, Stream dataStream, Stream signatureStream)
    {
        try
        {
            // 读取公钥
            var pgpPub = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(publicKeyStream));

            // 读取签名
            var pgpFact = new PgpObjectFactory(PgpUtilities.GetDecoderStream(signatureStream));
            var pgpObj = pgpFact.NextPgpObject();

            if (pgpObj is PgpCompressedData compressedData)
            {
                pgpObj = new PgpObjectFactory(compressedData.GetDataStream()).NextPgpObject();
            }

            if (pgpObj is not PgpSignatureList sigList)
            {
                throw new PgpException("签名文件格式不正确。");
            }

            var sig = sigList[0];
            var key = pgpPub.GetPublicKey(sig.KeyId);

            if (key == null)
            {
                throw new PgpException("无法找到匹配的公钥。");
            }

            sig.InitVerify(key);

            // 读取数据并传递给签名对象
            using (var inputData = dataStream)
            {
                int ch;
                while ((ch = inputData.ReadByte()) >= 0)
                {
                    sig.Update((byte)ch);
                }
            }

            return sig.Verify();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证过程中出现错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 将字符串转换为 MemoryStream。
    /// </summary>
    /// <param name="s">输入字符串。</param>
    /// <returns>MemoryStream 对象。</returns>
    private static MemoryStream GenerateStreamFromString(string s)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(s));
    }


    public static string CreateSignature(
        string content,
        string keyIn,
        string passPhrase)
    {
        Stream outputStreamRaw = new MemoryStream();
        Stream outputStream = new ArmoredOutputStream(outputStreamRaw);
        

        var pgpSec = new EncryptionKeys(keyIn, passPhrase);
        PgpPrivateKey pgpPrivKey = pgpSec.PrivateKey;
        PgpSignatureGenerator sGen = new PgpSignatureGenerator(
            pgpSec.PrivateKey.PublicKeyPacket.Algorithm, HashAlgorithmTag.Sha1);

        sGen.InitSign(PgpSignature.BinaryDocument, pgpPrivKey);

        BcpgOutputStream bOut = new BcpgOutputStream(outputStream);

        Stream fIn = new MemoryStream(Encoding.UTF8.GetBytes(content));

        int ch;
        while ((ch = fIn.ReadByte()) >= 0)
        {
            sGen.Update((byte)ch);
        }

        fIn.Close();

        sGen.Generate().Encode(bOut);

        outputStream.Close();

        outputStreamRaw.Seek(0, SeekOrigin.Begin);
        return new StreamReader(outputStreamRaw).ReadToEnd();
    }
}