   M S B A M L   `   `   `  ����W  SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35U<clr-namespace:System.Windows.Media;assembly=PresentationCoreSystem.Windows.Media  5      V RMangaAssistant.WPF.Controls, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null_4clr-namespace:MangaAssistant.WPF.Controls.Converters&MangaAssistant.WPF.Controls.Converters 5      ,   &MangaAssistant.WPF.Controls.SeriesPage   5      \ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35J 9http://schemas.microsoft.com/winfx/2006/xaml/presentation        5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089<x,http://schemas.microsoft.com/winfx/2006/xaml       5      Lmc;http://schemas.openxmlformats.org/markup-compatibility/2006       5      Bd2http://schemas.microsoft.com/expression/blend/2008       5      Pmedia<clr-namespace:System.Windows.Media;assembly=PresentationCore       5      M
converters4clr-namespace:MangaAssistant.WPF.Controls.Converters       5        � 	Resources  5         ActionButtonStyle �� 
TargetType �� Property �� Value �� 
TargetType ��Name �� CornerRadius J� Opacity �� Triggers	 P� Property
 P� Value N� EnterActions �� 
TargetName �� TargetProperty	 W� To g� Duration N� ExitActions  ChapterItemStyle �� PaddingJ  DMangaAssistant.WPF.Controls.Converters.FilePathToFolderNameConverter ! FilePathToFolderNameConverter  ChapterItemTemplate � ColumnDefinitions �� StringFormat �� 	Converter � ToolTip � ContextMenu  GridViewHeaderStyle �� Triggers��%  &	        &	 �    &	 �    &	 �    7  7   &	 L
    �� 5   
   " ��6*   .�5      ��  � �� �#3333376+   �� 5       � ��
 �����6+   �� 5       � ��$ 40��6&   �� 5       � ��$ 40��6'   �� 5       � ��$	 4,0q�6'   �� 5       �@�Cursor$
 Hand��6'   �� 5       � �� 5      �� 5      " ��6&   .R�5      �� $ ButtonBorder��5   !   #��z��5   !   $ 8��5   !   ._�5      � .+�5   "   �� $��Center=�63   $��Center=�5   1   �� 5   "   $ HoverOverlay��6)   ���#3399FF$ 0Q�5    )   $ 8��5!   )   5"      5#       5$      P� 5%      	 �@J�IsMouseOver$

 True��6=    5&   "   �� 5'   &   .��5(   *   �� .��5)   .   W� $ HoverOverlay��6>   $ Opacity�5*   <    0.25+   <   $ 0:0:0.2D�6E   5,   +   5-   '    5/   "   �� 50   &   .��51   *   �� .��52   .   W� $ HoverOverlay��6>   $ Opacity�53   <    054   <   $ 0:0:0.2D�6C   55   +   56   '   58      5:      5<      5=      �� 5@   
   " ��6)   .�5A      ��  � �� �#2525266+   �� 5B       � ��
 �����6+   �� 5C       � ��$ 10q�6(   �� 5D       � ��$	 0,2q�6'   �� 5E       � �� 5F      �� 5G      " ��6&   .R�5H      �� #��z��6!   $ 4��5I   !   # z��5J   !   ._�5K      �� 5L       5M      P� 5N      	 �@J�IsMouseOver$

 True��6=   . �5O   "   ��  � �� �#3E3E426?   5P      5R      5T      5U        5X   
   63   �� 5[   
   .R�5\      �  5]      �� 5^      $	��100 �6'   �� 5_      $	��300 �6'   �� 5`      $	��100 �6'   �� 5a      $	��300 �6'   �� 5b      $��50 �6'   .+�5e      �� $��0��6   
