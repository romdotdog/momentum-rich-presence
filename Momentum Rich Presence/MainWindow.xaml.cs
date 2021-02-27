using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DiscordRPC;
using DiscordRPC.Logging;
using Memory;

namespace Momentum_Rich_Presence
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mem mem = new Mem();
        DispatcherTimer updater = new DispatcherTimer();
        public DiscordRpcClient client;
        string previousMap = "";
        DateTime? start = null;

        public MainWindow()
        {
            InitializeComponent();
            updater.Tick += Update;
            updater.Interval = TimeSpan.FromMilliseconds(1000);


            client = new DiscordRpcClient("801172982231859291");

            //Set the logger
            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            //Subscribe to events
            client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };

        }

        string getMap()
        {
            return mem.readString("engine.dll+5E9569", "", 64).Trim(new[] { '\0' });
        }

        /*
        float getTimescale()
        {
            byte[] array = new byte[4];
            mem.readMemory(ref array, "engine.dll+6343F4");
            return BitConverter.ToSingle(array, 0);
        } 
        */

        int getSpeed()
        {
            byte[] array = new byte[4];
            mem.readMemory(ref array, "GameUI.dll+001CBFE8,65C");
            return BitConverter.ToInt32(array, 0);
        }

        private void Inject_Click(object sender, RoutedEventArgs e)
        {
            bool success = mem.OpenGameProcess(mem.getProcIDFromName("hl2"));
            if (success)
            {
                mem.getModules();
                Status.Content = "Injected.";
                Status.Foreground = new SolidColorBrush(Color.FromArgb(255, 166, 226, 46));
                updater.Start();
                client.Initialize();
            }
        }
        private void Update(object sender, EventArgs e)
        {
            if (mem.procs.HasExited)
            {
                Status.Content = "Currently not injected";
                Status.Foreground = new SolidColorBrush(Color.FromArgb(255, 249, 38, 114));
                client.Deinitialize();
                updater.Stop();
            } else
            {
                var map = getMap();
                var speed = getSpeed();
                // var ts = getTimescale();
                Map.Content = "Map: " + map.Replace("_", "___");
                Speed.Content = "Speed: " + speed + " hu";

                if (map != "" && previousMap != map)
                {
                    start = DateTime.UtcNow;
                } else if (map == "")
                {
                    start = null;
                }

                var futurePresence = new RichPresence()
                {
                    Details = map == "" ? "Main menu" : map,
                    State = speed + " hammer units", // ts == 1 ? (speed.ToString() + " hammer units") : $"TASing at {ts}x",
                    Assets = new Assets()
                    {
                        LargeImageKey = "logo",
                        LargeImageText = "Custom momentum presence.",
                    },
                    Timestamps = new Timestamps()
                    {
                        Start = start
                    }
                };

                if (!client.CurrentPresence || client.CurrentPresence.Details != futurePresence.Details || client.CurrentPresence.State != futurePresence.State)
                    client.SetPresence(futurePresence);
                previousMap = map;
            }
        }
    }
}
