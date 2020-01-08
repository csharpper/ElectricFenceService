using ElectricFenceService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServerHost
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //ShipMgr.Instance.Start("10.33.9.50", 8020);
            int listenPort = 0;
            if(int.TryParse(ConfigData.ListenPort, out listenPort) && listenPort > 0 && listenPort < 65535)
                ElectricFenceService.Fence.FenceTrackMgr.Instance.Start(listenPort);
            int aisPort = 0;
            if (!string.IsNullOrEmpty(ConfigData.AisHost) && int.TryParse(ConfigData.AisPort, out aisPort) && aisPort > 0 && aisPort < 65535)
                ShipMgr.Instance.Start(ConfigData.AisHost, aisPort);
            int webSocketPort = 0;
            if (int.TryParse(ConfigData.WebSocketPort, out webSocketPort) && webSocketPort > 0 && webSocketPort < 65535)
                HttpServerMgr.Instance.Start(webSocketPort);
        }
    }
}
