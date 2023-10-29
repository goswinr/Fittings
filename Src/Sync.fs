namespace Fittings

open System
open System.Threading
open System.Windows.Threading


/// Threading Utils to setup and access the SynchronizationContext
/// and evaluate any function on UI thread (Sync.doSync(f))
type SyncWpf private () = 

    // This static class could be a module too but then the .context member could not do a check for initialization on every access.
    // It would be done when the module is loaded, that might be too early.

    static let mutable errorFileWrittenOnce = false // to not create more than one error file on Desktop per app run

    static let mutable ctx : SynchronizationContext = null  // will be set on first access

    /// To ensure SynchronizationContext is set up.
    /// Optionally writes a log file to the desktop if it fails, since these errors can be really hard to debug
    static member installSynchronizationContext (logErrorsOnDesktop) = 
        if SynchronizationContext.Current = null then
            // https://stackoverflow.com/questions/10448987/dispatcher-currentdispatcher-vs-application-current-dispatcher
            DispatcherSynchronizationContext(Windows.Application.Current.Dispatcher) |> SynchronizationContext.SetSynchronizationContext
        ctx <- SynchronizationContext.Current

        if isNull ctx && logErrorsOnDesktop && not errorFileWrittenOnce then
            // reporting this to the UI instead would not work since there is no sync context for the UI
            let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") // to ensure unique file names
            let filename = sprintf "Fittings.SynchronizationContext setup failed-%s.txt" time
            let desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            let file = IO.Path.Combine(desktop,filename)
            try IO.File.WriteAllText(file, "Failed to get DispatcherSynchronizationContext") with _ -> () // file might be open or locked
            errorFileWrittenOnce <- true
            failwith ("SynchronizationContext setup failed See: " + file)


    /// The UI SynchronizationContext to switch to inside async workflows
    /// Accessing this member from any thread will set up the sync context first if it is not there yet.
    /// If installSynchronizationContext fails an error file is written on the desktop since debugging this kind of errors can be hard
    static member context = 
        if isNull ctx then SyncWpf.installSynchronizationContext(true)
        ctx

    /// Runs function on UI thread
    /// This Ui thread will also be a STAThread
    /// If installSynchronizationContext fails an error file is written on the desktop since debugging this kind of errors can be hard
    static member doSync(func) = 
        async {
            do! Async.SwitchToContext SyncWpf.context
            func()
            } |> Async.StartImmediate

        // see also:
        // https://github.com/fsprojects/FsXaml/blob/c0979473eddf424f7df83e1b9222a8ca9707c45a/src/FsXaml.Wpf/Utilities.fs#L132
        // https://stackoverflow.com/questions/61227071/f-async-switchtocontext-vs-dispatcher-invoke

