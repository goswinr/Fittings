namespace FsEx.Wpf

open System
open System.Windows


/// A class holding a resizable Window that remebers its position even after restarting.
/// The appName in constructor will be used as Title and to save a settings.txt file in User's Appdata folder
type PositionedWindow (appName) as this = 
    inherit Windows.Window() 

    let appFileName = 
        if String.IsNullOrWhiteSpace appName then "Unnamed FsEX PositionedWindow"
        else 
            let sb = new Text.StringBuilder()
            for c in appName.Trim() do // make a valid file name, not allowed < > : " / \ | ? *
                if (c >= '0' && c <= '9') 
                || (c >= 'A' && c <= 'Z') 
                || (c >= 'a' && c <= 'z') 
                ||  c = '.'  ||  c = '_'   ||  c = ' ' ||  c = '-' ||  c = '!' 
                ||  c = '&'  ||  c = '='
                ||  c = '+'  ||  c = '('   ||  c = ')' ||  c = '[' ||  c = ']'  then  sb.Append(c) |> ignore
            sb.ToString()
    
    let settings = Settings(appFileName)

    /// the owning window
    let owner = IntPtr.Zero
 
    let mutable setMaxAfterLoading = false

    let mutable isMinOrMax = false     
    
    do       
        if not (String.IsNullOrWhiteSpace appName) then base.Title <- appName
        base.ResizeMode  <- ResizeMode.CanResize  
              
        //-------------------------------------------------------------------------
        // -  all below code is for load and safe window location and size ---
        //-------------------------------------------------------------------------
        
        
        // (1) first restore normal size
        base.WindowStartupLocation <- WindowStartupLocation.Manual
        let winTop    = settings.GetFloat "WindowTop"    0.0
        let winLeft   = settings.GetFloat "WindowLeft"   0.0 
        let winHeight = settings.GetFloat "WindowHeight" 800.0
        let winWidth  = settings.GetFloat "WindowWidth"  800.0

        //let maxW = float <| Array.sumBy (fun (sc:Forms.Screen) -> sc.WorkingArea.Width)  Forms.Screen.AllScreens  // needed for dual screens ?, needs wins.forms
        //let maxH = float <| Array.sumBy (fun (sc:Forms.Screen) -> sc.WorkingArea.Height) Forms.Screen.AllScreens // https://stackoverflow.com/questions/37927011/in-wpf-how-to-shift-a-win-onto-the-screen-if-it-is-off-the-screen/37927012#37927012
            
        let offTolerance = 25.0 // beeing 20 pixel off screen is still good enough for beeing on screen and beeing draggable

        let maxW = SystemParameters.VirtualScreenWidth   + offTolerance
        let maxH = SystemParameters.VirtualScreenHeight  + offTolerance // somehow a window docked on the right is 7 pix bigger than the screen ?? // TODO check dual screens !!
            
        base.Top <-     winTop 
        base.Left <-    winLeft 
        base.Height <-  winHeight
        base.Width <-   winWidth

        // (2) only now set the maximise flag or correct position if off the screen
        if settings.GetBool "WindowIsMax" false then
            //base.WindowState <- WindowState.Maximized // always puts it on first screen, do in loaded event instead
            setMaxAfterLoading <- true            
            isMinOrMax  <- true

        elif  winTop  < -offTolerance || winHeight + winTop  > maxH then 
            eprintfn "FsEx.PositionedWindow:Could not restore previous Window position:"
            eprintfn "FsEx.PositionedWindow: winTopPosition: %.1f  + winHeight: %.1f  = %.1f that is bigger than maxH: %.1f + %.1f tolerance" winTop winHeight   ( winHeight + winTop ) SystemParameters.VirtualScreenHeight offTolerance
            base.WindowStartupLocation <- WindowStartupLocation.CenterScreen                
            base.Height <- 600.0                
            base.Width  <- 600.0

        elif winLeft < -offTolerance || winWidth  + winLeft > maxW then
            eprintfn "FsEx.PositionedWindow: Could not restore previous Window position:"
            eprintfn "FsEx.PositionedWindow: winLeftPosition: %.1f  + winWidth: %.1f = %.1f that is bigger than maxW: %.1f + %.1f tolerance" winLeft winWidth ( winWidth +  winLeft) SystemParameters.VirtualScreenWidth offTolerance
            base.WindowStartupLocation <- WindowStartupLocation.CenterScreen
            base.Height <- 600.0                
            base.Width  <- 600.0
        
        //Turns out that we cannot maximize the window until it's loaded. 
        //http://mostlytech.blogspot.com/2008/01/maximizing-wpf-window-to-second-monitor.html
        this.Loaded.Add (fun _ -> if setMaxAfterLoading then this.WindowState <- WindowState.Maximized)

        this.LocationChanged.Add(fun e -> // occures for every pixel moved
            async{
                // normally the state change event comes after the location change event but before size changed. async sleep in LocationChanged prevents this
                do! Async.Sleep 200 // so that StateChanged event comes first
                if this.WindowState = WindowState.Normal &&  not isMinOrMax then 
                    if this.Top > -500. && this.Left > -500. then // to not save on minimizing on minimized: Top=-32000 Left=-32000 
                        settings.SetFloatDelayed "WindowTop"  this.Top  100 // get float in statechange maximised needs to access this before 350 ms pass
                        settings.SetFloatDelayed "WindowLeft" this.Left 100
                        settings.Save ()
                }
                |> Async.StartImmediate
            )

        this.StateChanged.Add (fun e ->
            match this.WindowState with 
            | WindowState.Normal -> 
                // because when Window is hosted in other App the restore from maximised does not remember the previous position automatically                
                this.Top <-     settings.GetFloat "WindowTop"    0.0
                this.Left <-    settings.GetFloat "WindowLeft"   0.0 
                this.Height <-  settings.GetFloat "WindowHeight" 800.0
                this.Width <-   settings.GetFloat "WindowWidth"  800.0
                settings.SetBool  "WindowIsMax" false
                isMinOrMax <- false
                settings.Save ()
                
            | WindowState.Maximized ->
                // normally the state change event comes after the location change event but before size changed. async sleep in LocationChanged prevents this
                isMinOrMax  <- true
                settings.SetBool  "WindowIsMax" true
                settings.Save  ()    
                        

            |WindowState.Minimized ->                 
                isMinOrMax  <- true
            |wch -> 
                eprintfn "FsEx.PositionedWindow: unknown WindowState State change=%A" wch
                isMinOrMax  <- true
            )

        this.SizeChanged.Add (fun e -> // does no get trigger on maximising 
            if this.WindowState = WindowState.Normal &&  not isMinOrMax  then 
                settings.SetFloatDelayed "WindowHeight" this.Height 100
                settings.SetFloatDelayed "WindowWidth"  this.Width  100
                settings.Save ()                
            )
   
   
    /// Get or Set the native Window Handle that owns this window. 
    /// Use if this Window is hosted in another native app  (via IntPtr).
    /// So that this window opens and closes at the same time as the main host window.
    member this.OwnerHandle
        with get () = owner
        and set ptr =
            if ptr <> IntPtr.Zero then
                Interop.WindowInteropHelper(this).Owner <- ptr
    
    
    