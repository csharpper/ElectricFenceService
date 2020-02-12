using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectricFenceService
{
    public static class ConfigData
    {
        public readonly static string FenceFileName = "fence.json";
        public readonly static string OnlineFileName = "online.json";
        public readonly static string ShieldFileName = "shield.json";
		[DefaultSettingValue("127.0.0.1")]
        public static string AisHost { get; private set; }
		[DefaultSettingValue("8020")]
        public static string AisPort { get; private set; }
		[DefaultSettingValue("8090")]
        public static string WebSocketPort { get; private set; }
		[DefaultSettingValue("8091")]
        public static string ListenPort { get; private set; }
		[DefaultSettingValue("15")]
        public static string TimeoutMinutes { get; private set; }
        static ConfigData()
        {
            foreach (var property in typeof(ConfigData).GetProperties())
            {
                if (Attribute.GetCustomAttribute(property, typeof(CustomAttribute)) != null)
                    continue;

                if (!property.CanWrite)
                    continue;
                var defaultValueAtt = Attribute.GetCustomAttribute(property, typeof(DefaultSettingValueAttribute)) as DefaultSettingValueAttribute;
                if (defaultValueAtt == null)
                    property.SetValue(null, Read(property.Name), null);
                else
                    property.SetValue(null, Read(property.Name, defaultValueAtt.Value), null);
            }
        }

        static string Read(string key, string defaultValue)
        {
            try
            {
                return Read(key);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public static string Read(string key)
        {
            if (!ConfigurationManager.AppSettings.AllKeys.Contains(key))
                throw new InvalidOperationException("读取配置文件失败。Key：" + key);

            return ConfigurationManager.AppSettings[key];
        }

        //该属性用来指示自动设置，如果属性上有该Attribute，则不会自动赋值
        [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
        class CustomAttribute : Attribute
        {
        }
    }
}
