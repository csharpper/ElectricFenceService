namespace ElectricFenceService.User
{
    /// <summary>
    /// 默认权限等级1，2，3
    /// </summary>
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Level { get; set; }
    }
}