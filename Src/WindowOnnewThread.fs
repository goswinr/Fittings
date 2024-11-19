namespace Fittings

open System
open System.Windows
open System.Threading
open System.Windows.Threading


/// A module to creates a WPF window on a new STA thread.
/// Returns the window and the associated SynchronizationContext
/// call BeginInvokeShutdown on thread when window is closed
[<RequireQualifiedAccess>]
module WindowOnNewThread =

    /// Creates a WPF window on a new STA thread.
    /// Returns the window and the associated SynchronizationContext
    /// call BeginInvokeShutdown on thread when window is closed
    let show (createWindow:unit->Window) : Window*SynchronizationContext =
        //adapted from  http://reedcopsey.com/2011/11/28/launching-a-wpf-window-in-a-separate-thread-part-1/

        let mutable asyncContext: SynchronizationContext = null
        let mutable win: Window = null

        let th = new Thread(new ThreadStart( fun () ->
                let ctx = new DispatcherSynchronizationContext( Dispatcher.CurrentDispatcher)
                asyncContext <- ctx:>SynchronizationContext
                SynchronizationContext.SetSynchronizationContext( new DispatcherSynchronizationContext( Dispatcher.CurrentDispatcher))
                win <- createWindow()
                win.Closed.Add ( fun _ ->  Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background))
                win.Show()
                // Start the Dispatcher Processing
                System.Windows.Threading.Dispatcher.Run()
                ))
        th.SetApartmentState(ApartmentState.STA)
        th.IsBackground <- true
        th.Start()
        win, asyncContext

    (*
    /// Create a new thread, launch a WPF window on it, wait for the window to initialize and then
    /// return a reference to it, it's SynchronizationContext and it's isAlive state.
    /// You can then update the window via async workflows which start with 'do! Async.SwitchToContext context'
    let showOLD(title) =
        // from http://www.fssnip.net/hL/0
        let w = ref null
        let ctx = ref null
        let resetEvent = new ManualResetEventSlim()
        let isAlive = ref true
        let launcher() =
            w := new Window()
            (!w).Loaded.Add(fun _ ->
                ctx := SynchronizationContext.Current
                resetEvent.Set())
            (!w).Title <- title
            (!w).ShowDialog() |> ignore
            isAlive := false
        let thread = new Thread(launcher)
        thread.SetApartmentState(ApartmentState.STA)
        thread.IsBackground <- true // without this the thread would never stop even if all windows are closed https://stackoverflow.com/questions/1111369/how-do-i-create-and-show-wpf-windows-on-separate-threads
        thread.Start()
        resetEvent.Wait()
        resetEvent.Dispose()
        !w, !ctx, isAlive
    *)