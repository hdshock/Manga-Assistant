   M S B A M L   `   `   `  ����  MangaAssistant.WPF.Controls+    %MangaAssistant.WPF.Controls.MangaCard   5      W SPresentationCore, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=7.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5      R NSystem.Xaml, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      -   5          >�Name$
  Root��5
      $	��300��5      $	��200��6    � Cursor$
 Hand��5	       � 	Resources 5         OnMouseEnter �� 
TargetName �� TargetProperty	 W� To g� Duration  OnMouseLeave��%�  &	        &	 �     �� 5   
   .��5      W� $ 	Transform��6   $ ScaleX�5       1.055      $ 0:0:0.2D�6&   W� 5      $ 	Transform��6   $ ScaleY�5       1.055      $ 0:0:0.2D�6&   5      �� 5   
   .��5      W� $ 	Transform��6   $ ScaleX�5       15      $ 0:0:0.2D�6#   W� 5      $ 	Transform��6   $ ScaleY�5       15      $ 0:0:0.2D�6#   5      5      z�5      �� 5   
   -   5        	Transform��5      .��5"      �� -   5        
������ $��0q�5$      $��0q�5%      .��5&   
   �  � RowDefinitions 5'      �� 5(      $��* �6    �� 5)      $
��Auto �6    .+�5-      �� $��0��6    �� CornerRadius$ 8��5.      $
}�True��5/      ._�50      �� $��UniformToFill=�51      ��50      �� *CoverSource+	 �� ElementName$
	 Root��51      52      �� 55      $��1��6   $��0,8,0,0q�56      .+�57      �� 
�������$
��Wrap=�59      $��CharacterEllipsis=�5:      $��40��5;      ��57      �� *Title+$
	 Root��5;      � 5=      $��0,4,0,0q�6   
 � ColumnDefinitions
 5>      �� 5?      $��* �6+   �� 5@      $
��Auto �6+   .+�5C      � $��2��5D   !   ���#333337���#3399FF � Value 5C   "   �� *
Progress+$
	 Root��5F   !   �� 5H      $��1��6    ���#B0B0B0$��12(�5K      $��8,0,0,0q�5L      ��5I      �� *UnreadChapters+$
	 Root�� �� StringFormat$	 {0}��5L      5M      5N      5O      5P      5Q      