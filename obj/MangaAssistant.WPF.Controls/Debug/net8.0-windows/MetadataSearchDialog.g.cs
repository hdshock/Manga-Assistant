﻿#pragma checksum "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "925035398BE4504F3667FC40755492BED2EE368A"
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
    /// MetadataSearchDialog
    /// </summary>
    public partial class MetadataSearchDialog : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 26 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox SearchBox;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SearchButton;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ListView ResultsList;
        
        #line default
        #line hidden
        
        
        #line 121 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button CancelButton;
        
        #line default
        #line hidden
        
        
        #line 130 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button SelectButton;
        
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
            System.Uri resourceLocater = new System.Uri("/MangaAssistant.WPF.Controls;component/metadatasearchdialog.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
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
            this.SearchBox = ((System.Windows.Controls.TextBox)(target));
            
            #line 34 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
            this.SearchBox.KeyDown += new System.Windows.Input.KeyEventHandler(this.SearchBox_KeyDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.SearchButton = ((System.Windows.Controls.Button)(target));
            
            #line 43 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
            this.SearchButton.Click += new System.Windows.RoutedEventHandler(this.SearchButton_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.ResultsList = ((System.Windows.Controls.ListView)(target));
            
            #line 51 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
            this.ResultsList.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.ResultsList_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 4:
            this.CancelButton = ((System.Windows.Controls.Button)(target));
            
            #line 128 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
            this.CancelButton.Click += new System.Windows.RoutedEventHandler(this.OnCancelClick);
            
            #line default
            #line hidden
            return;
            case 5:
            this.SelectButton = ((System.Windows.Controls.Button)(target));
            
            #line 136 "..\..\..\..\src\MangaAssistant.WPF.Controls\MetadataSearchDialog.xaml"
            this.SelectButton.Click += new System.Windows.RoutedEventHandler(this.OnSelectClick);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

