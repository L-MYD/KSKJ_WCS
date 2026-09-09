using System;
using System.Security.Cryptography;

namespace WCS_Helper
{
    // nan_T 2026-09-09：新增密码哈希工具，替代原来的明文存储/明文比较。
    // 采用 PBKDF2-SHA256（.NET 内置 Rfc2898DeriveBytes，无需第三方包），
    // 存储格式：PBKDF2:{迭代次数}:{Base64盐}:{Base64哈希}
    // 同时兼容数据库中历史遗留的明文密码：Verify 时若发现不是哈希格式，按明文比较，
    // 由调用方在登录成功后调用 Hash 重新落库，完成平滑升级。
    public static class PasswordHasher
    {
        // nan_T 2026-09-09：哈希格式前缀，用于区分新哈希与旧明文
        private const string HashPrefix = "PBKDF2:";

        // nan_T 2026-09-09：迭代次数，PBKDF2 计算轮数，越高越安全但越慢
        private const int Iterations = 10000;

        // nan_T 2026-09-09：盐长度（字节）
        private const int SaltSize = 16;

        // nan_T 2026-09-09：哈希结果长度（字节），256 位
        private const int HashSize = 32;

        /// <summary>
        /// nan_T 2026-09-09：对明文密码生成 PBKDF2 哈希字符串
        /// </summary>
        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            // nan_T 2026-09-09：每次哈希使用随机盐，防止彩虹表攻击
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

            return $"{HashPrefix}{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// nan_T 2026-09-09：校验明文密码与库中存储值是否匹配。
        /// 返回值：isValid 是否匹配；needsUpgrade 匹配但库中为旧明文、需要升级为哈希。
        /// </summary>
        public static (bool isValid, bool needsUpgrade) Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
                return (false, false);

            // nan_T 2026-09-09：新格式哈希，按 PBKDF2 常数时间比较
            if (stored.StartsWith(HashPrefix, StringComparison.Ordinal))
            {
                var parts = stored.Split(':');
                if (parts.Length != 4)
                    return (false, false);

                if (!int.TryParse(parts[1], out int iterations))
                    return (false, false);

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

                // nan_T 2026-09-09：固定时间比较，防止计时侧信道
                return (CryptographicOperations.FixedTimeEquals(actualHash, expectedHash), false);
            }

            // nan_T 2026-09-09：兼容历史明文密码，匹配成功后提示调用方升级
            return (stored == password, true);
        }
    }
}
