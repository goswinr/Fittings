namespace FsEx.Wpf

open System
open System.Windows.Media


module Brush = 


  /// Adds bytes to each color channel to increase brightness, negative values to make darker
  /// result will be clamped between 0 and 255
  let changeLuminace (amount:int) (col:Windows.Media.Color)=
      let inline clamp x = if x<0 then 0uy elif x>255 then 255uy else byte(x)
      let r = int col.R + amount |> clamp      
      let g = int col.G + amount |> clamp
      let b = int col.B + amount |> clamp
      Color.FromArgb(col.A, r,g,b)
  
  /// Adds bytes to each color channel to increase brightness
  /// result will be clamped between 0 and 255
  let brighter (amount:int) (br:SolidColorBrush)  = SolidColorBrush(changeLuminace amount br.Color) 
  
  /// Removes bytes from each color channel to increase darkness, 
  /// result will be clamped between 0 and 255
  let darker  (amount:int) (br:SolidColorBrush)  = SolidColorBrush(changeLuminace -amount br.Color) 


  /// Make it therad safe and faster
  let inline freeze(br:SolidColorBrush)= 
      if not br.IsFrozen then
          if br.CanFreeze then br.Freeze()
          else                 eprintfn "Could not freeze SolidColorBrush: %A" br         
      br
  
  /// Returns a frozen SolidColorBrush
  let make (red,green,blue) = 
      let r = byte red
      let g = byte green
      let b = byte blue
      freeze (new SolidColorBrush(Color.FromRgb(r,g,b)))  
