﻿#pragma checksum "C:\Users\theunth\SKDRV\SkyDrive\Werk\TWorks\VTrakt\WPtrakt\ViewEpisode.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "526173772C3FF8A0E586278A882DCD22"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.269
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
    
    
    public partial class ViewEpisode : Microsoft.Phone.Controls.PhoneApplicationPage {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal Microsoft.Phone.Controls.Panorama EpisodePanorama;
        
        internal System.Windows.Controls.ProgressBar progressBarLoading;
        
        internal System.Windows.Controls.TextBlock loadingTextSuggest;
        
        internal System.Windows.Controls.TextBlock ImdbButton;
        
        internal System.Windows.Controls.ListBox HistoryList;
        
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
            System.Windows.Application.LoadComponent(this, new System.Uri("/WPtrakt;component/ViewEpisode.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.EpisodePanorama = ((Microsoft.Phone.Controls.Panorama)(this.FindName("EpisodePanorama")));
            this.progressBarLoading = ((System.Windows.Controls.ProgressBar)(this.FindName("progressBarLoading")));
            this.loadingTextSuggest = ((System.Windows.Controls.TextBlock)(this.FindName("loadingTextSuggest")));
            this.ImdbButton = ((System.Windows.Controls.TextBlock)(this.FindName("ImdbButton")));
            this.HistoryList = ((System.Windows.Controls.ListBox)(this.FindName("HistoryList")));
        }
    }
}

