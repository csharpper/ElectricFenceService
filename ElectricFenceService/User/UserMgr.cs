using ElectricFenceService.Http;
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

        public string ToJsonSafe()
        {
            var users = _users.CloneSafe();
            return JsonConvert.SerializeObject(users, Formatting.Indented);
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
            lock(_users)
                _users.AddOrUpdate(region);
        }

        public void ChangePass(string user, string pass)
        {
            lock (_users)
            {
                if (_users.Users.ContainsKey(user))
                {
                    var u = _users.Users[user].Clone();
                    u.Password = pass;
                    _users.AddOrUpdate(u);
                }
            }
        }

        public int Login(string user, string pass)
        {
            lock (_users)
            {
                if (!_users.Users.ContainsKey(user))
                    return -1;
                if (_users.Users[user].Password != pass)
                    return -2;
                return _users.Users[user].Level;
            }
        }

        public bool IsExist(string userName)
        {
            lock (_users)
            {
                return _users.Users.ContainsKey(userName);
            }
        }

        public UserInfo Get(string userName)
        {
            lock (_users)
            {
                if(_users.Users.ContainsKey(userName))
                    return _users.Users[userName].Clone();
                return null;
            }
        }

        private void save()
        {
            File.WriteAllText(_path, ToJson());
        }

        public int Delete(IEnumerable<string> users)
        {
            if (users.Any(_ => IsDefaultName(_)))
                throw new InvalidCastException("不支持删除默认的用户");
            int count = 0;
            lock (_users)
            {
                foreach (var user in users)
                {
                    if (_users.Users.Remove(user))
                    {
                        Common.Log.Logger.Default.Trace("成功删除用户：" + user);
                        count++;
                    }
                }
            }
            return count;
        }

        public bool IsDefaultName(string name)
        {
            return name == "system" || name == "admin" || name == "user";
        }
    }
}