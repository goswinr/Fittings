#r @"PresentationCore"
#r @"PresentationFramework"
#r @"WindowsBase"
//#r @"System.Xaml"
//#r @"UIAutomationTypes"
#r @"C:\GitHub\FsEx.Wpf\bin\Release\net472\FsEx.Wpf.dll"

open System
open System.Windows
open System.Windows.Controls

open FsEx.Wpf

let mutable win :Window = null

Sync.doSync( fun ()  ->  
    win <- PositionedWindow("GosTest") 
    win.Show() 
    ) 

Sync.doSync( fun ()  ->  
    win.Content <- Label(Content="Hello, World") 
    ) 
    
Sync.doSync( fun ()  ->  
    for i = 0 to 40 do 
        Threading.Thread.Sleep 20
        win.Left <- win.Left - float (i) 
        win.Topmost <- true
    for i = 0 to 40 do 
        Threading.Thread.Sleep 20
        win.Left <- win.Left + float (i) 
        win.Topmost <- true    
        
    ) 