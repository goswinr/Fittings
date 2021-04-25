namespace FsEx.Wpf

open System
open System.Globalization
open System.Windows
open System.Windows.Controls
open System.Windows.Input
open System.Windows.Data
open System.ComponentModel


module FormatedFloats = 

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

    /// A text box for Numbers formated with NumberFormating.thousandSeparator via FormatedFloatBinding
    /// Handles deleting correctly to remove number and separator at once if appropiate.
    /// Also has custom logic for positioning caret
    /// (Inserting thousandSeparator is controlled by converter in FormatedFloatBinding) 
    type FormatedFloatTextBox() as this  = 
        inherit TextBox()                
    
        /// storing the expected caret position after inserting or delete key
        /// counting only numbers and any othe symbols like  , . - but not NumberFormating.thousandSeparator
        let mutable numsBeforeCaret = 0
    
        /// the caret position ignoring thousand separators
        let setNumCaret (s:string , pos, shift) = 
            numsBeforeCaret<-0
            for i=0 to pos-1 do
                //printfn "setNumCaret:%d" i
                if s.[i] <> NumberFormating.thousandSeparator then 
                    numsBeforeCaret <- numsBeforeCaret + 1
            numsBeforeCaret <- numsBeforeCaret + shift
            //printfn "numsBeforeCaret:%d" numsBeforeCaret

        /// setting the caret to the position memorized via the above function 
        let setActualCaret () = 
            let s = this.Text  
            let rec loop (nums,i) = 
                if i=s.Length               then this.CaretIndex <- i
                elif nums = numsBeforeCaret then this.CaretIndex <- i //max 0 (min this.Text.Length i)
                elif s.[i] <> NumberFormating.thousandSeparator then loop(nums+1,i+1)
                else loop(nums,i+1)
            loop(0,0)


        let updateBindings() = 
            this.GetBindingExpression(TextBox.TextProperty).UpdateSource()
            //this.GetBindingExpression(TextBox.TextProperty).UpdateTarget() // not needed
            setActualCaret()


        do 
            // explicit binding update:
            // there are some case that should not trigger a binding update
            this.TextChanged.Add (fun a -> 
                let t = this.Text
                let i = this.CaretIndex
                let len = t.Length                 
                if  len = 0  then a.Handled <- true // empty field
                elif t="-"   then a.Handled <- true // just typed a minus
                elif i=len   then // entered last char                
                    let last = t.[len-1]
                    if   last = '.'  then a.Handled <- true // just typed a '.'
                    elif last = ','  then a.Handled <- true // just typed a ',' , will be convrted to '.' in binding convertor
                    else 
                        let hasDot = t.IndexOf "." <> -1
                        if hasDot && last = '0'  then a.Handled <- true // one or several '0' after a '.'
                        else updateBindings()
                else updateBindings()                 
                )
       
        
            // otherwise it would be immpossible to delete a thousand separator:
            this.PreviewKeyDown.Add (fun a -> 
                let t = this.Text
                let i = this.CaretIndex 
                if this.IsSelectionActive && this.SelectionLength > 0 then // just the cursor in counts a active selction already
                    //printfn "Selection active"
                    if a.Key = Key.Delete  || a.Key = Key.Back then
                        setNumCaret(t, this.SelectionStart,0)                    
                    else // any key
                        setNumCaret(t, this.SelectionStart, 1)

                else                
                    if a.Key = Key.Delete then
                        setNumCaret(t, i, 0) // 0 = caret does not move
                        if t.Length > i+1 then // 2 chars left minimum
                            //printf "t.[i] = %c " t.[i] 
                            if t.[i] = NumberFormating.thousandSeparator then 
                                a.Handled<-true
                                this.Text <- t.Remove(i,2)
                                //this.CaretIndex <- i // done by setActualCaret after binding update
                
                    elif a.Key = Key.Back then
                        setNumCaret(t,i, -1)// anticipates that caret moves one to left
                        if i>1 then // 2 chars left minimum
                            //printf "t.[i-1] = %c " t.[i-1] 
                            if t.[i-1] = NumberFormating.thousandSeparator then 
                                a.Handled<-true
                                this.Text <- t.Remove( i-2 , 2)
                                //this.CaretIndex <- i-2 // done by setActualCaret after binding update
            
                
                    else // any key - also non numeric keys, but this has no effect on result
                        setNumCaret(t, i, 1) // anticipates that caret moves one to right
            
                                //elif a.Key = Key.OemComma then // replace comma with period !! // done in Binding converter
                //   let t = this.Text
                //   let i = this.CaretIndex
                //   a.Handled<-true
                //   this.Text <- t.Insert(i,".")
                //   this.CaretIndex <- i+1
                )


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
            
