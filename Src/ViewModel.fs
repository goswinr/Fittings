namespace FsEx.Wpf


open System
open System.Windows
open System.Windows.Data
open System.Globalization
open System.ComponentModel


module ViewModel =  

      /// A base class for a viewmodel implementing INotifyPropertyChanged
    type ViewModelBase() = 
        // alternative: http://www.fssnip.net/4Q/title/F-Quotations-with-INotifyPropertyChanged
        let ev = new Event<_, _>()
    
        interface INotifyPropertyChanged with
            [<CLIEvent>]
            member x.PropertyChanged = ev.Publish

        /// use nameof operator on members to provide the string reqired 
        /// member x.Val
        ///    with get()  = val
        ///    and set(v)  = val <- v; x.OnPropertyChanged(nameof x.Val)
        member x.OnPropertyChanged(propertyName : string) = 
            ev.Trigger(x, new PropertyChangedEventArgs(propertyName))  
    
    module Literals = 
        
        /// string for RhinoMath.UnsetValue -1.23432101234321e+308
        [<Literal>]
        let RhinoMathUnset = "RhinoMath.Unset" // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
        
        [<Literal>]
        let PositiveInfinity = "∞"
        
        [<Literal>]
        let NegativeInfinity = "-∞"
        
        [<Literal>]
        let NaN = "NaN"

        [<Literal>]
        let AlmostZero = "~0.0"

        [<Literal>]
        let AlmostZeroNeg = "-~0.0"


    module NumberFormating = 
        
        // implementations copied from FsEx.MathUtil and FsEx.NiceString

        /// CultureInfo.InvariantCulture
        let invC = Globalization.CultureInfo.InvariantCulture
        
        /// American Englisch culture (used for float parsing)
        let enUs = CultureInfo.GetCultureInfo("en-us")
        
        // German culture (used for float parsing)
        //let deAt = CultureInfo.GetCultureInfo("de-at")

        /// set this to change the printing of floats larger than 10'000
        let mutable thousandSeparator = '\'' // = just one quote '

        

        /// Assumes a string that represent a float or int with '.' as decimal serapator and no other input formating
        let addThousandSeparators (s:string) =
            let b = Text.StringBuilder(s.Length + s.Length / 3 + 1)
            let inline add (c:char) = b.Append(c) |> ignore
        
            let inline doBeforeComma st en =         
                for i=st to en-1 do // dont go to last one becaus it shall never get a separator 
                    let rest = en-i            
                    add s.[i]
                    if rest % 3 = 0 then add thousandSeparator
                add s.[en] //add last (never with sep)

            let inline doAfterComma st en = 
                add s.[st] //add fist (never with sep)        
                for i=st+1 to en do // dont go to last one becaus it shal never get a separator                       
                    let pos = i-st
                    if pos % 3 = 0 then add thousandSeparator            
                    add s.[i]
            
            
            let start = 
                if s.[0] = '-' then  add '-'; 1 /// add minus if present and move start location
                else                          0 

            match s.IndexOf('.') with 
            | -1 -> doBeforeComma start (s.Length-1)
            | i -> 
                if i>start then doBeforeComma start (i-1)
                add '.'
                if i < s.Length then doAfterComma (i+1) (s.Length-1)

            b.ToString() 

        let tryParseNiceFloat (s:string)=
            match s with 
            |Literals.NaN -> None
            |Literals.PositiveInfinity -> None
            |Literals.NegativeInfinity-> None
            |Literals.RhinoMathUnset -> Some 0.0 // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
            |Literals.AlmostZero ->  Some 0.0
            |Literals.AlmostZeroNeg->  Some 0.0
            | _ -> 
                let cleanFloat = s.Replace(string(thousandSeparator),"") // no need to take care of decimal comma here. nice string never has one
                match Double.TryParse(cleanFloat, NumberStyles.Float, invC) with 
                | true, v -> Some v
                | _ -> None
        
        let int (x:int) = 
            if abs(x) > 1000 then x.ToString() |> addThousandSeparators
            else                  x.ToString()  

        /// Formating with automatic precision 
        /// e.g.: 0 digits behind comma if above 1000 
        /// if there are more than 15 zeros behind the comma just '~0.0' will be displayed
        /// if the value is smaller than NiceStringSettings.roundToZeroBelow '0.0' will be shown.
        /// this is Double.Epsilon by default
        let float  (x:float) =
            if   Double.IsNaN x then Literals.NaN
            elif x = Double.NegativeInfinity then Literals.NegativeInfinity
            elif x = Double.PositiveInfinity then Literals.PositiveInfinity
            elif x = -1.23432101234321e+308 then Literals.RhinoMathUnset // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
            elif x = 0.0 then "0" // not "0" as in sprintf "%g"
            else
                let  a = abs x                
                if   a >= 10000.            then x.ToString("#"        ) |> addThousandSeparators 
                elif a >= 1000.             then x.ToString("#.#", invC) |> addThousandSeparators 
                elif a >= 100.              then x.ToString("#.##" , invC)
                elif a >= 10.               then x.ToString("#.###" , invC)
                elif a >= 1.                then x.ToString("#.####" , invC) //|> addThousandSeparators              
                elif a >= 0.1               then x.ToString("0.#####" , invC) |> addThousandSeparators 
                elif a >= 0.01              then x.ToString("0.######" , invC) |> addThousandSeparators 
                elif a >= 0.001             then x.ToString("0.#######" , invC) |> addThousandSeparators 
                elif a >= 0.0001            then x.ToString("0.########" , invC) |> addThousandSeparators 
                elif a >= 0.00001           then x.ToString("0.#########" , invC) |> addThousandSeparators 
                elif a >= 0.000001          then x.ToString("0.##########" , invC) |> addThousandSeparators 
                elif a >= 0.0000001         then x.ToString("0.###########" , invC) |> addThousandSeparators 
                elif a >= 0.000000000000001 then x.ToString("0.###############" , invC) |> addThousandSeparators // 15 decimal paces for doubles
                elif x >= 0.0 then Literals.AlmostZero
                else Literals.AlmostZeroNeg
     

    /// A binding that uses variable custom float formating  for diplaying floats
    /// includes thousand separators in Binding converter
    /// uses UpdateSourceTrigger.Explicit so update thsese bindings explicitly
    type FormatedFloatBinding (viewMmodel:INotifyPropertyChanged, propertyName : string, snapToInt:bool) = 
        inherit Binding() 
    
        do  
            // this and other case ar handele explicitly in Textbox.TextChanged. event
            //try
            //    // So that wpf textboxes that are bound to floats can have a dot input too. see https://stackoverflow.com/a/35942615/969070
            //    // setting this might fails when a hosting WPF process is alread up and running (eg loaded in another WPF thread ,for example in Seff UI therad)  
            //    FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty <- false
            //with  _ -> ()
            //    //if FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty then 
            //    //    eprintfn "could not set KeepTextBoxDisplaySynchronizedWithTextProperty to false "
                
            base.Source <- viewMmodel
            base.Path <- new PropertyPath(propertyName) 
            base.Mode <- BindingMode.TwoWay 
            base.UpdateSourceTrigger <- UpdateSourceTrigger.Explicit // the requires explicit events on UIControls ( not UpdateSourceTrigger.PropertyChanged  )
                        
            //base.StringFormat <- "0.##" 
            
            base.Converter <-
                {new IValueConverter with 
                    member  _.Convert(value:obj,  targetType:Type, parameter:obj,  culture:CultureInfo) =  
                        //match value with 
                        //| :? string -> printfn "convert string to %s:%A" targetType.Name value
                        //| :? float -> printfn "convert float to %s:%A" targetType.Name value
                        //| _ -> printfn "convert Other to %s:%A" targetType.Name value 
                        if targetType = typeof<string> then 
                            match value with 
                            | :? float as v ->  
                                if snapToInt then v.ToString("0")|> NumberFormating.addThousandSeparators        
                                else  NumberFormating.float v                                       
                                :> obj
                            | _ -> value
                        else 
                            value
                    
                    member _.ConvertBack(value:obj,  targetType:Type, parameter:obj,  culture:CultureInfo) = 
                        //match value with 
                        //| :? string -> printfn "convert BACK string to %s:%A" targetType.Name value
                        //| :? float -> printfn "convert BACK float to %s:%A" targetType.Name value
                        //| _ -> printfn "convert BACK Other to %s:%A" targetType.Name value                     
                        if targetType = typeof<Double> then 
                            match value with 
                            | :? string as str -> 
                                let strEn = str.Replace( ",", ".") // to allow german formating too, also done in  TextBoxForSepChar TextChanged event          
                                match NumberFormating.tryParseNiceFloat strEn with                      
                                |Some v ->v :> obj
                                |None   ->  null
                            | _ ->  value
                        else 
                            value
                        }
            
