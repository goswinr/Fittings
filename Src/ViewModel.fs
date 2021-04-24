namespace FsEx.Wpf


open System
open System.Windows
open System.Windows.Data
open System.Windows.Controls
open System.Globalization
open FsEx


open System.Collections.ObjectModel
open System.ComponentModel
open Microsoft.FSharp
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

module ViewModel =        
    

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
                for i=st to en-1 do // dont go to last one becaus it shal never get a separator 
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
            |"NaN" -> None
            |"Negative Infinity" -> None
            |"Positive Infinity" -> None
            |"-1.23432e+308 (=RhinoMath.UnsetValue)" -> Some 0.0 // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
            |"~0.0" ->  Some 0.0
            |"~-0.0"->  Some 0.0
            | _ -> 
                match Double.TryParse(s.Replace(string(thousandSeparator),""),NumberStyles.Float,invC) with 
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
            if   Double.IsNaN x then "NaN"
            elif x = Double.NegativeInfinity then "Negative Infinity"
            elif x = Double.PositiveInfinity then "Positive Infinity"
            elif x = -1.23432101234321e+308 then "-1.23432e+308 (=RhinoMath.UnsetValue)" // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
            elif x = 0.0 then "0.0" // not "0" as in sprintf "%g"
            else
                let  a = abs x
                if   a > 10000.     then x.ToString("#")|> addThousandSeparators 
                elif a > 1000.      then x.ToString("#")
                elif a > 100.       then x.ToString("#.#" , invC)
                elif a > 10.        then x.ToString("#.##" , invC)
                elif a > 1.         then x.ToString("#.###" , invC)
                //elif   a < roundToZeroBelow then "0.0"
                elif a > 0.1        then x.ToString("0.####" , invC)|> addThousandSeparators 
                elif a > 0.01       then x.ToString("0.#####" , invC)|> addThousandSeparators 
                elif a > 0.001      then x.ToString("0.######" , invC)|> addThousandSeparators 
                elif a > 0.0001     then x.ToString("0.#######" , invC)|> addThousandSeparators 
                elif a > 0.00001    then x.ToString("0.########" , invC)|> addThousandSeparators 
                elif a > 0.000001   then x.ToString("0.#########" , invC)|> addThousandSeparators 
                elif a > 0.0000001  then x.ToString("0.##########" , invC)|> addThousandSeparators 
                elif a > 0.000000000000001 then x.ToString("0.###############" , invC)|> addThousandSeparators // 15 decimal paces for doubles
                elif x > 0.0 then "~0.0"
                else "~-0.0"
               



    type ViewModelBase() = 
    
        //using quotations to avoid unsafe strings
        // http://www.fssnip.net/4Q/title/F-Quotations-with-INotifyPropertyChanged
        let propertyChanged = new Event<_, _>()
    
        let toPropName(query : Expr) =  match query with PropertyGet(a, b, list) -> b.Name | _ -> ""

        interface INotifyPropertyChanged with
            [<CLIEvent>]
            member x.PropertyChanged = propertyChanged.Publish

        abstract member OnPropertyChanged: string -> unit // needed ?
    
        default x.OnPropertyChanged(propertyName : string) = // needed ?
            propertyChanged.Trigger(x, new PropertyChangedEventArgs(propertyName))

        member x.OnPropertyChanged(expr : Expr) =
            let propName = toPropName(expr)
            x.OnPropertyChanged(propName)


    /// uses float.ToNiceString from FsEx for diplaying floats 
    type BindingTwoWay(model:INotifyPropertyChanged, memberExpr:Expr, snapToInt:bool) = 
        inherit Binding() 
    
        let toPropName(query : Expr) =  match query with PropertyGet(a, b, list) -> b.Name | _ -> ""
    
        do  
            try
                // so that wpf textboxes that are bound to floats can have a dot input too. see https://stackoverflow.com/a/35942615/969070
                // setting this might fails when a hosting WPF process is alread up and running (eg loaded in another WPF thread ,for example in Seff UI therad)  
                FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty <- false
            with  _ -> ()
                //if FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty then 
                //    eprintfn "could not set KeepTextBoxDisplaySynchronizedWithTextProperty to false "
                
            base.Source <- model
            base.Path <- new PropertyPath(toPropName(memberExpr) ) 
            base.UpdateSourceTrigger <- UpdateSourceTrigger.PropertyChanged 
            base.Mode <- BindingMode.TwoWay
            //base.StringFormat <- "0.##" 
            //base.StringFormat <- "N2" //always show two digits behind comma
            base.Converter <-
                {new IValueConverter with 
                    member  this.Convert(value:obj,  targetType:Type, parameter:obj,  culture:CultureInfo) =  
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
                    
                    member this. ConvertBack(value:obj,  targetType:Type, parameter:obj,  culture:CultureInfo) = 
                        //match value with 
                        //| :? string -> printfn "convert BACK string to %s:%A" targetType.Name value
                        //| :? float -> printfn "convert BACK float to %s:%A" targetType.Name value
                        //| _ -> printfn "convert BACK Other to %s:%A" targetType.Name value                     
                        if targetType = typeof<Double> then 
                            match value with 
                            | :? string as str ->  
                                match NumberFormating.tryParseNiceFloat str with                                 
                                |Some v -> v :> obj
                                |None   -> null
                            | _ -> value
                        else 
                            value
                        }
            
