﻿using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Windows;

namespace LXtory
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "LXtory_singleFtr4SgH37";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                SingleInstance<App>.Cleanup();
            }
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            return MainLogic.ReadCommandLineArgs(args);
        }
    }
}
