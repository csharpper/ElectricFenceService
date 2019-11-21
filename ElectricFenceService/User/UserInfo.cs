using System;

namespace ElectricFenceService.User
{
    public interface UserBase
    {
        /// <summary>
        /// 用户名
        /// </summary>
        string UserName { get; }
        /// <summary>
        /// 用户登录时的权限
        /// </summary>
        int Level { get; }
    }
    /// <summary>
    /// 默认权限等级1，2，3
    /// </summary>
    public class UserInfo:UserBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 用户登录时的经度
        /// </summary>
        public double Longitude { get; set; } = 122.1458;
        /// <summary>
        /// 用户登录时的纬度
        /// </summary>
        public double Latitude { get; set; } = 30.0502;
        public int Scale { get; set; } = 10;
        public int Level { get; set; }

        public UserInfo Clone()
        {
            return (UserInfo)MemberwiseClone();
        }
    }
}