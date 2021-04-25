namespace FsEx.Wpf

open System
open System.Windows
open System.Windows.Data
open System.Windows.Controls
open System.Globalization
open FsEx

open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows.Input


module Sliders = 

    open ViewModel

    /// A View model for a slider control
    /// Includes a changed event if value changes
    type SliderViewModel (minVal:float, maxVal:float, initalVal:float,  snapToInt) as x = 
        inherit ViewModelBase()
    
        let changed = new Event<float>()        
    
        let mutable minVal = minVal
        let mutable maxVal = maxVal
        let mutable currentVal = initalVal 
    
        member x.MinVal
            with get()  = minVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v<>minVal then  minVal <- v; x.OnPropertyChanged(nameof x.MinVal)
        
         member x.MaxVal
            with get()  = maxVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v<>maxVal then maxVal <- v; x.OnPropertyChanged(nameof x.MaxVal)     

        member x.CurrentValue
            with get()  = currentVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v<>currentVal then currentVal <- v; x.OnPropertyChanged(nameof x.CurrentValue); changed.Trigger(v) 
        
        member val MinValBinding        = FormatedFloatBinding(x, nameof x.MinVal, snapToInt) 
        member val MaxValBinding        = FormatedFloatBinding(x, nameof x.MaxVal, snapToInt)
        member val CurrentValueBinding  = FormatedFloatBinding(x, nameof x.CurrentValue , snapToInt)
    
        member val SnapToInteger = snapToInt
    
        /// reports the new value of the slider
        [<CLIEvent>]
        member x.Changed = changed.Publish
    
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


    /// add a element to a UIElementCollection
    let inline add (el:#UIElement) (parent:UIElementCollection) = parent.Add el  |> ignore ; parent
    

    let makeSliderPanel(label:string, sliderVM:SliderViewModel) :DockPanel= 
        // make view
        let header  = TextBlock(              MinWidth = 100. ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right )
        let curt    = FormatedFloatTextBox(   MinWidth = 60.  ,  Margin = Thickness(6. , 3. , 6. , 3.)) 
        let mit     = FormatedFloatTextBox(   MinWidth = 40.  ,  Margin = Thickness(3.)) 
        let slider  = Slider(                 MinWidth = 100. ,  Margin = Thickness(3.))    
        let mat     = FormatedFloatTextBox(   MinWidth = 40.  ,  Margin = Thickness(3.)) 
   
        header.Text <- label + ":"
        mit.Background <- Brush.make(245,  245,  245) 
        mat.Background <- Brush.make(245,  245,  245)
    
        slider.Background <- Brush.make(255,  255,  255) 
        slider.Delay <- 50  
        if sliderVM.SnapToInteger then 
            slider.IsSnapToTickEnabled <-true
            slider.TickFrequency <- 1.0
            //if sliderVM.MaxVal - sliderVM.MinVal < 20. then  
                //slider.TickPlacement <- Primitives.TickPlacement.Both // draw ticks too?    
    
        //bind to view model
        mit.SetBinding   (TextBox.TextProperty  , sliderVM.MinValBinding      )    |> ignore 
        mat.SetBinding   (TextBox.TextProperty  , sliderVM.MaxValBinding      )    |> ignore 
        curt.SetBinding  (TextBox.TextProperty  , sliderVM.CurrentValueBinding)    |> ignore 
        slider.SetBinding(Slider.MinimumProperty, sliderVM.MinValBinding      )    |> ignore 
        slider.SetBinding(Slider.MaximumProperty, sliderVM.MaxValBinding      )    |> ignore 
        slider.SetBinding(Slider.ValueProperty  , sliderVM.CurrentValueBinding)    |> ignore  
        
        // explicit binding update
        slider.ValueChanged.Add (fun a -> 
            slider.GetBindingExpression(Slider.ValueProperty).UpdateSource() //because the binding in text box is set to explicit update
            //slider.GetBindingExpression(Slider.ValueProperty).UpdateTarget()
            )

        let d = new DockPanel()
        d.Children
        |> add header
        |> add curt        
        |> add mit
        |> add mat
        |> add slider //added last so it will strech to fill remaining space
        |> ignore 
    
        DockPanel.SetDock(header,Dock.Left)
        DockPanel.SetDock(curt,Dock.Left)
        DockPanel.SetDock(mit,Dock.Left)
        DockPanel.SetDock(mat,Dock.Right)    
    
        d.Background <- Brush.make(235,  235,  235) 
        d

(*
test:
Sync.doSync( fun ()  ->  
    
    printfn "KeepTextBoxDisplaySynchronizedWithTextProperty:%b" FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty 
    let win = PositionedWindow("GosTest") 
    let s1vm = SliderViewModel(-10.5 , 100.,  0.,  false)
    let s2vm = SliderViewModel(-9., 9.,  0., true)
    let s1 = makeSliderPanel("gos1"    , s1vm ) 
    let s2 = makeSliderPanel("gos zwei", s2vm) 
    let vst = StackPanel(Orientation=Orientation.Vertical) 
    vst.Children.Add s1 |> ignore 
    vst.Children.Add s2 |> ignore 
    win.Content <- vst
    
    //s1vm.Changed.Add(fun v -> printfn "%.16f" v) 
    //s2vm.Changed.Add(fun v -> printfn "%.16f" v) 
    
    win.Show() 
    ) 
*)
   