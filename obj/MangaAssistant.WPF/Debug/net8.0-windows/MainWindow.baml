   M S B A M L   `   `   `  ����M  IMangaAssistant.WPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:MangaAssistant.WPFMangaAssistant.WPF  5   	   V RMangaAssistant.WPF.Controls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullnNclr-namespace:MangaAssistant.WPF.Controls;assembly=MangaAssistant.WPF.ControlsMangaAssistant.WPF.Controls 5   	   M+clr-namespace:MangaAssistant.WPF.ViewModelsMangaAssistant.WPF.ViewModels  5   	   M+clr-namespace:MangaAssistant.WPF.ConvertersMangaAssistant.WPF.Converters  5	   	   #    MangaAssistant.WPF.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation       5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089<x,http://schemas.microsoft.com/winfx/2006/xaml      5   	   Bd2http://schemas.microsoft.com/expression/blend/2008      5   	   Lmc;http://schemas.openxmlformats.org/markup-compatibility/2006      5   	   4local clr-namespace:MangaAssistant.WPF      5   	   econtrolsNclr-namespace:MangaAssistant.WPF.Controls;assembly=MangaAssistant.WPF.Controls      5   	   D
viewmodels+clr-namespace:MangaAssistant.WPF.ViewModels      5   	   D
converters+clr-namespace:MangaAssistant.WPF.Converters      5	   	     � Title$  Manga Assistant��5
   	   $
��1000��5   	   $
��1200��5   	    � WindowStartupLocation$ CenterScreen=�5   	   ���#1E1E1E � 	Resources 5          BooleanToVisibilityConverter>   8MangaAssistant.WPF.Converters.PathToImageSourceConverter  PathToImageSourceConverter  MenuButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType �� Triggers P� Property	 P� Value��%u  &	        &	      &	 &     �� 5   
   6'     5   
   60   �� 5   
   " ��6(   .�5      ��  � ��
 ���� 6+   �� 5       � ��
 �����6+   �� 5       � ��$ 40��6'   �� 5       � ��$	 0,5q�6'   �� 5       � ��$
 Left=�6;   �� 5       � ��$
 15,0q�6(   �� 5       � �� 5      �� 5      " ��6&   .R�5       �� #��z��6!   $��0q�5!   !   ._�5"      �� #��z��6/   $��Center=�5#   -   5$       5%      P� 5&       �@J�IsMouseOver$
	 True��6=   . �5'   "   ��  � �� �#3E3E426?   5(      5*      5,      5-      5.      .��50      � 
 � ColumnDefinitions
 51   
   �� 52      $	��250 �6   �� 53      $��* �6   .+�56   
   � $��0��6    � RowDefinitions 57      �� 58      $��60 �6    �� 59      $��* �6    .+�5=      � ���#252526.+�5>      ��  �� Orientation$ 
Horizontal=�6   $
��16,0q�5?      .+�5@      �� $��/Assets/logo.png��6   $��32��5A      $��32��5B      $��0,0,12,0q�5C      �� 5D      $��Manga Assistant��6    
�������$��18(�5F      $��Center=�5G      5H      5I      5J      �� 5M   
   $��0��6   ���#252526���#3E3E42$��0,0,1,0q�5P      ._�5Q      �� $��16q�6   .+�5S      �� $��LIBRARY��6   ���#B0B0B0$��12(�5U      $��8,0,0,8q�5V      �� 5W      -   5        #��[ 5W      .��5Y      �� $ 
Horizontal=�6!   .+�5Z      �� $	����6$   $��Segoe MDL2 Assets)�5[   "   $��0,0,8,0q�5\   "   �� 5]      $��Library View��6$   5^      5_      �� 5`      -   5        #��[ 5`      .��5b      �� $ 
Horizontal=�6!   .+�5c      �� $	����6$   $��Segoe MDL2 Assets)�5d   "   $��0,0,8,0q�5e   "   �� 5f      $��Settings��6$   5g      5h      5i      5j      � 5m   
   $��1��6    5n      �� 5o      $��60 �6    �� 5p      $
��Auto �6    �� 5q      $��* �6    .+�5u      � ���#252526
 5v      �� 5w      $��* �6'   �� 5x      $
��Auto �6'   .+�5|      �� ���#333337 �� CornerRadius$ 4��5}      $��20,15q�5~      $��30��5      ._�5�      � .+�5�      �� 
������ 
�������$��0q�5�   "   $��Center=�5�   "   $��	30,0,10,0q�5�   "   �� 5�      $
��🔍��6$   ���#808080$��Center=�5�   "   $��10,0,0,0q�5�   "   5�      5�      � 5�      $��1��6   $��	10,0,20,0q�5�      y�5�      �� *
IsScanning+ �� 	Converter# [  .+�5�      �� $ Vertical=�6!   $��Center=�5�      .+�5�      �� $��Scanning Library...��6$   
�������$��Center=�5�   "   $��0,0,0,4q�5�   "   �� 5�      $ 
Horizontal=�6%   $��Center=�5�   #   .+�5�      �� 
���������6(   �� *ScannedSeries+5�   &   �� 5�      $��/��6(   
�������$	��4,0q�5�   &   �� 5�      
���������6(   �� *TotalSeries+5�   &   5�      � 5�       � Maximum$ 1Q�5�   $   $��2��5�   $   $	��150��5�   $   $��0,4,0,0q�5�   $   ���#333337���#3399FF � Value 5�   &   �� *ScanProgress+5�   $   5�      5�      5�      �� 5�      $��1��6   ���#2D2D30$ 
Horizontal=�5�      $��0,0,0,1q�5�      5�      � 5�      $��2��6   .+�5�      �� -   5         ��Name$ LibraryView��5�      $
��Auto=�5�      $��Disabled=�5�      .��5�      �� ��6#   �� *Series+��5�      �� 5�      .R�5�   "   � $��15q�6,   5�      ��5�      �� 5�      .R�5�   "   +  %MangaAssistant.WPF.Controls.MangaCard  -   5        $	��133��5�   %   $	��186��5�   %   $��8q�5�   %      Title 5�   %   �� *Title+   Progress 5�   %   �� *
Progress+   CoverSource 5�   %   �� *	CoverPath+# [    UnreadChapters 5�   %   �� *ChapterCount+5�   %   5�      5�      5�      � 5�      -   5         �Name$ SeriesDetailContainer��5�      ���#1E1E1E$y�	Collapsed=�5�      � 5�      -   5        $ SettingsContainer��5�      $y�	Collapsed=�62   5�      5�      5�      5�      