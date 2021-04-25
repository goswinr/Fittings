namespace FsEx.Wpf


open System
open System.Threading


module internal Util = 
    
    let maxCharsInString = 300

    /// If the input string is longer than maxChars + 20 then 
    /// it returns the input string trimmed to maxChars, a count of skiped characters and the last 6 characters (all enclosed in double quotes ")
    /// e.g. "abcde[..20 more Chars..]xyz"
    /// Else, if the input string is less than maxChars + 20, it is still returned in full (enclosed in double quotes ").
    /// also see String.truncatedFormated
    let truncateString (stringToTrim:string) =
        if stringToTrim.Length <= maxCharsInString + 20 then sprintf "\"%s\""stringToTrim
        else 
            let len   = stringToTrim.Length
            let st    = stringToTrim.Substring(0, maxCharsInString) 
            let last6 = stringToTrim.Substring(len-7) 
            sprintf "\"%s[..%d more Chars..]%s\"" st (len - maxCharsInString - 6) last6


/// Writes Async and with Lock, 
/// Optionally only once after a delay in which it might be called several times
type SaveWriter () = 
    // this class also exist in FsEx.IO
    
    let counter = ref 0L // for atomic writing back to file
    
    let lockObj = new Object()

    /// File will be written async and with a Lock.
    /// If it fails an Error is printed to the Error stream via eprintfn
    member this.Write (path, text) =        
        async{
            lock lockObj (fun () -> // lock is using Monitor class : https://github.com/dotnet/fsharp/blob/6d91b3759affe3320e48f12becbbbca493574b22/src/fsharp/FSharp.Core/prim-types.fs#L4793
                try  IO.File.WriteAllText(path,text)
                with ex ->  eprintfn "SaveWriter.Write failed with: %A \r\n while writing:\r\n%s" ex (Util.truncateString text) // use %A to trimm long text        
                )       
            } |> Async.Start

      
    /// GetString will be called in sync on calling thread, but file will be written async.
    /// Only if after the delay the counter value is the same as before. 
    /// That means no more recent calls to this function have been made during the delay.
    /// If other calls to this function have been made then only the last call will be written as file
    /// If it fails an Error is printed to the Error stream via eprintfn
    member this.WriteIfLast (path, getText: unit->string, delayMillisSeconds:int) =
        async{
            let k = Interlocked.Increment counter
            do! Async.Sleep(delayMillisSeconds) // delay to see if this is the last of many events (otherwise there is a noticable lag in dragging window around, for example, when saving window position)
            if !counter = k then //k > 2L &&   //do not save on startup && only save last event after a delay if there are many save events in a row ( eg from window size change)(ignore first two event from creating window)
                try 
                    let text = getText()               
                    this.Write (path, text) // this should never fail since exeptiona are caught inside 
                with ex -> 
                    eprintfn "SaveWriter.WriteIfLast: getText() for path (%s) failed with: %A" path ex                 
            } |> Async.StartImmediate
