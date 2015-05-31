using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Stack_CPU
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string HEXFORMAT = "x{0} ({1})";
        private const string MEMFORMAT = "x{0}: {1}";
        private const string COMPILEER = "Compile error at address x{0}: {1}";
        private const string RUNTIMEER = "Runtime error at address x{0}: {1}";

        private StackCPU stackcpu;
        private int len;

        public MainPage()
        {
            this.InitializeComponent();
            stackcpu = new StackCPU();
            len = 2;
        }

        private async void RaiseError(string error)
        {
            await (new MessageDialog(error, "Error").ShowAsync());
        }

        private async void RaiseHaltMessage()
        {
            await (new MessageDialog("End of Program", "Halt").ShowAsync());
        }

        private void RaiseRuntimeError()
        {
            RaiseError(string.Format(RUNTIMEER, stackcpu.LastErrorAddr.ToString("X" + len), stackcpu.LastError));
        }

        private void abbCompile_Click(object sender, RoutedEventArgs e)
        {
            lstMem.Items.Clear();
            stackcpu.MemSize = GetBlockSize();
            List<string> lst = tbxCode.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
            stackcpu.Lines = lst;
            if (stackcpu.Compile())
            {
                stackcpu.ClearStack();
                ReloadMemory();
                ReloadStack();
            }
            else
            {
                RaiseError(string.Format(COMPILEER, stackcpu.LastErrorAddr.ToString("X" + len), stackcpu.LastError));
            }
        }

        private void abbReset_Click(object sender, RoutedEventArgs e)
        {
            stackcpu.ClearStack();
            ReloadStack();
            ReloadMemory();
        }

        private void ReloadStack()
        {
            lstDS.Items.Clear();
            lstRS.Items.Clear();

            List<int> tmp = stackcpu.DataStack;
            foreach (byte t in tmp)
            {
                lstDS.Items.Add(string.Format(HEXFORMAT, t.ToString("X2").ToUpper(), (sbyte)t));
            }

            tmp = stackcpu.ReturnStack;
            foreach (byte t in tmp)
            {
                lstRS.Items.Add(string.Format(HEXFORMAT, t.ToString("X2").ToUpper(), (sbyte)t));
            }

            int pc = stackcpu.PC;

            tbxPC.Text = "PC: " + string.Format(HEXFORMAT, pc.ToString("X" + len).ToUpper(), pc);

            ScrollStackToBottom();
        }

        private void ScrollStackToBottom()
        {
            if (lstDS.Items.Count > 0)
            {
                lstDS.SelectedIndex = lstDS.Items.Count - 1;
                lstDS.ScrollIntoView(lstDS.Items[lstDS.Items.Count - 1]);
            }
            if (lstRS.Items.Count > 0)
            {
                lstRS.SelectedIndex = lstRS.Items.Count - 1;
                lstRS.ScrollIntoView(lstRS.Items[lstRS.Items.Count - 1]);
            }
        }

        private void ScrollMemToPC()
        {
            if (stackcpu.PC >= 0 && lstMem.Items.Count >= stackcpu.PC + 1)
            {
                lstMem.SelectedIndex = stackcpu.PC;
                lstMem.ScrollIntoView(lstMem.Items[stackcpu.PC]);
            }
        }

        private int GetBlockSize()
        {
            return (int) Math.Pow(2, cbxBlock.SelectedIndex) * 16;
        }

        private void ReloadMemory()
        {
            string t;
            int l = stackcpu.MemSize;

            // resize list
            while (lstMem.Items.Count < l) lstMem.Items.Add("");

            for (int i = 0; i < l; ++i)
            {
                int m = stackcpu.Memory(i);
                int p = StackCPU.opGetCode(m);
                t = StackCPU.opGetOps(m);
                if (p == 0xffff || t == "")
                {
                    byte r = (byte)m;
                    t = string.Format(HEXFORMAT, r.ToString("X2").ToUpper(), r.ToString());
                }
                lstMem.Items[i] = string.Format(MEMFORMAT, i.ToString("X" + len).ToUpper(), t);
            }
            ScrollMemToPC();
        }

        private void abbRun_Click(object sender, RoutedEventArgs e)
        {
            bool r = stackcpu.Run();
            ReloadStack();
            ReloadMemory();
            if (!r) RaiseRuntimeError();
        }

        private void abbStepOver_Click(object sender, RoutedEventArgs e)
        {
            if (!stackcpu.Halt)
            {
                bool r = stackcpu.StepOver();
                ReloadStack();
                ReloadMemory();
                if (!r) RaiseRuntimeError();
            }
            if (stackcpu.Halt) RaiseHaltMessage();
        }

        private void abbStepInto_Click(object sender, RoutedEventArgs e)
        {
            if (!stackcpu.Halt)
            {
                bool r = stackcpu.StepInto();
                ReloadStack();
                ReloadMemory();
                if (!r) RaiseRuntimeError();
            }
            if (stackcpu.Halt) RaiseHaltMessage();
        }

        private void lstMem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollMemToPC();
        }

        private void lstDS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollStackToBottom();
        }

        private void lstRS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollStackToBottom();
        }
    }
}
