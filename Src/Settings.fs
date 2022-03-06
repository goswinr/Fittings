namespace FsEx.Wpf

open System
open System.Globalization
open System.Text

/// A class to save window size, layout and position, and more Settings.
/// This class is useful when in a hosted context app.config does not work.
/// Keys may not contain the separator character, Values and keys may not contain a new line character
/// Comments are not allowed
/// Any errors are saved to this.Errors list.
type Settings (settingsFile:IO.FileInfo, separator:char, errorLogger:string->unit) = 

    let  sep  = separator // key value separator

    let writer = 
        if not settingsFile.Directory.Exists then
            IO.Directory.CreateDirectory(settingsFile.DirectoryName)  |> ignore
        SaveReadWriter(settingsFile.FullName, errorLogger)

    let settingsDict = 
        let dict = new Collections.Concurrent.ConcurrentDictionary<string,string>()
        if writer.CreateFileIfMissing("") then //errors get  logged //for case when Settings file is not found. (This is expected on first use of the App.)"
            match writer.ReadAllLines() with
            |None -> () //errors get logged
            |Some lns ->
                for ln in lns do
                    match ln.IndexOf(sep) with
                    | -1 -> errorLogger (sprintf "Bad line in settings file: '%s'" ln)
                    | i ->
                        let k = ln.Substring(0,i)
                        let v = ln.Substring(i+1)// + 1 to skip sep
                        dict.[k] <- v // TODO allow for comments? tricky because comments need to be saved back too
        dict


    let settingsAsString () = 
        let sb = StringBuilder()
        for KeyValue(k,v) in settingsDict |> Seq.sortBy (fun (KeyValue(k,v)) -> k) do // sorted for better debugging
            sb.Append(k).Append(sep).AppendLine(v) |> ignore
        sb.ToString()

    let get k = 
         match settingsDict.TryGetValue k with
         |true, v  ->  Some v
         |false, _ ->  None

    let pfloat(s:string) def = match Double.TryParse(s)  with (true, v) -> v |(false,_) -> def
    let pint  (s:string) def = match Int32.TryParse(s)   with (true, v) -> v |(false,_) -> def
    let pbool (s:string) def = match Boolean.TryParse(s) with (true, v) -> v |(false,_) -> def

    let getFloat  key def = match get key with Some v -> pfloat v def  | None -> def
    let getInt    key def = match get key with Some v -> pint v def    | None -> def
    let getBool   key def = match get key with Some v -> pbool v def   | None -> def


    /// A class to save window size, layout and position,  and more Settings
    /// This class is useful when in a hosted context app.config does not work
    /// Values in txt file will be separated by  '=' .
    /// Comments are not allowed
    new (settingsFile:IO.FileInfo, errorLogger:string->unit) = 
        Settings (settingsFile, '=', errorLogger)


    /// Save setting with a delay.
    /// Delayed because the onMaximise of window event triggers first location changed and then state changed,
    /// State change event should still be able to Get previous size and location that is not saved yet
    /// call Save() afterwards
    member this.SetDelayed (k, v , delay:int)= 
        async{  do! Async.Sleep(delay)
                settingsDict.[k] <- v
        } |> Async.Start

    /// Add string value to the Concurrent settings Dictionary.
    /// Keys may not contain the separator character, Values and keys may not contain a new line character.
    /// If they do it will be replaced by empty string. And a message will be written to errorLogger
    /// Comments care not allowed.
    /// Call this.Save() afterwards to write to file async with a bit of delay.
    member this.Set (key:string, value:string) :unit = 
        let mutable k = key
        let mutable v = value
        //let mutable ok = true
        if k.IndexOf(sep)  > -1 then k<-k.Replace(string sep,"") (*; ok <- false *); errorLogger(sprintf "separator in key:%s" key)
        if k.IndexOf('\r') > -1 then k<-k.Replace("\r"      ,"") (*; ok <- false *); errorLogger(sprintf "newline in key:%s" key)
        if k.IndexOf('\n') > -1 then k<-k.Replace("\n"      ,"") (*; ok <- false *); errorLogger(sprintf "newline in key:%s" key)
        if v.IndexOf('\r') > -1 then v<-v.Replace("\r"      ,"") (*; ok <- false *); errorLogger(sprintf "newline in value:%s" value)
        if v.IndexOf('\n') > -1 then v<-v.Replace("\n"      ,"") (*; ok <- false *); errorLogger(sprintf "newline in value:%s" value)
        settingsDict.[k] <- v
        //ok

    /// Get String value from settings
    member this.Get k = get k

    /// Write to Settings to  file in specified in constructor
    /// Writes after a delay of 250 ms and only if there was no more recent call to Save.
    member this.Save () = 
        writer.WriteIfLast (settingsAsString,  250)

    /// Using maximum digits of precision
    member this.SetFloatHighPrec (key, v:float)      = this.Set (key ,v.ToString("0.#",CultureInfo.InvariantCulture)) // InvariantCulture to not mess up , and .

    /// Using just one digit after zero for precision
    member this.SetFloat         (key,v:float)       = this.Set (key,v.ToString("0.#",CultureInfo.InvariantCulture)) // InvariantCulture to not mess up , and .

    /// Save float to dict after a delay.
    /// Using just one digit after zero for precision
    /// A delay is useful e.g. because the onMaximise of window event triggers first Location changed and then state changed,
    /// State change event should still be able to Get previous size and location that is not saved yet
    member this.SetFloatDelayed (key ,v:float, delay) = this.SetDelayed (key ,v.ToString("0.#",CultureInfo.InvariantCulture), delay) // InvariantCulture to not mess up , and .

    member this.SetInt          (key ,v:int)         = this.Set (key ,string v)

    member this.SetBool         (key ,v:bool)        = this.Set (key ,string v)

    member this.GetFloat        (key, def)  = getFloat key def

    member this.GetInt          (key, def)  = getInt   key def

    member this.GetBool         (key, def)  = getBool  key def



