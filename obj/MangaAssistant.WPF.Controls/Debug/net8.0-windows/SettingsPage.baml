   M S B A M L   `   `   `  ����V  RMangaAssistant.WPF.Controls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null_4clr-namespace:MangaAssistant.WPF.Controls.ViewModels&MangaAssistant.WPF.Controls.ViewModels  5      nNclr-namespace:MangaAssistant.WPF.Controls;assembly=MangaAssistant.WPF.ControlsMangaAssistant.WPF.Controls  5      _4clr-namespace:MangaAssistant.WPF.Controls.Converters&MangaAssistant.WPF.Controls.Converters  5	      .    (MangaAssistant.WPF.Controls.SettingsPage   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation        5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089<x,http://schemas.microsoft.com/winfx/2006/xaml       5      Lmc;http://schemas.openxmlformats.org/markup-compatibility/2006       5      Bd2http://schemas.microsoft.com/expression/blend/2008       5      M
viewmodels4clr-namespace:MangaAssistant.WPF.Controls.ViewModels       5      econtrolsNclr-namespace:MangaAssistant.WPF.Controls;assembly=MangaAssistant.WPF.Controls       5      M
converters4clr-namespace:MangaAssistant.WPF.Controls.Converters       5	        � 	Resources  5      D   >MangaAssistant.WPF.Controls.Converters.InverseBooleanConverter   InverseBooleanConverter  SettingsComboBoxStyle �� 
TargetType �� Property �� Value  SettingsComboBoxItemStyle �� Triggers P� Property P� Value  ActionButtonStyle  WarningButtonStyle �� BasedOn��%s  &	        &	      &	 �     &	 <    &	     7    5   
   6-   �� 5   
   " ��6.   .�5      ��  � ��
 �����6+   �� 5       � ��
 �   �6+   �� 5       � ��$ 30��6'   �� 5       � ��$ 0q�60   5      �� 5   
   " ��62   .�5      ��  � ��
 �����6+   �� 5       � ��
 �   �6+    5      P� 5       �@J�IsMouseOver$
 True��61   . �5      ��  � �� �#e0e0e063   5      P� 5        �@��
IsSelected$
 True��60   . �5!      ��  � �� �#3399FF63   �� 5"       � ��
 �����63   5#      5%      �� 5(   
   " ��6*   .�5)      ��  � ��$ 30��6'   �� 5*       � ��$	 100��6&   �� 5+       � ��$ 10,0,0,0q�6'   �� 5,       � �� �#3399FF6+   �� 5-       � ��
 �����6+   �� 5.       � ��$ 0q�60    5/      P� 50       �@J�IsMouseOver$
 True��61   . �51      ��  � �� �#1a75ff63   52      P� 53       � {�$ False��6/   . �54      ��  � �� �#cccccc63   55      57      �� 5:   
   " ��6+   8   .�5;      ��  � �� �#ff44446+    5<      P� 5=       �@J�IsMouseOver$
 True��61   . �5>      ��  � �� �#ff1a1a63   5?      P� 5@       � {�$ False��6/   . �5A      ��  � �� �#ffcccc63   5B      5D      5E      .��5G      �� $
��Auto=�6   .��5H   
   �� $��40q�6   .+�5J      �� $��Settings��6   
�������$��32(�5L      $
��Bold"�5M      $��0,0,0,30q�5N      �� 5Q      ���#252526 �� CornerRadius$ 8��5R      	 �� Padding$	 20q�5S      $��0,0,0,20q�5T      ._�5U      �� .+�5V      �� $��Library Settings��6    
�������$��20(�5X      $��SemiBold"�5Y      $��0,0,0,20q�5Z      �� 5]      $��Library Folders��6    ���#B0B0B0$��16(�5_      $��0,0,0,10q�5`      �� 5b      
 �� Orientation$
 
Horizontal=�6!   $��0,0,0,20q�5c      .+�5d      �� $	��400��6"   $��30��5e   !   
�������
���   �$��0q�5h   !   $��Center=�5i   !   $
��10,0q�5j   !    �� 
IsReadOnly$
 True��5l   !   ��5k   !   �� *LibraryPath+ �� UpdateSourceTrigger$ PropertyChanged=�5l   !   �� 5m      $��Browse��6!   #��[ 5n   !   ��5o   !   �� *BrowseCommand+5p      �� 5s      $��Library Scan Interval��6    ���#B0B0B0$��16(�5u      $��0,0,0,10q�5v      �� 5w      $
 
Horizontal=�6!   $��0,0,0,20q�5x      .+�5y      �� $	��200��6#   #��[ 5z   !    �� SelectedIndex$ 1��5{   !   .@�5|      �� $��
Every Hour��6+   #��[ 6@   �� 5}      $��Every 12 Hours��6+   #��[ 6D   �� 5~      $��Every 24 Hours��6+   #��[ 6D   �� 5      $��Manual Only��6+   #��[ 6A   5�      �� 5�      $
��Save��6!   #��[ 5�   !   ��5�   !   �� *SaveSettingsCommand+�� 5�      $��
Force Scan��6!   #��[ 5�   !   ��5�   !   �� *ForceScanCommand+5�      �� 5�      $��Cache Management��6    ���#B0B0B0$��16(�5�      $��	0,20,0,10q�5�      �� 5�      $
 
Horizontal=�6!   $��0,0,0,20q�5�      .+�5�      �� $��Reset Cache��6!   #��[ 5�   !   $	��120��5�   !   ��5�   !   �� *ResetCacheCommand+{�5�   !   �� *IsResetting+ �� 	Converter# [   � ToolTip 5�      _� 5�   "   $6��0Clear the library cache and perform a fresh scan��6*   5�      5�      5�      5�      5�      5�      5�      