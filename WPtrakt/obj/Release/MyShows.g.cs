﻿#pragma checksum "C:\Users\taurgis\SkyDrive\Werk\TWorks\VTrakt\WPtrakt\MyShows.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "CC02AB5FDADC528412F617ACCD540578"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17626
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Microsoft.Phone.Controls;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace WPtrakt {
    
    
    public partial class MyShows : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal Microsoft.Phone.Controls.Panorama MyShowsPanorama;
        
        internal System.Windows.Controls.ProgressBar progressBar;
        
        internal System.Windows.Controls.TextBlock loadingText;
        
        internal System.Windows.Controls.ProgressBar progressBarCalendar;
        
        internal System.Windows.Controls.TextBlock loadingTextCalendar;
        
        internal System.Windows.Controls.ProgressBar progressBarSuggest;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/WPtrakt;component/MyShows.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.MyShowsPanorama = ((Microsoft.Phone.Controls.Panorama)(this.FindName("MyShowsPanorama")));
            this.progressBar = ((System.Windows.Controls.ProgressBar)(this.FindName("progressBar")));
            this.loadingText = ((System.Windows.Controls.TextBlock)(this.FindName("loadingText")));
            this.progressBarCalendar = ((System.Windows.Controls.ProgressBar)(this.FindName("progressBarCalendar")));
            this.loadingTextCalendar = ((System.Windows.Controls.TextBlock)(this.FindName("loadingTextCalendar")));
            this.progressBarSuggest = ((System.Windows.Controls.ProgressBar)(this.FindName("progressBarSuggest")));
        }
    }
}

