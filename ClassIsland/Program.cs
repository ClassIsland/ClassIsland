using System;
using System.Collections;
using System.Diagnostics;

namespace ClassIsland
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
