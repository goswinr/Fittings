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
    
    /// a text box that handels delet correctlt for thousand seperator character
    /// Inset in controlled by converter in binding 
    type TextBoxForSepChar() as this  = 
        inherit TextBox()
        
        let updateBindings() = 
            this.GetBindingExpression(TextBox.TextProperty).UpdateSource()
            //this.GetBindingExpression(TextBox.TextProperty).UpdateTarget()
        
        do 
            // explicit binding update
            // some case that should not trigger a binding update
            this.TextChanged.Add (fun a -> 
                let t = this.Text
                let i = this.CaretIndex
                let len = t.Length 
                let lasti = len - 1
                if  len = 0      then a.Handled <- true // empty field
                elif t="-"   then a.Handled <- true // just typed a minus
                elif t.[lasti] = '.' && i=len then a.Handled <- true // typed a '.'
                else                    
                    match t.IndexOf "." with
                    | -1 -> updateBindings()
                    | i  ->                             
                        if t.[lasti] = '0' then a.Handled <- true // one or several '0' after a '.'
                        else updateBindings()                        
                )
           
            
            // otherwise it would be immpossible to delete a thousand separator:
            this.PreviewKeyDown.Add (fun a -> 
                if a.Key = Key.Delete then 
                    let t = this.Text
                    let i = this.CaretIndex
                    if t.Length > i+1 then // 2 chars left minimum
                        if t.[i] = NumberFormating.thousandSeparator then 
                            a.Handled<-true
                            this.Text <- t.Remove(i,2)
                            this.CaretIndex <- i
                    
                elif a.Key = Key.Back then 
                    let t = this.Text
                    let i = this.CaretIndex
                    if i>1 then // 2 chars left minimum
                        if t.[i-1] = NumberFormating.thousandSeparator then 
                            a.Handled<-true
                            this.Text <- t.Remove(i-2,2)
                            this.CaretIndex <- i-2
                
                // replace comma with period !!
                elif a.Key = Key.OemComma then 
                   let t = this.Text
                   let i = this.CaretIndex
                   a.Handled<-true
                   this.Text <- t.Insert(i,".")
                   this.CaretIndex <- i+1
                )


    /// add a element to a UIElementCollection
    let inline add (el:#UIElement) (parent:UIElementCollection) = parent.Add el  |> ignore ; parent
    

    let makeSliderPanel(label:string, sliderVM:SliderViewModel) :DockPanel= 
        // make view
        let header  = TextBlock(           MinWidth = 100. ,  Margin = Thickness(3.),  TextAlignment=TextAlignment.Right )
        let curt    = TextBoxForSepChar(   MinWidth = 60.  ,  Margin = Thickness(6. , 3. , 6. , 3.)) 
        let mit     = TextBoxForSepChar(   MinWidth = 40.  ,  Margin = Thickness(3.)) 
        let slider  = Slider(              MinWidth = 100. ,  Margin = Thickness(3.))    
        let mat     = TextBoxForSepChar(   MinWidth = 40.  ,  Margin = Thickness(3.)) 
   
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
   