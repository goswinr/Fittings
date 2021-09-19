namespace FsEx.Wpf

open System
open System.Windows
open System.Threading


module WindowOnNewThread = 

    /// Create a new thread, launch a WPF window on it, wait for the window to initialize and then
    /// return a reference to it, it's SynchronizationContext and it's isAlive state.
    /// You can then update the window via async workflows which start with 'do! Async.SwitchToContext context'
    let show(title) = 
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
        thread.IsBackground <- true
        thread.Start()
        resetEvent.Wait()
        resetEvent.Dispose()
        !w, !ctx, isAlive
