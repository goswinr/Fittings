namespace FsEx.Wpf

open System
open System.Windows
open System.Windows.Data
open System.Windows.Controls
open System.Globalization


open System.Collections.ObjectModel
open System.ComponentModel
open System.Windows.Input
open FsEx.Wpf.ViewModel
open FsEx.Wpf.FormatedFloats

module Sliders =

    

    /// A View model for a slider control
    /// Includes a changed event if value changes
    type SliderViewModel (minVal:float, maxVal:float, initalVal:float,  snapToInt) as x = 
        inherit ViewModelBase()
    
        let changed = new Event<float>()        
        let minChanged = new Event<float>()        
        let maxChanged = new Event<float>()        
    
        let mutable minVal = minVal
        let mutable maxVal = maxVal
        let mutable currentVal = initalVal 
        //let currentVal = ref initalVal 
    
        member x.MinVal
            with get()  = minVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v<>minVal then  
                    minVal <- v
                    x.OnPropertyChanged(nameof x.MinVal)
                    minChanged.Trigger(v) 
        
         member x.MaxVal
            with get()  = maxVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v<>maxVal then 
                    maxVal <- v
                    x.OnPropertyChanged(nameof x.MaxVal)     
                    maxChanged.Trigger(v) 

        member x.CurrentValue
            with get()  = currentVal
            and set(v0) =   
                let v = if snapToInt then round (v0)  else v0
                if v <> currentVal then 
                    currentVal <- v
                    x.OnPropertyChanged(nameof x.CurrentValue) 
                    changed.Trigger(v) 
        
        member val MinValBinding        = FormatedFloatBinding(x, nameof x.MinVal, snapToInt) 
        member val MaxValBinding        = FormatedFloatBinding(x, nameof x.MaxVal, snapToInt)
        member val CurrentValueBinding  = FormatedFloatBinding(x, nameof x.CurrentValue , snapToInt)
    
        member val SnapToInteger = snapToInt
    
        /// reports the new value of the sliders cursor
        [<CLIEvent>]
        member x.Changed = changed.Publish
        
        /// slider minValue changed
        [<CLIEvent>]
        member x.MinChanged = minChanged.Publish
        
        /// slider maxValue changed
        [<CLIEvent>]
        member x.MaxChanged = maxChanged.Publish

    

    /// add a element to a UIElementCollection
    let inline add (el:#UIElement) (parent:UIElementCollection) = parent.Add el  |> ignore ; parent
    
    type SliderPanel = {
        header:TextBlock
        currentVal :FormatedFloatTextBox
        minVal:FormatedFloatTextBox
        slider:Slider
        maxVal:FormatedFloatTextBox
        panel :DockPanel
        }
    

    

    let makeSliderPanel(label:string, sliderVM:SliderViewModel) :SliderPanel= 
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

        let setChangeStep() = 
            slider.SmallChange <- (sliderVM.MaxVal - sliderVM.MinVal) / 40. // 40 steps in slider when pressing right or left arrow key
            slider.LargeChange <- (sliderVM.MaxVal - sliderVM.MinVal) / 20. // 20 steps in slider when klicking right or left of the current value cursor
        setChangeStep()
    
        //bind to view model
        mit.SetBinding   (TextBox.TextProperty  , sliderVM.MinValBinding      )    |> ignore 
        mat.SetBinding   (TextBox.TextProperty  , sliderVM.MaxValBinding      )    |> ignore 
        curt.SetBinding  (TextBox.TextProperty  , sliderVM.CurrentValueBinding)    |> ignore 
        slider.SetBinding(Slider.MinimumProperty, sliderVM.MinValBinding      )    |> ignore 
        slider.SetBinding(Slider.MaximumProperty, sliderVM.MaxValBinding      )    |> ignore 
        slider.SetBinding(Slider.ValueProperty  , sliderVM.CurrentValueBinding)    |> ignore  
        
        // explicit binding update ( for textboxes the  explicit  update is set in FormatedFloatTextBox class)
        slider.ValueChanged.Add (fun a -> 
            slider.GetBindingExpression(Slider.ValueProperty).UpdateSource() )//because the binding in text box is set to explicit update
            //slider.GetBindingExpression(Slider.ValueProperty).UpdateTarget() )

        sliderVM.MinChanged.Add ( fun _ -> setChangeStep())
        sliderVM.MaxChanged.Add ( fun _ -> setChangeStep())

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
        {
        header=header
        currentVal = curt
        minVal= mit
        slider= slider
        maxVal= mat
        panel=d
        }

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
   