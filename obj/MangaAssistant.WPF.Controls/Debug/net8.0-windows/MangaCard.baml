   M S B A M L   `   `   `  ����  MangaAssistant.WPF.Controls+    %MangaAssistant.WPF.Controls.MangaCard   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      $	��300��5      $	��200��6     � Cursor$
  Hand��5	       � 	Resources 5
         OnMouseEnter �� 
TargetName �� TargetProperty	 W� To g� Duration  OnMouseLeave��%�  &	        &	 �     �� 5   
   .��5      W� $ 	Transform��6   $ ScaleX�5       1.055      $ 0:0:0.2D�6&   W� 5      $ 	Transform��6   $ ScaleY�5       1.055      $ 0:0:0.2D�6&   5      �� 5   
   .��5      W� $ 	Transform��6   $ ScaleX�5       15      $ 0:0:0.2D�6#   W� 5      $ 	Transform��6   $ ScaleY�5       15      $ 0:0:0.2D�6#   5      5      z�5      �� 5   
   -   5        	Transform��5      .��5!      �� ���#2D2D30 �� CornerRadius$ 8��5"      $
  Hand��5#      ��5$   
   �� 5%       �� 
TargetType" ��6    �� Triggers 5&      P� 5'      	 P� Property	 �@J�IsMouseOver
 P� Value$

 True��65   . �5(      ��  �� Property � �� �� Value �#3E3E4267   5)      5+      ._�5.   
   � .+�50      �� $ 8��6   $
}�True��51      ._�52      �� -   5         ��Name$ CoverImageControl��52      $��UniformToFill=�54      ��53      �� *CoverSource+ �� RelativeSource ��  �� AncestorType UserControl54      55      �� 58      ���	#80000000$ 8��59      ._�5:      � .+�5<      �� 
�������$��14(�5>      $��SemiBold"�5?      $
��Wrap=�5@      $��10,10,10,40q�5A      $��Bottom=�5B      ��5<       �� *Title+ ��  UserControl5B      � 5E      $��Bottom=�6   $��10q�5F      .+�5G      � $��4��5H   $   ���	#40FFFFFF���#3399FF � Value 5G   &   �� *
Progress+ ��  UserControl5J   $   �� 5M      ���#3399FF$ 10��5N   !   $��Right=�5O   !   $	��Top=�5P   !   $��	0,-20,0,0q�5Q   !   $��20��5R   !   $��20��5S   !    �� Padding$	 6,0q�5T   !   ._�5U      �� 
�������$��12(�5W   &   $��Center=�5X   &   $��Center=�5Y   &   ��5U   (   �� *UnreadChapters+ ��  UserControl5Y   &   5Z      5[      5\      5]      5^      5_      5`      