﻿#pragma checksum "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "6AECFC7A944DB1B8664314E28AABCB69569B26D0"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MangaAssistant.WPF;
using MangaAssistant.WPF.Controls;
using MangaAssistant.WPF.Converters;
using MangaAssistant.WPF.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace MangaAssistant.WPF {
    
    
    /// <summary>
    /// MainWindow
    /// </summary>
    public partial class MainWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {
        
        
        #line 129 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchBox;
        
        #line default
        #line hidden
        
        
        #line 184 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ScrollViewer LibraryView;
        
        #line default
        #line hidden
        
        
        #line 212 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SeriesDetailContainer;
        
        #line default
        #line hidden
        
        
        #line 218 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SettingsContainer;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/MangaAssistant.WPF;component/mainwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 88 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.LibraryView_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            
            #line 97 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.Settings_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.SearchBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 135 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            this.SearchBox.KeyDown += new System.Windows.Input.KeyEventHandler(this.SearchBox_KeyDown);
            
            #line default
            #line hidden
            return;
            case 4:
            this.LibraryView = ((System.Windows.Controls.ScrollViewer)(target));
            return;
            case 6:
            this.SeriesDetailContainer = ((System.Windows.Controls.Grid)(target));
            return;
            case 7:
            this.SettingsContainer = ((System.Windows.Controls.Grid)(target));
            return;
            }
            this._contentLoaded = true;
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "9.0.2.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        void System.Windows.Markup.IStyleConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 5:
            
            #line 204 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            ((MangaAssistant.WPF.Controls.MangaCard)(target)).SeriesClicked += new System.Windows.RoutedEventHandler(this.MangaCard_SeriesClicked);
            
            #line default
            #line hidden
            
            #line 205 "..\..\..\..\src\MangaAssistant.WPF\MainWindow.xaml"
            ((MangaAssistant.WPF.Controls.MangaCard)(target)).MetadataUpdateRequested += new System.Windows.RoutedEventHandler(this.MangaCard_MetadataUpdateRequested);
            
            #line default
            #line hidden
            break;
            }
        }
    }
}

