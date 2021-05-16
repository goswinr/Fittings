namespace FsEx.Wpf

open System
open System.Threading
open System.Windows.Threading


/// Threading Utils to setup and access the SynchronizationContext 
/// and evaluate any function on UI thread (Sync.doSync(f))
type SyncWpf private () =    
    
    // This static class could be a module too but then the .context member could not do a check for initilisation on every access.
    // It would be done when the module is loaded, that might be too early.

    static let mutable errorFileWrittenOnce = false // to not create more than one error file on Desktop per app run

    static let mutable ctx : SynchronizationContext = null  // will be set on first access

    /// To ensure SynchronizationContext is set up.
    /// optionally writes a log file to the desktop if it fails, since these errors can be really hard to debug    
    static let installSynchronizationContext (logErrosOnDesktop) =         
        if SynchronizationContext.Current = null then 
            // https://stackoverflow.com/questions/10448987/dispatcher-currentdispatcher-vs-application-current-dispatcher
            DispatcherSynchronizationContext(Windows.Application.Current.Dispatcher) |> SynchronizationContext.SetSynchronizationContext
        ctx <- SynchronizationContext.Current
            
        if isNull ctx && logErrosOnDesktop && not errorFileWrittenOnce then 
            // reporting this to the UI instead would not work since there is no sync context for the UI
            let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff") // to ensure unique file names  
            let filename = sprintf "SynchronizationContext setup failed-%s.txt" time
            let desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            let file = IO.Path.Combine(desktop,filename)
            try IO.File.WriteAllText(file, "Failed to get DispatcherSynchronizationContext") with _ -> () // file might be open or locked 
            errorFileWrittenOnce <- true
            failwith ("SynchronizationContext setup failed See: " + file)
    
   
    /// The UI SynchronizationContext to switch to inside async workflows
    /// Accessing this member from any thread will set up the sync context first if it is not there yet.
    /// If installSynchronizationContext fails an error file is written on the desktop since debugging this kind of errors can be hard
    static member context =
        if isNull ctx then installSynchronizationContext(true)
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

type Synchroniser() =
    
    
    let mutable busy =  false  
    
    let mutable pending = None 
    
    let counter = ref 0L
    
    /// This ensures that only one of the  supplied function runs at the same times
    /// Calls get discarded if another one is running. Except for most recent call:
    /// It also ensures that the last call is not ommited, when no other one is pending, even if it was busy while receiving it.
    /// For example this is usfull for redrawing a UI or graphic while a slider sends many value changed events.
    /// Does not do a switch to UI thread (except for the last call)
    member _.doOnlyOne(updateUi: unit-> unit) =
        if busy then  
            pending <- Some updateUi 
        else
            busy <- true
            pending <- None // first set to none
            updateUi() // during this call pending might get set to some oder call
            busy <- false
            //done updateUi, now check for last and most recent updateUi call that might have been set to pending var in the meantime
            match pending with 
            |None -> () 
            |Some f -> 
                let k = !counter + 1L
                counter := k
                // this might get called very often, would it be cheaper to use a task instead ?
                async{                     
                    do! Async.Sleep 50
                    if k = !counter then  // only do if last call for 50 ms                        
                        match pending with // check again just in case
                        |None -> () 
                        |Some f -> f() 
                } |> Async.StartImmediate
    
    member _.SetReady() = 
        busy <- false

    /// This ensures that only one of the  supplied function runs at the same times
    /// IMPORTANT: Call Synchroniser.SetReady() at the end (of any nested async workflows), to enable next call.
    /// Calls get discarded if another one is running. Except for most recent call:
    /// It also ensures that the last call is not ommited, when no other one is pending, even if it was busy while receiving it.
    /// For example this is usfull for redrawing a UI or graphic while a slider sends many value changed events.
    /// Does not do a switch to UI thread (except for the last call)
    member _.doOnlyOneManual(updateUi: unit->unit) =
        if busy then  
            pending <- Some updateUi 
        else
            busy <- true
            pending <- None // first set to none
            updateUi() // during this call pending might get set to some oder call
            // busy <- false // this has to be done explicitly via this.SetReady() inside of updateUi function
            
            //done updateUi, now check for last and most recent updateUi call that might have been set to pending var in the meantime
            match pending with 
            |None -> () 
            |Some f -> 
                let k = !counter + 1L
                counter := k
                // this might get called very often, would it be cheaper to use a task instead ?
                async{                     
                    do! Async.Sleep 50
                    if k = !counter then  // only do if last call for 50 ms                        
                        match pending with // check again just in case
                        |None -> () 
                        |Some f -> f() 
                } |> Async.StartImmediate