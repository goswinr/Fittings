namespace Fittings

open System
open System.Windows
open System.Runtime.InteropServices
open System.ComponentModel

#nowarn "44" //This construct is deprecated. Recovery from corrupted process state exceptions is not supported; HandleProcessCorruptedStateExceptionsAttribute is ignored.


/// A class to provide an Error Handler that can catch corrupted state or access violation errors from FSI threads too.
type ProcessCorruptedState(applicationName:string, appendText:unit->string) =

    let desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)

    let appName =
        let mutable n = applicationName
        for c in IO.Path.GetInvalidFileNameChars() do  n <- n.Replace(c, '_')
        n

    [< Security.SecurityCritical >]//to handle AccessViolationException too
    [< Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions >] //https://stackoverflow.com/questions/3469368/how-to-handle-accessviolationexception/4759831
    //NET 6.0: This construct is deprecated. Recovery from corrupted process state exceptions is not supported; HandleProcessCorruptedStateExceptionsAttribute is ignored.
    member this.Handler (_sender:obj) (e: UnhandledExceptionEventArgs) :unit=
            // Starting with the .NET Framework 4, this event is not raised for exceptions that corrupt the state of the process,
            // such as stack overflows or access violations, unless the event handler is security-critical and has the HandleProcessCorruptedStateExceptionsAttribute attribute.
            // https://docs.microsoft.com/en-us/dotnet/api/system.appdomain.unhandledexception?redirectedfrom=MSDN&view=netframework-4.8
            // https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/clr-inside-out-handling-corrupted-state-exceptions
            // https://stackoverflow.com/questions/39956163/gracefully-handling-corrupted-state-exceptions

            let time = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff")// to ensure unique file names
            let filename = sprintf "%s-Fittings-UnhandledException-%s.txt" appName time
            let file = IO.Path.Combine(desktop,filename)
            let win32Err = ProcessCorruptedState.getWin32Errors()
            let err = sprintf "%s:ProcessCorruptedState Special Handler: AppDomain.CurrentDomain.UnhandledException: \r\n isTerminating: %b : \r\n time: %s \r\n\r\n%A \r\n\r\n%s \r\n\r\n%s" applicationName e.IsTerminating time e.ExceptionObject (appendText()) win32Err

            try IO.File.WriteAllText(file, err) with _ -> () // file might be open and locked
            eprintfn "%s" err

    static member getWin32Errors() =
        let lastError = Marshal.GetLastWin32Error() // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/18d8fbe8-a967-4f1c-ae50-99ca8e491d2d
        if lastError <> 0 then
            "WIN32 LAST ERROR:\r\n-no win32 Errors-"
        else
            let innerEx = new Win32Exception(lastError) //Win32 error codes are translated from their numeric representations into a system message
            sprintf "WIN32 LAST ERROR:\r\nErrorCode %d: %s-" lastError innerEx.Message



/// To set up global AppDomain.CurrentDomain.UnhandledException.Handler.
/// A class to provide an Error Handler that can catch corrupted state
/// or access violation errors from FSI threads too.
/// (applicationName) for name to be displayed.
/// (appendText:unit->string) to get additional text to add to the error message.
type ErrorHandling(applicationName:string, appendText:unit->string)  =

    let maxThrowCount = 20

    let mutable throwCount = 0

    /// set up global AppDomain.CurrentDomain.UnhandledException.Handler
    /// (applicationName) for name to be displayed
    /// (appendText:unit->string) to get additional text to add to the error message
    /// Exception get printed to the text writer at Console.SetError
    /// UnhandledException that cant be caught create a log file on the desktop
    member this.Setup() : unit=
        throwCount <- 0 // reset
        if not <| isNull Application.Current then // null if application is not yet created, or no application in hosted context
            Application.Current.DispatcherUnhandledException.Add(fun e ->
                let mutable print = true
                if print then
                    if throwCount < maxThrowCount then // reduce printing to Log UI, it might crash from printing too much
                        throwCount <- throwCount + 1
                        if e <> null then
                            eprintfn "%s:Application.Current.DispatcherUnhandledException in main Thread:\r\n%A" applicationName e.Exception
                            eprintfn "%s" (ProcessCorruptedState.getWin32Errors())
                            e.Handled<- true
                        else
                            eprintfn "%s:Application.Current.DispatcherUnhandledException in main Thread: *null* Exception Object" applicationName
                            eprintfn "%s" (ProcessCorruptedState.getWin32Errors())
                    else
                        print <- false
                        eprintfn "\r\nMORE THAN %d Application.Current.DispatcherUnhandledExceptions"    maxThrowCount
                        eprintfn "\r\n\r\n   *** LOGGING STOPPED. CLEAR LOG FIRST TO START PRINTING AGAIN *** "
                         )

        //catching un-handled exceptions generated from all threads running under the context of a specific application domain.
        //https://dzone.com/articles/order-chaos-handling-unhandled
        //https://stackoverflow.com/questions/14711633/my-c-sharp-application-is-returning-0xe0434352-to-windows-task-scheduler-but-it

        AppDomain.CurrentDomain.UnhandledException.AddHandler( new UnhandledExceptionEventHandler( ProcessCorruptedState(applicationName,appendText).Handler))


    // set up global AppDomain.CurrentDomain.UnhandledException.Handler
    // (applicationName) for name to be displayed
    // Exception get printed to the text writer at Console.SetError
    // UnhandledException that cant be caught create a log file on the desktop
    //member this.setupSimple(applicationName) =    setup(applicationName,fun () -> "")
