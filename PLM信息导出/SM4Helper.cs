
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PLM信息导出
{
    /// <summary>
    /// 国密4加密
    /// 专供plm对接
    /// </summary>
    public class SM4Helper
    {
        /// <summary>
        /// 定制的随机数生成器
        /// plm对接用
        /// </summary>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static byte[] CustomGenerateBytes(byte[] seed)
        {
            return CustomGenerateBytes(seed, 128);
        }

        /// <summary>
        /// 定制的随机数生成器
        /// plm对接用
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="keySize"></param>
        /// <returns></returns>
        public static byte[] CustomGenerateBytes(byte[] seed, int keySize)
        {
            var kg = GeneratorUtilities.GetKeyGenerator("SM4");

            var rng = new CustomSHA1PRNG();
            rng.AddSeedMaterial(seed);
            var sr = new SecureRandom(rng);
            kg.Init(new KeyGenerationParameters(sr, keySize));
            return kg.GenerateKey();
        }

        /// <summary>
        /// 16进制字符串转bytes
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] HexConvert(string hexString)
        {
            byte[] byteArray = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                byteArray[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return byteArray;
        }

        /// <summary>
        /// bytes转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string HexConvert(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// 签名
        /// https://flowus.cn/dnge/share/7a475b74-bbdc-4686-b6bd-36ce3ee47f02
        /// </summary>
        /// <param name="sk"></param>
        /// <param name="ak"></param>
        /// <param name="ts"></param>
        /// <returns></returns>
        public static string Sign(string sk, string ak, string ts)
        {
            var md5 = $"{ak}{ts}{{}}";

            md5 = HexConvert(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(md5)));
            var lastMD5 = HexConvert(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(ts)))
                .Substring(16);

            byte[] plaintext = Encoding.UTF8.GetBytes(md5);
            byte[] keyBytes = CustomGenerateBytes(Encoding.UTF8.GetBytes(sk));
            byte[] iv = CustomGenerateBytes(Encoding.UTF8.GetBytes(lastMD5));

            KeyParameter key = ParameterUtilities.CreateKeyParameter("SM4", keyBytes);
            ParametersWithIV keyParamWithIv = new ParametersWithIV(key, iv);

            IBufferedCipher inCipher = CipherUtilities.GetCipher("SM4/CBC/PKCS5Padding");

            inCipher.Init(true, keyParamWithIv);

            return HexConvert(inCipher.DoFinal(plaintext));
        }

        /// <summary>
        /// 验签
        /// </summary>
        /// <param name="sk"></param>
        /// <param name="ak"></param>
        /// <param name="ts"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool CheckSign(string sk, string ak, string ts, string target)
        {
            var md5 = $"{ak}{ts}{{}}";

            md5 = HexConvert(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(md5)));
            var lastMD5 = HexConvert(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(ts)))
                .Substring(16);

            //byte[] plaintext = Encoding.UTF8.GetBytes(md5);
            byte[] keyBytes = CustomGenerateBytes(Encoding.UTF8.GetBytes(sk));
            byte[] iv = CustomGenerateBytes(Encoding.UTF8.GetBytes(lastMD5));

            KeyParameter key = ParameterUtilities.CreateKeyParameter("SM4", keyBytes);
            ParametersWithIV keyParamWithIv = new ParametersWithIV(key, iv);

            IBufferedCipher inCipher = CipherUtilities.GetCipher("SM4/CBC/PKCS5Padding");

            inCipher.Init(false, keyParamWithIv);

            return md5 == Encoding.UTF8.GetString(inCipher.DoFinal(HexConvert(target)));
        }

        /// <summary>
        /// 定制的sha1随机数生成器
        /// plm那边用seed控制随机数来稳定加密，很奇怪
        /// java和dotnet随机数实现不一样，加解密不通，所以得搬一个
        /// 找到一个其他人的实现，修修补补用起来了
        /// https://stackoverflow.com/questions/70587126/how-to-get-the-same-result-in-c-sharp-with-securerandom-getinstancesha1prng
        /// </summary>
        private class CustomSHA1PRNG : IRandomGenerator
        {
            private const int DIGEST_SIZE = 20;

            public CustomSHA1PRNG()
            {
            }

            private static void updateState(byte[] state, byte[] output)
            {
                int last = 1;
                int v;
                byte t;
                bool zf = false;

                // state(n + 1) = (state(n) + output(n) + 1) % 2^160;
                for (int i = 0; i < state.Length; i++)
                {
                    // Add two bytes
                    v = (int)(sbyte)state[i] + (int)(sbyte)output[i] + last;
                    // Result is lower 8 bits
                    t = (byte)(sbyte)v;
                    // Store result. Check for state collision.
                    zf = zf | (state[i] != t);
                    state[i] = t;
                    // High 8 bits are carry. Store for next iteration.
                    last = v >> 8;
                }

                // Make sure at least one bit changes!
                if (!zf)
                {
                    state[0] = (byte)(sbyte)(state[0] + 1);
                }
            }

            private static void GetBytes(byte[] seed, byte[] result)
            {
                byte[] state;
                byte[] remainder = null;
                int remCount;
                int index = 0;
                int todo;
                byte[] output = remainder;

                using (var sha1 = new SHA1CryptoServiceProvider())
                {
                    state = sha1.ComputeHash(seed);
                    remCount = 0;

                    // Use remainder from last time
                    int r = remCount;
                    if (r > 0)
                    {
                        // How many bytes?
                        todo = (result.Length - index) < (DIGEST_SIZE - r)
                            ? (result.Length - index)
                            : (DIGEST_SIZE - r);
                        // Copy the bytes, zero the buffer
                        for (int i = 0; i < todo; i++)
                        {
                            result[i] = output[r];
                            output[r++] = 0;
                        }

                        remCount += todo;
                        index += todo;
                    }

                    // If we need more bytes, make them.
                    while (index < result.Length)
                    {
                        // Step the state
                        output = sha1.ComputeHash(state);
                        updateState(state, output);

                        // How many bytes?
                        todo = (result.Length - index) > DIGEST_SIZE ? DIGEST_SIZE : result.Length - index;
                        // Copy the bytes, zero the buffer
                        for (int i = 0; i < todo; i++)
                        {
                            result[index++] = output[i];
                            output[i] = 0;
                        }

                        remCount += todo;
                    }

                    // Store remainder for next time
                    //remainder = output;
                    //remCount %= DIGEST_SIZE;
                }
            }

            private static byte[] Seed { get; set; }

            public void AddSeedMaterial(byte[] seed)
            {
                Seed = seed;
            }

            public void AddSeedMaterial(long seed)
            {
                throw new NotImplementedException();
            }

            public void NextBytes(byte[] bytes)
            {
                GetBytes(Seed, bytes);
            }

            public void NextBytes(byte[] bytes, int start, int len)
            {
                throw new NotImplementedException();
            }
        }
    }
}
