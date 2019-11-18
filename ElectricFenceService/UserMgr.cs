using ElectricFenceService.User;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ElectricFenceService
{
    public class UserMgr
    {
        public readonly static UserMgr Instance = new UserMgr();
        UserData _users;
        string _path = "user.json";
        UserMgr()
        {
            loadData();
        }

        public void Init()
        { }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(_users, Formatting.Indented);
        }

        private void loadData()
        {
            if (File.Exists(_path))
            {
                string info = File.ReadAllText(_path);
                _users = JsonConvert.DeserializeObject<UserData>(info);
            }
            else
            newAddSave();
        }

        private void newAddSave()
        {

            _users = new UserData()
            {
                Users = new Dictionary<string, UserInfo>()
            };
            _users.Users["system"] = new UserInfo() { UserName = "system", Password = "system", Level = 1 };
            _users.Users["admin"] = new UserInfo() { UserName = "admin", Password = "admin", Level = 2 };
            _users.Users["user"] = new UserInfo() { UserName = "user", Password = "user", Level = 3 };
            save();
        }

        public void Set(UserInfo region)
        {
            _users.AddOrUpdate(region);
        }

        private void save()
        {
            File.WriteAllText(_path, ToJson());
        }

    }
}