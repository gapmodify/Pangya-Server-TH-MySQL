using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server_Launcher
{
    public partial class Main : Form
    {
        public int irestart = 0;
        public int iping = 0;

        private int authconnect = 0;
        private int loginconnect = 0;
        private int messconnect = 0;
        private int game01connect = 0;
        private int game02connect = 0;

        private Task monitorTask;
        private CancellationTokenSource cancellationTokenSource;

        private Bitmap RED = new Bitmap(22, 22);
        private Bitmap GREEN = new Bitmap(22, 22);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        private const int WS_BORDER = 8388608;
        private const int WS_DLGFRAME = 4194304;
      

        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (Graphics flagGraphics = Graphics.FromImage(RED))
            {
                flagGraphics.FillRectangle(Brushes.Red, 0, 0, 22, 22);
            }
            using (Graphics flagGraphics2 = Graphics.FromImage(GREEN))
            {
                flagGraphics2.FillRectangle(Brushes.Green, 0, 0, 22, 22);
            }

            cancellationTokenSource = new CancellationTokenSource();
            monitorTask = Task.Run(() => CheakServerAsync(cancellationTokenSource.Token));
        }

        private async void quitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            
            await Task.Run(() =>
            {
                ProssKill("GameServer");
                ProssKill("LoginServer");
                ProssKill("AuthServer");
                ProssKill("Messenger");
            });

            if (monitorTask != null)
            {
                try
                {
                    await Task.WhenAny(monitorTask, Task.Delay(2000));
                }
                catch { }
            }

            cancellationTokenSource?.Dispose();
            Close();
        }

        private void LoadApplication(Process compiler, IntPtr handle, int x, int y)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int timeout =  1000;

            compiler.Start();
            while (compiler.MainWindowHandle == IntPtr.Zero)
            {
                System.Threading.Thread.Sleep(10);
                compiler.Refresh();

                if (sw.ElapsedMilliseconds > timeout)
                {
                    sw.Stop();
                    return;
                }
            }

            SetParent(compiler.MainWindowHandle, handle);
            SetWindowPos(compiler.MainWindowHandle, 0, 0, 0, x, y, 0x0040);
        }

      

        public async Task CheakServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        int newLoginconnect = ExeCheaker("LoginServer");
                        int newAuthconnect = ExeCheaker("AuthServer");
                        int newGame01connect = ExeCheaker("GameServer");
                        int newMessconnect = ExeCheaker("Messenger");

                        loginconnect = newLoginconnect;
                        authconnect = newAuthconnect;
                        game01connect = newGame01connect;
                        messconnect = newMessconnect;
                    }, cancellationToken);

                    if (irestart == 1)
                    {
                        if (authconnect == 0)
                        {
                            ProssKill("GameServer");
                            ProssKill("LoginServer");
                            ProssKill("AuthServer");
                            ProssKill("Messenger");

                            await ProssLaunchAsync(1);
                            await ProssLaunchAsync(2);
                            await ProssLaunchAsync(3);
                            await ProssLaunchAsync(4);
                        }
                        else
                        {
                            if (loginconnect == 0)
                            {
                                await ProssLaunchAsync(2);
                            }
                            if (game01connect == 0)
                            {
                                await ProssLaunchAsync(3);
                            }
                            if (messconnect == 0)
                            {
                                await ProssLaunchAsync(4);
                            }
                        }
                    }

                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        public async Task<int> PortCheakerAsync(string host, int port)
        {
            using (TcpClient tc = new TcpClient())
            {
                try
                {
                    await tc.ConnectAsync(host, port);
                    return tc.Connected ? 1 : 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int ExeCheaker(string name)
        {
            Process[] myProcesses = Process.GetProcessesByName(name);
            return myProcesses.Length > 0 ? 1 : 0;
        }

        public void ProssKill(string kill)
        {
            Process[] myProcesses = Process.GetProcessesByName(kill);
            foreach (Process myProcess in myProcesses)
            {
                try
                {
                    myProcess.Kill();
                    myProcess.WaitForExit(500);
                }
                catch { }
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Image = authconnect == 0 ? RED : GREEN;
            pictureBox2.Image = loginconnect == 0 ? RED : GREEN;
            pictureBox5.Image = messconnect == 0 ? RED : GREEN;
            pictureBox3.Image = game01connect == 0 ? RED : GREEN;
        }

        public async Task ProssLaunchAsync(int Type)
        {
            await Task.Run(() =>
            {
                Process compiler = new Process();

                try
                {
                    IntPtr handle = IntPtr.Zero;
                    int width = 0;
                    int height = 0;

                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            switch (Type)
                            {
                                case 1:
                                    handle = tabPage1.Handle;
                                    width = tabPage1.Size.Width;
                                    height = tabPage1.Size.Height;
                                    break;
                                case 2:
                                    handle = tabPage2.Handle;
                                    width = tabPage2.Size.Width;
                                    height = tabPage2.Size.Height;
                                    break;
                                case 3:
                                    handle = tabPage3.Handle;
                                    width = tabPage3.Size.Width;
                                    height = tabPage3.Size.Height;
                                    break;
                                case 4:
                                case 5:
                                    handle = tabPage5.Handle;
                                    width = tabPage5.Size.Width;
                                    height = tabPage5.Size.Height;
                                    break;
                                default:
                                    handle = tabPage3.Handle;
                                    width = tabPage3.Size.Width;
                                    height = tabPage3.Size.Height;
                                    break;
                            }
                        }));
                    }

                    switch (Type)
                    {
                        case 1:
                            compiler.StartInfo.FileName = "AuthServer.exe";
                            compiler.StartInfo.UseShellExecute = true;
                            compiler.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            LoadApplication(compiler, handle, width, height);
                            authconnect = 1;
                            Thread.Sleep(3000);
                            break;

                        case 2:
                            compiler.StartInfo.FileName = "LoginServer.exe";
                            compiler.StartInfo.UseShellExecute = true;
                            compiler.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            LoadApplication(compiler, handle, width, height);
                            loginconnect = 1;
                            break;

                        case 3:
                            compiler.StartInfo.FileName = "GameServer.exe";
                            compiler.StartInfo.UseShellExecute = true;
                            compiler.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            LoadApplication(compiler, handle, width, height);
                            game01connect = 1;
                            break;

                        case 4:
                            compiler.StartInfo.FileName = "Messenger.exe";
                            compiler.StartInfo.UseShellExecute = true;
                            compiler.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            LoadApplication(compiler, handle, width, height);
                            messconnect = 1;
                            break;

                        default:
                            compiler.StartInfo.FileName = "Unknown.exe";
                            compiler.StartInfo.UseShellExecute = true;
                            compiler.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                            LoadApplication(compiler, handle, width, height);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            MessageBox.Show(ex.Message + ": " + compiler.StartInfo.FileName, "Pangya Server Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }));
                    }
                    else
                    {
                        MessageBox.Show(ex.Message + ": " + compiler.StartInfo.FileName, "Pangya Server Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            });
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage1;
            await ProssLaunchAsync(1);
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage1;
            await Task.Run(() => ProssKill("AuthServer"));
        }

        private async void Button4_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
            await ProssLaunchAsync(2);
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
            await Task.Run(() => ProssKill("LoginServer"));
        }

        private async void Button6_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage3;
            await ProssLaunchAsync(3);
        }

        private async void Button5_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage3;
            await Task.Run(() => ProssKill("GameServer"));
        }

        private async void Button10_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
            await ProssLaunchAsync(4);
        }

        private async void Button9_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
            await Task.Run(() => ProssKill("Messenger"));
        }

        private async void Button11_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                ProssKill("GameServer");
                ProssKill("LoginServer");
                ProssKill("AuthServer");
                ProssKill("Messenger");
            });

            await ProssLaunchAsync(1);
            await ProssLaunchAsync(2);
            await ProssLaunchAsync(3);
            await ProssLaunchAsync(4);
        }

        private async void Button12_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                ProssKill("GameServer");
                ProssKill("Messenger");
                ProssKill("LoginServer");
                ProssKill("AuthServer");
            });
        }
    }
}