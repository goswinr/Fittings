namespace Fittings

open System
open System.Windows


/// A class holding a re-sizable Window that remembers its position even after restarting.
/// The path in settingsFile will be used to persist the position of this window in a txt file.
/// The errorLogger function will be called if persisting the window size does not work.
type PositionedWindow (settingsFile:IO.FileInfo, errorLogger:string->unit) as this =
    inherit Windows.Window()

    // The ErrorLogger function will be called if the previous Window position could not restore.
    // The window be positioned in the screen center with a size of 600 x 600.

    let settings = new PersistentSettings(settingsFile, errorLogger)

    /// the owning window
    let mutable owner = IntPtr.Zero

    let mutable setMaxAfterLoading = false

    let mutable isMinOrMax = false

    do
        //if not (String.IsNullOrWhiteSpace appName) then base.Title <- appName // might still have special signs
        base.ResizeMode  <- ResizeMode.CanResize

        //-------------------------------------------------------------------------
        //----  all below code is for load and safe window location and size ------
        //-------------------------------------------------------------------------

        // (1) first restore normal size
        base.WindowStartupLocation <- WindowStartupLocation.Manual
        let winTop    = settings.GetFloat ("WindowTop"    , 100.0  )
        let winLeft   = settings.GetFloat ("WindowLeft"   , 100.0  )
        let winHeight = settings.GetFloat ("WindowHeight" , 800.0  )
        let winWidth  = settings.GetFloat ("WindowWidth"  , 800.0  )

        //let maxW = float <| Array.sumBy (fun (sc:Forms.Screen) -> sc.WorkingArea.Width)  Forms.Screen.AllScreens  // needed for dual screens ?, needs wins.forms
        //let maxH = float <| Array.sumBy (fun (sc:Forms.Screen) -> sc.WorkingArea.Height) Forms.Screen.AllScreens // https://stackoverflow.com/questions/37927011/in-wpf-how-to-shift-a-win-onto-the-screen-if-it-is-off-the-screen/37927012#37927012

        let offTolerance = 25.0 // being 20 pixel off screen is still good enough for being on screen and being drag-able

        let maxW = SystemParameters.VirtualScreenWidth   + offTolerance
        let maxH = SystemParameters.VirtualScreenHeight  + offTolerance // somehow a window docked on the right is 7 pix bigger than the screen ?? // TODO check dual screens !!

        base.Top <-     winTop
        base.Left <-    winLeft
        base.Height <-  winHeight
        base.Width <-   winWidth

        // (2) only now set the maximize flag or correct position if off the screen
        if settings.GetBool ("WindowIsMax", false) then
            //base.WindowState <- WindowState.Maximized // always puts it on first screen, do in loaded event instead
            setMaxAfterLoading <- true
            isMinOrMax  <- true

        elif  winTop  < -offTolerance || winHeight + winTop  > maxH then
            //errorLogger "Fittings.PositionedWindow:Could not restore previous Window position:"
            //errorLogger (sprintf "Fittings.PositionedWindow: winTopPosition: %.1f  + winHeight: %.1f  = %.1f that is bigger than maxH: %.1f + %.1f tolerance" winTop winHeight   ( winHeight + winTop ) SystemParameters.VirtualScreenHeight offTolerance )
            base.WindowStartupLocation <- WindowStartupLocation.CenterScreen
            base.Height <- 600.0
            base.Width  <- 600.0

        elif winLeft < -offTolerance || winWidth  + winLeft > maxW then
            //errorLogger "Fittings.PositionedWindow: Could not restore previous Window position:"
            //errorLogger <| sprintf "Fittings.PositionedWindow: winLeftPosition: %.1f  + winWidth: %.1f = %.1f that is bigger than maxW: %.1f + %.1f tolerance" winLeft winWidth ( winWidth +  winLeft) SystemParameters.VirtualScreenWidth offTolerance
            base.WindowStartupLocation <- WindowStartupLocation.CenterScreen
            base.Height <- 600.0
            base.Width  <- 600.0

        //Turns out that we cannot maximize the window until it's loaded.
        //http://mostlytech.blogspot.com/2008/01/maximizing-wpf-window-to-second-monitor.html
        this.Loaded.Add (fun _ -> if setMaxAfterLoading then this.WindowState <- WindowState.Maximized)

        this.LocationChanged.Add(fun _ -> // occurs for every pixel moved
            async{
                // normally the state change event comes after the location change event but before size changed. async sleep in LocationChanged prevents this
                do! Async.Sleep 200 // so that StateChanged event comes first
                if this.WindowState = WindowState.Normal &&  not isMinOrMax then
                    if this.Top > -500. && this.Left > -500. then // to not save on minimizing on minimized: Top=-32000 Left=-32000
                        settings.SetFloatDelayed ("WindowTop"  ,this.Top  ,100) // get float in state change Maximized needs to access this before 350 ms pass
                        settings.SetFloatDelayed ("WindowLeft" ,this.Left ,100)
                        settings.SaveWithDelay ()
                }
                |> Async.StartImmediate
            )

        this.StateChanged.Add (fun _ ->
            match this.WindowState with
            | WindowState.Normal ->
                // because when Window is hosted in other App the restore from Maximized does not remember the previous position automatically
                this.Top <-     settings.GetFloat ("WindowTop"    , 100.0 )
                this.Left <-    settings.GetFloat ("WindowLeft"   , 100.0 )
                this.Height <-  settings.GetFloat ("WindowHeight" , 800.0 )
                this.Width <-   settings.GetFloat ("WindowWidth"  , 800.0 )
                settings.SetBool  ("WindowIsMax", false)  |> ignore
                isMinOrMax <- false
                settings.SaveWithDelay ()

            | WindowState.Maximized ->
                // normally the state change event comes after the location change event but before size changed. async sleep in LocationChanged prevents this
                isMinOrMax  <- true
                settings.SetBool ("WindowIsMax", true) |> ignore
                settings.SaveWithDelay  ()

            |WindowState.Minimized ->
                isMinOrMax  <- true

            | _ ->
                // eprintfn "Fittings.PositionedWindow: unknown WindowState State change=%A" wch
                // isMinOrMax  <- true
                () // never happens
            )

        this.SizeChanged.Add (fun _ -> // does no get trigger on maximizing
            if this.WindowState = WindowState.Normal &&  not isMinOrMax  then
                settings.SetFloatDelayed ("WindowHeight", this.Height, 100 )
                settings.SetFloatDelayed ("WindowWidth" , this.Width , 100 )
                settings.SaveWithDelay ()
            )

    /// Create from application name only
    /// Settings will be saved in LocalApplicationData folder
    /// In a subfolder called 'applicationName'.
    /// The file itself will be called 'Fittings.PositionedWindow.Settings.txt'.
    /// The ErrorLogger function will be called if the previous Window position could not restore.
    /// The window be positioned in the screen center with a size of 600 x 600.
    new (applicationName:string, errorLogger:string->unit) =
        let appName =
           let mutable n = applicationName
           for c in IO.Path.GetInvalidFileNameChars() do  n <- n.Replace(c, '_')
           n
        let appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        let p = IO.Path.Combine(appData,appName)
        IO.Directory.CreateDirectory(p) |> ignore
        let f = IO.Path.Combine(p,"Fittings.PositionedWindow.Settings.txt")
        PositionedWindow(IO.FileInfo(f), errorLogger)


    /// Indicating if the Window is in Full-screen mode or minimized mode (not normal mode)
    member this.IsMinOrMax = isMinOrMax

    /// Get or Set the native Window Handle that owns this window.
    /// Use if this Window is hosted in another native app  (via IntPtr).
    /// So that this window opens and closes at the same time as the main host window.
    member this.OwnerHandle
        with get () = owner
        and set ptr =
            if ptr <> IntPtr.Zero then
                owner <- ptr
                Interop.WindowInteropHelper(this).Owner <- ptr

    member this.Settings = settings



