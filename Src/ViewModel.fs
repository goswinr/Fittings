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

        /// if the absolut value of a float is below this, display just Zero
        /// default = Double.Epsilon = no rounding down
        let mutable roundToZeroBelow = Double.Epsilon

        let addThousandSeparators (s:string) =
            let last = s.Length - 1         
            let sb = Text.StringBuilder()
            let inline add (c:char) = sb.Append(c) |> ignore
            for i = 0 to last do
                if i = 0 || i = last then 
                    add s.[i]
                elif i = 1 && s.[0] = '-' then 
                    add s.[i]
                else
                    if (last - i + 1) % 3 = 0 then 
                        add thousandSeparator
                        add s.[i]
                    else                
                        add s.[i]
            sb.ToString() 
    
        let int (x:int) = 
            if abs(x) > 1000 then x.ToString() |> addThousandSeparators
            else                  x.ToString() 

        /// Formating with automatic precision 
        /// e.g.: 0 digits behind comma if above 1000 
        /// if there are more than 15 zeros behind the comma just '~0.0' will be displayed
        /// if the value is smaller than NumberFormating.roundToZeroBelow '0.0' will be shown.
        /// this is Double.Epsilon by default
        let float  (x:float) =
            if   Double.IsNaN x then "NaN"
            elif x = Double.NegativeInfinity then "Negative Infinity"
            elif x = Double.PositiveInfinity then "Positive Infinity"
            elif x = -1.23432101234321e+308 then "-1.23432e+308 (=RhinoMath.UnsetValue)" // for https://developer.rhino3d.com/api/RhinoCommon/html/F_Rhino_RhinoMath_UnsetValue.htm
            elif x = 0.0 then "0.0" // not "0" as in sprintf "%g"
            else
                let  a = abs x
                if   a < roundToZeroBelow then "0.0"
                elif a > 10000. then x.ToString("#")|> addThousandSeparators 
                elif a > 1000.  then x.ToString("#")
                elif a > 100.   then x.ToString("#.#" , invC)
                elif a > 10.    then x.ToString("#.##" , invC)
                elif a > 1.     then x.ToString("#.###" , invC)
                elif a > 0.1    then x.ToString("0.####" , invC)
                elif a > 0.01   then x.ToString("0.#####" , invC)
                elif a > 0.001  then x.ToString("0.######" , invC)
                elif a > 0.0001 then x.ToString("0.#######" , invC)
                elif a > 0.000000000000001 then x.ToString("0.###############" , invC)// 15 decimal paces for doubles
                else "~0.0"

        /// A very tolerant custom float parser
        /// ignores all non numeric characters ( expect leading '-' )
        /// and considers '.' and  ',' as decimal point
        /// does not allow for scientific notation
        let tryParseFloatTolerant(s:string) =
            let sb = Text.StringBuilder(s.Length)
            for c in s do
                if c >= '0' && c <= '9' then sb.Append(c) |> ignore
                elif c = '.' then sb.Append(c) |> ignore
                elif c = '-' && sb.Length = 0  then sb.Append(c) |> ignore //only add minus at start
                elif c = ',' then sb.Append('.') |> ignore // german formating
            match Double.TryParse(sb.ToString(), NumberStyles.Float, enUs) with
            | true, f -> Some f
            | _ ->   None 





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
                                match NumberFormating.tryParseFloatTolerant str with                                 
                                |Some v -> v :> obj
                                |None   -> null
                            | _ -> value
                        else 
                            value
                        }
            