�������$��Center=�5h      $��Center=�5i      ��5f      �� *Number+$ Chapter {0}��5i      �� 5k      $��1��6   
�������$��Center=�5n      $��Center=�5o      $��CharacterEllipsis=�5p      ��5l      �� *Title+5p      �� 5r      $��2��6   
�������$��Center=�5u      $��Center=�5v      ��5s      �� *	PageCount+5v      �� 5x      $��3��6   
�������$��Center=�5{      $��Center=�5|      $��CharacterEllipsis=�5}      ��5y      �� *
FilePath+8    5~      �� *
FilePath+�� 5�      -   5        $��4��5�      8�� $��30��5�      $��30��6$   $��Center=�5�       5�      �� 5�      .@�5�      m� -   5        $��Download��5�   '   5�   '   m� 5�      -   5        $��Delete��5�   '   5�   '   5�      .��5�      �� $	��⋮��6    $��20(�5�      $��Center=�5�      5�      5�      5�      �� 5�   
   " ��6,   .�5�      ��  � �� �#2525266+   �� 5�       � ��
 �����6+   �� 5�       � ��$
 10,8q�6(   �� 5�       � �� �#3E3E426,   �� 5�       � ��$ 0,0,0,1q�60   �� 5�       � ��$ Center=�6;   �� 5�       � �� 5�      �� 5�      " ��6&   .R�5�      � .+�5�      �� #��z��6%   #��z��5�   %   #��z��5�   %   ._�5�   "   �� #��z��63   $��Center=�5�   1   #��z��5�   1   5�      5�      5�      5�       5�      P� 5�      	 �@��Role$
 Padding=�6*   . �5�      ��  � ��$ 0q�68   �� 5�       � ��
 ���� 63   5�      5�      5�      .��5�      �  � RowDefinitions 5�   
   �� 5�      $	��500 �6   �� 5�      $��* �6   .+�5�   
   ��  K� ZIndex$
 9999��6   $��2��6%   .+�5�      ��  �� Orientation$ 
Horizontal=�6    �� Right$ 40��5�      
 �� Top$ 20��5�      .+�5�      �� -   5        #��[  5�      $ Edit Series��5�      .��5�      � .+�5�      �� $��✏️��6$   $��20(�5�   "   $��Center=�5�   "   $��Center=�5�   "   �� 5�      $��E��6$   $��11(�5�   "   $
��Bold"�5�   "   $��
12,-12,0,0q�5�   "   ���#FFD7005�   "   5�      5�      �� 5�      -   5        #��[  5�      $ Refresh Metadata��5�      .��5�      � .+�5�      �� $
��🔄��6$   $��20(�5�   "   $��Center=�5�   "   $��Center=�5�   "   �� 5�      $��R��6$   $��11(�5�   "   $
��Bold"�5�   "   $��
12,-12,0,0q�5�   "   ���#FFD7005�   "   5�      5�      �� 5�      -   5        #��[  5�      $ Search Metadata��5�      .��5�      � .+�5�      �� $
��🔍��6$   $��20(�5�   "   $��Center=�5�   "   $��Center=�5�   "   �� 5�      $��S��6$   $��11(�5�   "   $
��Bold"�5�   "   $��
12,-12,0,0q�5�   "   ���#FFD7005�   "   5�      5�      �� 5�      -   5        #��[  5�      $ Update Series��5�      .��5�      � .+�5�      �� $��⬆️��6$   $��20(�5�   "   $��Center=�5�   "   $��Center=�5�   "   �� 5�      $��U��6$   $��11(�5�   "   $
��Bold"�5�   "   $��
12,-12,0,0q�5�   "   ���#FFD7005�   "   5      5     5     5     � 5  
   .+�5     � .+�5	     �� -   5         ��Name$ BannerImage��5	     $V��Ppack://application:,,,/MangaAssistant.WPF;component/Assets/placeholder-cover.jpg��5
     $��UniformToFill=�5      J� Effect 5     -   'System.Windows.Media.Effects.BlurEffect  5        Radius$ 30Q�6%   5     �� 5     -	   5          ��Name$  BannerOverlay��5     ��5     �� 5     ! �� 
StartPoint$	! 0,0&�6.   " �� EndPoint$	" 0,1&�6?   .��5     � # � Color$# 	#CC000000��6+   $ � Offset$$ 0Q�6=   � 5     $# 	#FF000000��6+   $$ 1Q�6=   5     5     5     � 5     $��40q�6   $��Bottom=�6    5     �� 5     $
��Auto �6'   �� 5     $��* �6'   .+�5#     �� $	��280��6   $	��400��6%   $ 8��5$     $
}�True��5%     $��Bottom=�5&      5'     3   -System.Windows.Media.Effects.DropShadowEffect  5(     %   
BlurRadius$% 20Q�6+   &   ShadowDepth$& 0Q�5)  )   '   Opacity$	' 0.8Q�5*  )   ._�5,     �� -
   5        $ 
