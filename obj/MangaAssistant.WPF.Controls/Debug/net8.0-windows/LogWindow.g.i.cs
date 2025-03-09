﻿#pragma checksum "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "65E041A0C80F5FB295CFB039CCF3E89765D2402F"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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


namespace MangaAssistant.WPF.Controls {
    
    
    /// <summary>
    /// LogWindow
    /// </summary>
    public partial class LogWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 34 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox LogLevelFilter;
        
        #line default
        #line hidden
        
        
        #line 42 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button ClearLogsButton;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button RefreshLogsButton;
        
        #line default
        #line hidden
        
        
        #line 62 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox LogTextBox;
        
        #line default
        #line hidden
        
        
        #line 82 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock LogStatusText;
        
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
            System.Uri resourceLocater = new System.Uri("/MangaAssistant.WPF.Controls;component/logwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
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
            this.LogLevelFilter = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 2:
            this.ClearLogsButton = ((System.Windows.Controls.Button)(target));
            
            #line 46 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
            this.ClearLogsButton.Click += new System.Windows.RoutedEventHandler(this.ClearLogs_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.RefreshLogsButton = ((System.Windows.Controls.Button)(target));
            
            #line 51 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
            this.RefreshLogsButton.Click += new System.Windows.RoutedEventHandler(this.RefreshLogs_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.LogTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 5:
            this.LogStatusText = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 6:
            
            #line 90 "..\..\..\..\src\MangaAssistant.WPF.Controls\LogWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.CloseButton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

