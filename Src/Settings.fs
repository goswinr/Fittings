namespace FsEx.Wpf


open System
open System.Text

 
/// A class to save window size, layout and position,  and more Settings
/// This class is usefull wenn in a hosted contex app.config does not work
/// values in txt file wil be sperated by  '=' 
/// comments are not allowed
type Settings (appName:string) = 
    
    let  sep    = '=' // key value separator    
    
    let filePath = 
        let appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        let p = IO.Path.Combine(appData,appName)
        IO.Directory.CreateDirectory(p) |> ignore 
        let f = IO.Path.Combine(p,"Settings.txt")
        f
        
    let writer = SaveReadWriter(filePath)

    let settingsDict = 
        let dict = new Collections.Concurrent.ConcurrentDictionary<string,string>()   
        try            
            for ln in writer.ReadAllLines() do
                match ln.Split(sep) with
                | [|k;v|] -> dict.[k] <- v // TODO allow for comments? tricky because comments need to be saved back too
                | _       -> eprintfn "Bad line in settings file file: '%s'" ln
        with 
            | :? IO.FileNotFoundException ->  () // eprintfn   "Settings file not found. (This is expected on first use of the App.)"
            | e ->                            eprintfn  "Problem reading or initalizing settings file: %A"  e
        dict    

    
    let settingsAsString () = 
        let sb = StringBuilder()
        for KeyValue(k,v) in settingsDict do
            sb.Append(k).Append(sep).AppendLine(v) |> ignore
        sb.ToString() 
    
    let get k = 
         match settingsDict.TryGetValue k with 
         |true, v  ->  Some v
         |false, _ ->  None
    

    let getFloat  key def = match get key with Some v -> float v           | None -> def
    let getInt    key def = match get key with Some v -> int v             | None -> def
    let getBool   key def = match get key with Some v -> Boolean.Parse v   | None -> def    
    
    /// Save setting with a delay
    /// delayed because the onMaximise of window event triggers first Loaction changed and then state changed, 
    /// state change event should still be able to Get previous size and loaction that is not saved yet
    /// call Save() afterwards
    member this.SetDelayed k v (delay:int)=         
        async{  do! Async.Sleep(delay) 
                settingsDict.[k] <- v         
                } |> Async.Start
    
    /// Add string value to Setting dict
    /// String must be a single line 
    /// leading an trailing whitespace will be trimmed both on key and value upon retrival, not upon writing
    /// values will be separated by '='
    /// comments care not allowed.
    /// call this.Save() afterwards to write to file in appdata folder
    member this.Set (k:string) (v:string) = 
        if k.IndexOf(sep) > -1 then eprintf  "Settings key shall not contain '%c' : %s%c%s"  sep  k  sep  v            
        if v.IndexOf(sep) > -1 then eprintf  "Settings value shall not contain '%c' : %s%c%s"  sep  k  sep  v 
        settingsDict.[k] <- v             
        
    /// get String value from settings
    member this.Get k = get k        

    /// Write to Settings.txt file in appdata folder
    member this.Save () =                       
        writer.WriteIfLast (settingsAsString,  500)
        
    member this.SetFloat        key (v:float)       = this.Set key (string v)

    member this.SetFloatDelayed key (v:float) delay = this.SetDelayed key (string v) delay

    member this.SetInt          key (v:int)         = this.Set key (string v)

    member this.SetBool         key (v:bool)        = this.Set key (string v)

    member this.GetFloat        key def = getFloat key def

    member this.GetInt          key def = getInt   key def

    member this.GetBool         key def = getBool  key def
    