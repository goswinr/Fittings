namespace FsEx.Wpf

open System
open System.Threading


module internal Help = 

    let maxCharsInString = 500

    /// If the input string is longer than maxChars + 20 then 
    /// it returns the input string trimmed to maxChars, a count of skiped characters and the last 6 characters (all enclosed in double quotes ")
    /// e.g. "abcde[..20 more Chars..]xyz"
    /// Else, if the input string is less than maxChars + 20, it is still returned in full (enclosed in double quotes ").
    /// also see String.truncatedFormated
    let truncateString (stringToTrim:string) =
        if isNull stringToTrim then "-null string-" // add too, just in case this gets called externally        
        elif stringToTrim.Length <= maxCharsInString + 20 then sprintf "\"%s\""stringToTrim
        else 
            let len   = stringToTrim.Length
            let st    = stringToTrim.Substring(0, maxCharsInString) 
            let last20 = stringToTrim.Substring(len-21) 
            sprintf "\"%s[<< ... %d more chars ... >>]%s\"" st (len - maxCharsInString - 20) last20


/// Reads and Writes with Lock, 
/// Optionally only once after a delay in which it might be called several times
/// using Text.Encoding.UTF8
/// Writes Exceptions to errorLogger 
type SaveReadWriter (path:string, errorLogger:string->unit)= 
    // same class also exist in FsEx , TODO keep in sync!
   
    let counter = ref 0L // for atomic writing back to file
   
    let lockObj = new Object()
    
    /// Calls IO.File.Exists(path)
    member this.FileExists = IO.File.Exists(path)

    member this.FileDoesNotExists = not <| IO.File.Exists(path)

    /// The full file path
    member this.Path : string  = path 

    /// Creates file with text , only if it does not exist yet.
    /// Writes Exceptions to errorLogger. 
    /// Returns true if file exists or was successfully created 
    member this.CreateFileIfMissing(text) :bool =
        if IO.File.Exists(path) then 
            true
        else
            try 
                IO.File.WriteAllText(path, text,Text.Encoding.UTF8)  
                true
            
            with e -> 
                errorLogger(sprintf "FsEx.Wpf.SaveReadWriter.CreateFileIfMissing for path '%s' :\r\n%A" path e)   
                false


    /// Thread Save reading.
    /// Ensures that no writing happens while reading. 
    /// Writes Exceptions to errorLogger 
    member this.ReadAllText () : option<string> =
        // lock is using Monitor class : https://github.com/dotnet/fsharp/blob/6d91b3759affe3320e48f12becbbbca493574b22/src/fsharp/FSharp.Core/prim-types.fs#L4793
        lock lockObj (fun () -> 
            try Some <| IO.File.ReadAllText(path, Text.Encoding.UTF8)
            with e -> 
                errorLogger(sprintf "FsEx.Wpf.SaveReadWriter.ReadAllText from path '%s' :\r\n%A" path e)  
                None  )
            

    /// Thread Save reading.
    /// Ensures that no writing happens while reading.
    /// Writes Exceptions to errorLogger 
    member this.ReadAllLines () : option<string[]> =
        // lock is using Monitor class : https://github.com/dotnet/fsharp/blob/6d91b3759affe3320e48f12becbbbca493574b22/src/fsharp/FSharp.Core/prim-types.fs#L4793
        lock lockObj (fun () -> 
            try Some <| IO.File.ReadAllLines(path, Text.Encoding.UTF8)
            with e -> 
                errorLogger(sprintf "FsEx.Wpf.SaveReadWriter.ReadAllText from '%s' :\r\n%A" path e)   
                None  )
            
    
    /// File will be written async and with a Lock.
    /// Ensures that no reading happens while writing.
    /// Writes Exceptions to errorLogger 
    member this.WriteAsync (text) =        
        async{
            lock lockObj (fun () -> // lock is using Monitor class : https://github.com/dotnet/fsharp/blob/6d91b3759affe3320e48f12becbbbca493574b22/src/fsharp/FSharp.Core/prim-types.fs#L4793
                try  IO.File.WriteAllText(path,text, Text.Encoding.UTF8)
                // try & with is needed because exceptions on threadpool cannot be caught otherwise !!
                with ex ->  errorLogger(sprintf "FsEx.Wpf.SaveWriter.WriteAsync failed with: %A \r\n while writing to %s:\r\n%A" ex path (Help.truncateString text)) // use %A to trimm long text        
                )       
            } |> Async.Start

    /// File will be written async and with a Lock.    
    /// Ensures that no reading happens while writing.
    /// Writes Exceptions to errorLogger 
    member this.WriteAllLinesAsync (texts) =        
        async{
            lock lockObj (fun () -> // lock is using Monitor class : https://github.com/dotnet/fsharp/blob/6d91b3759affe3320e48f12becbbbca493574b22/src/fsharp/FSharp.Core/prim-types.fs#L4793
                try  IO.File.WriteAllLines(path,texts, Text.Encoding.UTF8)
                // try & with is needed because exceptions on threadpool cannot be caught otherwise !!
                with ex ->  errorLogger(sprintf "FsEx.Wpf.SaveWriter.WriteAllLinesAsync failed with: %A \r\n while writing to %s:\r\n%A" ex path (Array.truncate 20 texts)) // use %A to trimm long text        
                )       
            } |> Async.Start
   
    /// GetString will be called in sync on calling thread, but file will be written async.
    /// Only if after the delay the counter value is the same as before. 
    /// That means no more recent calls to this function have been made during the delay.
    /// If other calls to this function have been made then only the last call will be written as file.    
    /// Also ensures that no reading happens while writing.
    /// Writes Exceptions to errorLogger 
    member this.WriteIfLast ( getText: unit->string, delayMillisSeconds:int) =
        async{
            let k = Interlocked.Increment counter
            do! Async.Sleep(delayMillisSeconds) // delay to see if this is the last of many events (otherwise there is a noticable lag in dragging window around, for example, when saving window position)
            if !counter = k then //k > 2L &&   //do not save on startup && only save last event after a delay if there are many save events in a row ( eg from window size change)(ignore first two event from creating window)
                try 
                    let text = getText() 
                    this.WriteAsync (text) // this should never fail since exeptions are caught inside 
                with ex -> 
                    // try & with is needed because exceptions on threadpool cannot be caught otherwise !!
                    errorLogger(sprintf "FsEx.Wpf.SaveWriter.WriteIfLast: getText() for path '%s' failed with: %A" path ex )                
            } |> Async.StartImmediate   