using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService.User
{
    public class UserData
    {
        public Dictionary<string , UserInfo> Users { get; set; }
        public void AddOrUpdate(UserInfo info)
        {
            Common.Log.Logger.Default.Trace($"Update User:{info.UserName},{info.Level}");
            if (string.IsNullOrWhiteSpace(info.UserName))
                throw new InvalidCastException("用户名无效.");
            if (string.IsNullOrWhiteSpace(info.Password))
                throw new InvalidCastException("密码不能为空.");
            if (info.Password.Length < 8)
                throw new InvalidCastException("密码长度过短(不低于8位).");
            int pass = 0;
            if (int.TryParse(info.Password, out pass))
                throw new InvalidCastException("密码不能为纯数字。");
            if (info.Level < 1 || info.Level > 3)
                throw new InvalidCastException("不支持的用户等级。");
            lock (Users)
            {
                Users[info.UserName] = info;
            }
        }

        public UserInfo[] CloneSafe()
        {
            lock (Users)
            {
                var users = Users.Values.Select(_ => _.Clone()).ToList();
                users.ForEach(_ => _.Password = null);
                return users.ToArray();
            }
        }
    }
}
