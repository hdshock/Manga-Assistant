   M S B A M L   `   `   `  ����   MangaAssistant.WPF.Controls+    %MangaAssistant.WPF.Controls.MangaCard   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     @d2http://schemas.microsoft.com/expression/blend/2008     $	��300��$	��200��  � Cursor$
  Hand�� � 	Resources    OnMouseEnter �� 
TargetName �� TargetProperty	 W� To g� Duration  OnMouseLeave��%  &	        &	 z     �� .��W� $ 	Transform��$ ScaleX� 1.05$ 0:0:0.2D�W� $ 	Transform��$ ScaleY� 1.05$ 0:0:0.2D��� .��W� $ 	Transform��$ ScaleX� 1$ 0:0:0.2D�W� $ 	Transform��$ ScaleY� 1$ 0:0:0.2D�z��� -   	Transform��.���� ���#2D2D30 �� CornerRadius$ 8��$
  Hand������  �� 
TargetType" �� �� Triggers P� 	 P� Property	 �@J�IsMouseOver
 P� Value$

 True��. ���  �� Property � �� �� Value �#3E3E42._�� .+��� $ 8��$
}�True��._��� -    ��Name$ CoverImageControl��$��UniformToFill=����� *CoverSource+ �� RelativeSource ��  �� AncestorType UserControl�� ���	#80000000$ 8��._�� .+��� 
�������$��14(�$��SemiBold"�$
��Wrap=�$��10,10,10,40q�$��Bottom=����� *Title+ ��  UserControl� $��Bottom=�$��10q�.+�� $��4�����	#40FFFFFF���#3399FF � Value �� *
Progress+ ��  UserControl�� ���#3399FF$ 10��$��Right=�$	��Top=�$��	0,-20,0,0q�$��20��$��20�� �� Padding$	 6,0q�._��� 
�������$��12(�$��Center=�$��Center=����� *UnreadChapters+ ��  UserControl