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
                // so that text fields that are bound to floats can have a dot too:
                // https://stackoverflow.com/a/35942615/969070
                // setting this fails when a hosting WPF process is alread up and running (eg loaded in Seff Ui therad)  
                FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty <- false
            with 
                e -> () 
            
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
                                if snapToInt then v.AsString0    :> obj
                                else              v.ToNiceString :> obj
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
                                match UtilMath.tryParseFloatEnDe str with
                                |Some v ->  
                                    printfn "parse float  from'%s' = %A" str v
                                    v :> obj
                                |None ->  
                                    printfn "parse float failed on '%s'" str
                                    null
                            | _ -> value
                        else 
                            value
                        }
            
