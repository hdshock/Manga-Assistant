   M S B A M L   `   `   `  ����   MangaAssistant.WPF.Controls6    0MangaAssistant.WPF.Controls.MetadataSearchDialog   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006       � Title$  Search Metadata��$	��600��$	��500�� � WindowStartupLocation$ CenterOwner=����#1E1E1E.��� $��20q� � RowDefinitions �� $
��Auto ��� $��* ��� $
��Auto �.+�� $��0��$��0,0,0,20q� � ColumnDefinitions �� $��* ��� $
��Auto �.+��� -    ��Name$ 	SearchBox��$��0��$��0,0,10,0q�$��5q����#252526
����������#3E3E42$��1q��� -    ��Name$ SearchButton��$��1��$��Search��$
��20,5q����#3399FF
�������$��0q��� -    ��Name$ ResultsList��$��1��
������ $��0q�����  �� 
TargetType" ��.���  �� Property � ��	 �� Value	 �#252526��  � ��
	 �������  � ��$	 10q���  � ��$		 0,2q�
 �� Triggers
 P�  P� Property �@J�IsMouseOver P� Value$
 True��. ���  � ��	 �#3E3E42P�  �@��
IsSelected$
 True��. ���  � ��	 �#3E3E42���� .R��  �� $
��Auto ��� $��* �.+��� $��50��$��70�� �� CornerRadius$ 4��$
}�True��$��0,0,10,0q�._��� $��UniformToFill=����� *
CoverUrl+�� $��1��.+��� 
�������$��14(�$��SemiBold"�$
��Wrap=����� *Title+�� ���#B0B0B0$��12(�$
��Wrap=�$��0,4,0,0q����� *AlternativeTitle+��  �� Orientation$ 
Horizontal=�$��0,4,0,0q�.+��� ���#FF5555$��12(�$��0,0,8,0q����� *Status+�� ���#B0B0B0$��12(����� *Year+�� $��2��$ 
Horizontal=�$��Right=�$��0,20,0,0q�.+��� -   $ CancelButton��$��Cancel��$��20,10q�$��0,0,10,0q����#333337
�������$��0q��� -   $ SelectButton��$��Select��$��20,10q����#3399FF
�������$��0q�${�False��