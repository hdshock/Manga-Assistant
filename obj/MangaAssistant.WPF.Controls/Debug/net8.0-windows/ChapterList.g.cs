﻿#pragma checksum "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "FDA2351BA7B56788610CEB64DE6EBA9DECF2EF70"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MangaAssistant.WPF.Controls;
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
    /// ChapterList
    /// </summary>
    public partial class ChapterList : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 130 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox SortingComboBox;
        
        #line default
        #line hidden
        
        
        #line 142 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock ChapterCountText;
        
        #line default
        #line hidden
        
        
        #line 146 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Primitives.ToggleButton SortDirectionToggle;
        
        #line default
        #line hidden
        
        
        #line 186 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ItemsControl ChaptersItemsControl;
        
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
            System.Uri resourceLocater = new System.Uri("/MangaAssistant.WPF.Controls;component/chapterlist.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
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
            this.SortingComboBox = ((System.Windows.Controls.ComboBox)(target));
            
            #line 134 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
            this.SortingComboBox.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.SortingComboBox_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.ChapterCountText = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 3:
            this.SortDirectionToggle = ((System.Windows.Controls.Primitives.ToggleButton)(target));
            
            #line 152 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
            this.SortDirectionToggle.Checked += new System.Windows.RoutedEventHandler(this.SortDirectionToggle_Checked);
            
            #line default
            #line hidden
            
            #line 153 "..\..\..\..\src\MangaAssistant.WPF.Controls\ChapterList.xaml"
            this.SortDirectionToggle.Unchecked += new System.Windows.RoutedEventHandler(this.SortDirectionToggle_Unchecked);
            
            #line default
            #line hidden
            return;
            case 4:
            this.ChaptersItemsControl = ((System.Windows.Controls.ItemsControl)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

