﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Syncfusion.Licensing;

namespace GCS_G03
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //Register Syncfusion license
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NjA5MzgxQDMyMzAyZTMxMmUzMFNoTEc5YncvTFRlZzZKN1dldVkwNmQ0L2Z0QWZsMVFQT3pCZHhnaXROK2M9");
        }
    }
}