CoverImage��5,     $V��Ppack://application:,,,/MangaAssistant.WPF;component/Assets/placeholder-cover.jpg��5-     $��UniformToFill=�5.     5/     50     �� 53     $��1��6   $��40,0,0,0q�54     $��Bottom=�55     .+�57     � .+�59     �� -   5        ( ��Name$( SeriesTitle��59  $   $��Series Title��5:  "   
�������$��24(�5<  "   $
��Bold"�5=  "   $
��Wrap=�5>  "   $��0,0,0,20q�5?  "   5@     � 5C     $��0,20,0,0q�6    5D     �� 5E     $��* �6/   .+�5H     �� .+�5J     �� $ Vertical=�6)   $��0,0,0,15q�6@   .+�5K  "   �� $��Writers��6,   $��14(�5L  *   ���#FF5555$��SemiBold"�5N  *   $��0,0,0,4q�5O  *   �� 5P  "   -   5        $( WritersText��5P  ,   $�� ��5Q  *   $��14(�5R  *   
�������$
��Wrap=�5T  *   5U     �� 5X     $ Vertical=�6)   $��0,0,0,15q�6@   .+�5Y  "   �� $��Genres��6,   $��14(�5Z  *   ���#FF5555$��SemiBold"�5\  *   $��0,0,0,4q�5]  *   �� 5^  "   -   5        $( 
GenresText��5^  ,   $�� ��5_  *   $��14(�5`  *   
�������$
��Wrap=�5b  *   5c     �� 5f     $ Vertical=�6)   $��0,0,0,15q�6@   .+�5g  "   �� $��Publication��6,   $��14(�5h  *   ���#FF5555$��SemiBold"�5j  *   $��0,0,0,4q�5k  *   �� 5l  "   -   5        $( PublicationText��5l  ,   $�� ��5m  *   $��14(�5n  *   
�������5o  *   5p     �� 5s     $ Vertical=�6)   $��0,0,0,15q�6@   .+�5t  "   �� $
��Tags��6,   $��14(�5u  *   ���#FF5555$��SemiBold"�5w  *   $��0,0,0,4q�5x  *   �� 5y  "   -   5        $( TagsText��5y  ,   $�� ��5z  *   $��14(�5{  *   
�������$
��Wrap=�5}  *   5~     �� 5�     $ Vertical=�6)   $��0,0,0,15q�6@   .+�5�  "   �� $��Summary��6,   $��14(�5�  *   ���#FF5555$��SemiBold"�5�  *   $��0,0,0,4q�5�  *   � 5�  "    5�  &   �� 5�  *   $
��Auto �68   �� 5�  *   $
��Auto �68   .+�5�  &   �� -   5        $( SummaryText��5�  0   $��0��5�  .   $�� ��5�  .   $��14(�5�  .   
�������$
��Wrap=�5�  .   $��80��5�  .   $��WordEllipsis=�5�  .   �� 5�  &   -   5        ) ��Name$) ShowMoreButton��5�  -   $��1��5�  -   $��	Show More��5�  -   $
��Left=�5�  -   $��0,4,0,0q�5�  -   $	��8,4q�5�  -   
������ ���#3399FF$��0q�5�  -   * � Cursor$
* Hand��5�  -   ��5�  *   �� 5�  .   " ��64    5�  2   �� 5�  6   + �� Binding+ 6B   �� *Text+, �� ElementName$, SummaryText��- �� Value-  6t   .a�5�  :   ��  � y�$ 	Collapsed=�6W   5�  7   5�  /   5�  '   5�  #   5�     5�     5�     5�     5�     5�     � 5�  
   $��1��6   ���#1E1E1E 5�     �� 5�     $
��Auto �6    �� 5�     $��* �6    .+�5�     �� $ 
Horizontal=�6   $��
40,10,40,0q�5�     .+�5�     �� $��Sort by:��6   
�������$��Center=�5�     $��0,0,10,0q�5�     �� 5�     -   5        . ��Name$. ChapterSortingBox��5�     $	��150��5�     $��30��5�     ���#252526
����������#3E3E42$��1q�5�     $��5,5,5,5q�5�     ��5�     �� 5�     " ��6    .�5�     ��  � �� 5�  "   �� 5�  &   " ��66   .R�5�  *   � .+�5�  .   d� / d�Name$/ ToggleButton��6;   #��z��5�  9   #��z��5�  9   #��z��5�  9   #��z��5�  9   0 d� 	IsChecked0 5�  9   �� 1 �� Path$1 IsDropDownOpen�2 �� Mode$2 TwoWay=�3 �� RelativeSource3 �� *TemplatedParent+��5�  2   �� 5�  6   " d�6F   .R�5�  :   �� #��z��6A   #��z��5�  A   #��z��5�  A   ._�5�  >   �  5�  B   �� 5�  F   �� 5�  F   $��20 �6W   .+�5�  B   �� #��z��6S   #��z��5�  Q   $	��5,2q�5�  Q   $��Center=�5�  Q   F� 5�  B   $��1��6G   
�������$��Center=�5�  G   $��Center=�5�  G   "����        � 	=  	= � z     	5�  G   5�  ?   5�  ;   5�  7   5�  /   �� 5�  .   4 ��Name$4 ContentSite��6?   5 �� SelectionBoxItem#��z5 5�  =   6 �� SelectionBoxItemTemplate#��z6 5�  =   $��Center=�5�  =   $
��Left=�5�  =   $	��5,0q�5�  =   � 5�  .   7 �Name$7 Popup��64   $��Bottom=�5�  4   8 �� IsDropDownOpen#��z8 5�  4   9 � AllowsTransparency$
9 True��5�  4   $|�False��5�  4   .��5�  2   � : � ActualWidth#��z: 67   .+�5�  6   �� ���#252526���#3E3E42$��1q�5�  =   ._�5�  :   �� $	��200��6G   .��5�  >   �� ; K� IsItemsHost$
; True��6I   $��2q�5�  G   5   ;   5  7   5  3   5  /   5  +   5  '   5     5     ��5
     �� 5     " ��6    .�5     ��  � �� �#2525266;   �� 5      � ��
 �����6;   �� 5      � �� 5  "   �� 5  &   " ��66   .R�5  *   �� #��z��61   $	 5,8q�5  1   ._�5  .   �� 5  +    5  *   P� 5  .   	 �@J�IsMouseOver$

 True��6M   . �5  2   ��  � �� �#3E3E426O   5  /   P� 5  .   	 �@��
IsSelected$

 True��6L   . �5  2   ��  � �� �#3E3E426O   5  /   5  '   5     5      .@�5"     �� $��Sort by Chapter Number��6#   
�������< �� 
IsSelected$
< True��5$  "   �� 5%     $��Sort by Folder Location��6#   
�������5&  "   5'     5(     �� 5*     -   5        = ��Name$= ChaptersList��5+     $��1��5*     
������ $��0q�5-     $��40,10,40,20q�5.     #��[ 5/     #��[ 50     > �� View> 51     �� 52     !? �� ColumnHeaderContainerStyle? 53     �� 54     " ��6$   @ �� BasedOn#@ [ 6F   .P�57     �� $��Chapter��6)   A �� Width$	A 100��6:   �� 58     $��Title��6)   $	A 300��68   �� 59     $��Pages��6)   $	A 100��68   �� 5:     $��Folder Location��6)   $	A 300��6B   5;     5=     5>     5?     5@     